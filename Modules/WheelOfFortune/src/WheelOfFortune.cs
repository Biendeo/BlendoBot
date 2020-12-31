using BlendoBotLib;
using BlendoBotLib.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WheelOfFortune {
	[Command("?wof", "Wheel Of Fortune", "Play a round of the Second Guess puzzle.", "Biendeo", "1.0.0")]
	public class WheelOfFortune : CommandBase, IDisposable {
		public WheelOfFortune(ulong guildId, IBotMethods botMethods) : base(guildId, botMethods) { }
		public override string Usage => $"Usage: {Term.Code()}\nAfter triggering this, a puzzle will be presented. The puzzle will have a category and a phrase, with the letters of the phrase hidden initially. Your goal is to correctly type the phrase once you believe you know what it is. You only get one try, so make sure it is correct. The puzzle will reveal itself gradually one letter at a time for 30 seconds. If you get the answer wrong, I will react with a :x: to tell you the answer was wrong. Your future answers will not be counted. If you get the answer correct, the game is over and you win!";

		private static List<Puzzle> puzzles;

		private DiscordChannel currentChannel;
		private List<DiscordUser> eliminatedUsers;
		private Puzzle currentPuzzle;
		private DiscordMessage lastWinningMessage;

		private SemaphoreSlim semaphore;

		public override async Task<bool> Startup() {
			currentChannel = null;
			eliminatedUsers = new List<DiscordUser>();
			semaphore = new SemaphoreSlim(1);

			if (puzzles == null) {
				puzzles = new List<Puzzle>();
				if (File.Exists(Path.Combine(BotMethods.GetCommandCommonDataPath(this, this), "puzzles.txt"))) {
					using (var file = File.OpenRead(Path.Combine(BotMethods.GetCommandCommonDataPath(this, this), "puzzles.txt"))) {
						using var reader = new StreamReader(file);
						while (!reader.EndOfStream) {
							string line = reader.ReadLine();
							puzzles.Add(new Puzzle {
								Category = line.Split(";")[0],
								Phrase = line.Split(";")[1]
							});
						}
					}
					BotMethods.Log(this, new LogEventArgs {
						Type = LogType.Log,
						Message = $"Wheel Of Fortune loaded {puzzles.Count} puzzles"
					});
				} else {
					BotMethods.Log(this, new LogEventArgs {
						Type = LogType.Warning,
						Message = $"Wheel Of Fortune could not find {Path.Combine(BotMethods.GetCommandCommonDataPath(this, this), "puzzles.txt")}. Make sure that you have that file with words in the format: {"category;phrase"}."
					});
				}
			}

			await Task.Delay(0);
			return true;
		}

		public async Task HandleMessageListener(MessageCreateEventArgs e) {
			await semaphore.WaitAsync();
			if (currentChannel != null && e.Channel == currentChannel) {
				if (!eliminatedUsers.Contains(e.Author)) {
					var alphabetRegex = new Regex("[^A-Z]");
					string messageText = alphabetRegex.Replace(e.Message.Content.ToUpper(), "");
					string expectedAnswer = alphabetRegex.Replace(currentPuzzle.Phrase.ToUpper(), "");
					if (messageText == expectedAnswer) {
						currentChannel = null;
						eliminatedUsers.Clear();
						lastWinningMessage = e.Message;
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"Congratulations to {e.Author.Mention} for getting the correct answer! Thanks for playing!",
							Channel = e.Channel,
							LogMessage = "WheelOfFortuneGameWin"
						});
					} else {
						eliminatedUsers.Add(e.Author);
						await e.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❌"));
					}
				}
			}
			semaphore.Release();
		}

		public override async Task OnMessage(MessageCreateEventArgs e) {
			await semaphore.WaitAsync();
			if (currentChannel != null) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"A game is already in session in {currentChannel.Mention}, please wait until it has finished!",
					Channel = e.Channel,
					LogMessage = "WheelOfFortuneGameInProgress"
				});
			} else {
				currentChannel = e.Channel;
				await Task.Factory.StartNew(() => StartGame(e.Channel));
			}
			semaphore.Release();
		}

		private async Task StartGame(DiscordChannel channel) {
			var message = await BotMethods.SendMessage(this, new SendMessageEventArgs {
				Message = "Choosing a puzzle...",
				Channel = channel,
				LogMessage = "WheelOfFortuneGameStart"
			});

			await semaphore.WaitAsync();
			currentChannel = channel;
			eliminatedUsers.Clear();
			var random = new Random();
			currentPuzzle = puzzles[random.Next(0, puzzles.Count)];
			semaphore.Release();

			for (int i = 5; i > 0; --i) {
				await message.ModifyAsync($"Game starting in {i} second{(i != 1 ? "s" : string.Empty)}...");
				await Task.Delay(1000);
			}

			var messageListener = new WheelOfFortuneMessageListener(this);
			BotMethods.AddMessageListener(this, GuildId, messageListener);

			string revealedPuzzle = currentPuzzle.Phrase.ToUpper();
			for (char c = 'A'; c <= 'Z'; ++c) {
				revealedPuzzle = revealedPuzzle.Replace(c, '˷');
			}

			await message.ModifyAsync($"{currentPuzzle.Category}\n\n{revealedPuzzle}".CodeBlock());

			int timeToWait = 30000 / revealedPuzzle.Count(c => c == '˷');

			while (currentChannel != null && revealedPuzzle != currentPuzzle.Phrase.ToUpper()) {
				await Task.Delay(timeToWait);
				if (currentChannel != null) {
					bool replacedUnderscore = false;
					while (!replacedUnderscore) {
						int index = random.Next(0, revealedPuzzle.Length);
						if (revealedPuzzle[index] == '˷') {
							revealedPuzzle = revealedPuzzle.Substring(0, index) + currentPuzzle.Phrase.ToUpper()[index] + revealedPuzzle.Substring(index + 1);
							replacedUnderscore = true;
						}
					}
					await message.ModifyAsync($"{currentPuzzle.Category}\n\n{revealedPuzzle}".CodeBlock());
				}
			}

			await semaphore.WaitAsync();
			if (currentChannel != null) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"No one got the puzzle! The answer was {revealedPuzzle.Code()}. Thanks for playing!",
					Channel = channel,
					LogMessage = "WheelOfFortuneGameLose"
				});
			} else {
				await message.ModifyAsync($"{$"{currentPuzzle.Category}\n\n{revealedPuzzle}".CodeBlock()}\n{lastWinningMessage.Author.Mention} got the correct answer {currentPuzzle.Phrase.ToUpper().Code()}");
			}

			currentChannel = null;
			eliminatedUsers.Clear();
			currentPuzzle = null;
			semaphore.Release();

			BotMethods.RemoveMessageListener(this, GuildId, messageListener);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (semaphore != null) {
					semaphore.Dispose();
				}
			}
		}
	}
}

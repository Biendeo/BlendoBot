using BlendoBotLib;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WheelOfFortune {
	public class WheelOfFortune : CommandBase {
		public WheelOfFortune(ulong guildId, IBotMethods botMethods) : base(guildId, botMethods) { }

		public override string Term => "?wof";
		public override string Name => "Wheel Of Fortune";
		public override string Description => "Play a round of the Second Guess puzzle.";
		public override string Usage => $"Usage: {"?wof".Code()}\nAfter triggering this, a puzzle will be presented. The puzzle will have a category a phrase, with the letters of the phrase hidden initially. Your goal is to correctly type the answer once you believe you know what it is. You only get one try, so make sure it is correct. Every two seconds, a new letter in the puzzle will be revealed. If you get the answer wrong, I will react with a :x: to tell you the answer was wrong. Your future answers will not be counted. If you get the answer correct, the game is over and you win!";
		public override string Author => "Biendeo";
		public override string Version => "1.0.0";

		private static List<Puzzle> puzzles;

		private DiscordChannel currentChannel;
		private List<DiscordUser> eliminatedUsers;
		private Puzzle currentPuzzle;
		private DiscordMessage lastWinningMessage;

		public override async Task<bool> Startup() {
			currentChannel = null;
			eliminatedUsers = new List<DiscordUser>();

			if (puzzles == null) {
				puzzles = new List<Puzzle>();
				if (File.Exists(Path.Combine(BotMethods.GetCommandCommonDataPath(this, this), "puzzles.txt"))) {
					using (var file = File.OpenRead(Path.Combine(BotMethods.GetCommandCommonDataPath(this, this), "puzzles.txt"))) {
						using (var reader = new StreamReader(file)) {
							while (!reader.EndOfStream) {
								string line = reader.ReadLine();
								puzzles.Add(new Puzzle {
									Category = line.Split(";")[0],
									Phrase = line.Split(";")[1]
								});
							}
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
			if (currentChannel != null && e.Channel == currentChannel) {
				if (!eliminatedUsers.Contains(e.Author)) {
					var alphabetRegex = new Regex("[^A-Z]");
					var messageText = alphabetRegex.Replace(e.Message.Content.ToUpper(), "");
					var expectedAnswer = alphabetRegex.Replace(currentPuzzle.Phrase.ToUpper(), "");
					if (messageText == expectedAnswer) {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"Congratulations to {e.Author.Mention} for getting the correct answer! Thanks for playing!",
							Channel = e.Channel,
							LogMessage = "WheelOfFortuneGameWin"
						});
						currentChannel = null;
						eliminatedUsers.Clear();
						lastWinningMessage = e.Message;
					} else {
						eliminatedUsers.Add(e.Author);
						await e.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❌"));
					}
				}
			}
		}

		public override async Task OnMessage(MessageCreateEventArgs e) {
			if (currentChannel != null) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"A game is already in session in {e.Channel.Mention}, please wait until it has finished!",
					Channel = e.Channel,
					LogMessage = "WheelOfFortuneGameInProgress"
				});
			} else {
				await Task.Factory.StartNew(() => StartGame(e.Channel));
			}
		}

		private async Task StartGame(DiscordChannel channel) {
			var message = await BotMethods.SendMessage(this, new SendMessageEventArgs {
				Message = "Choosing a puzzle...",
				Channel = channel,
				LogMessage = "WheelOfFortuneGameStart"
			});

			currentChannel = channel;
			eliminatedUsers.Clear();
			var random = new Random();
			currentPuzzle = puzzles[random.Next(0, puzzles.Count)];

			for (int i = 5; i > 0; --i) {
				await message.ModifyAsync($"Game starting in {i} second{(i != 1 ? "s" : string.Empty)}...");
				await Task.Delay(1000);
			}

			var messageListener = new WheelOfFortuneMessageListener(this);
			BotMethods.AddMessageListener(this, GuildId, messageListener);

			string revealedPuzzle = currentPuzzle.Phrase.ToUpper();
			for (char c = 'A'; c <= 'Z'; ++c) {
				revealedPuzzle = revealedPuzzle.Replace(c, '_');
			}

			await message.ModifyAsync($"{currentPuzzle.Category}\n\n{revealedPuzzle}".CodeBlock());

			while (currentChannel != null && revealedPuzzle != currentPuzzle.Phrase.ToUpper()) {
				await Task.Delay(2000);
				if (currentPuzzle != null) {
					bool replacedUnderscore = false;
					while (!replacedUnderscore) {
						int index = random.Next(0, revealedPuzzle.Length);
						if (revealedPuzzle[index] == '_') {
							revealedPuzzle = revealedPuzzle.Substring(0, index) + currentPuzzle.Phrase.ToUpper()[index] + revealedPuzzle.Substring(index + 1);
							replacedUnderscore = true;
						}
					}
					await message.ModifyAsync($"{currentPuzzle.Category}\n\n{revealedPuzzle}".CodeBlock());
				}
			}

			await Task.Delay(500);

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

			BotMethods.RemoveMessageListener(this, GuildId, messageListener);
		}
	}
}

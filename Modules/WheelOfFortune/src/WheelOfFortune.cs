using BlendoBotLib;
using BlendoBotLib.Interfaces;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WheelOfFortune
{
    public class WheelOfFortune : ICommand, IDisposable
    {
        public string Name => "Wheel Of Fortune";
        public string Description => "Play a round of the Second Guess puzzle.";
        public string GetUsage(string term) => $"Usage: {term.Code()}\nAfter triggering this, a puzzle will be presented. The puzzle will have a category and a phrase, with the letters of the phrase hidden initially. Your goal is to correctly type the phrase once you believe you know what it is. You only get one try, so make sure it is correct. The puzzle will reveal itself gradually one letter at a time for 30 seconds. If you get the answer wrong, I will react with a :x: to tell you the answer was wrong. Your future answers will not be counted. If you get the answer correct, the game is over and you win!";
        public string Author => "Biendeo";
        public string Version => "1.5.0";

        private static List<Puzzle>? puzzles;

        private DiscordChannel? currentChannel;
        private List<DiscordUser> eliminatedUsers;
        private Puzzle? currentPuzzle;
        private DiscordMessage? lastWinningMessage;

        private SemaphoreSlim semaphore;
        private readonly IDiscordClient discordClient;
        private readonly ILogger<WheelOfFortune> logger;
        private readonly IMessageListenerRepository messageListenerRepository;
        private readonly IDataStore<WheelOfFortune> dataStore;

        public WheelOfFortune(
            IDataStore<WheelOfFortune> dataStore,
            IDiscordClient discordClient,
            IMessageListenerRepository messageListenerRepository,
            ILogger<WheelOfFortune> logger)
        {
            this.logger = logger;

            this.dataStore = dataStore;
            this.discordClient = discordClient;
            this.messageListenerRepository = messageListenerRepository;

            currentChannel = null;
            eliminatedUsers = new List<DiscordUser>();
            semaphore = new SemaphoreSlim(1);

            if (puzzles == null)
            {
                try
                {
                    puzzles = this.dataStore.ReadAsync<List<Puzzle>>("puzzles.txt").Result;
                    this.logger.LogInformation("Wheel Of Fortune loaded {} puzzles", puzzles.Count);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Wheel Of Fortune failed to load puzzles with exception");
                }
            }
        }

        public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddSingleton<IDataStore<WheelOfFortune>, PuzzlesTxtDataStore>();
        }

        public async Task HandleMessageListener(MessageCreateEventArgs e)
        {
            await semaphore.WaitAsync();
            if (currentChannel != null && e.Channel == currentChannel && currentPuzzle != null)
            {
                if (!eliminatedUsers.Contains(e.Author))
                {
                    var alphabetRegex = new Regex("[^A-Z]");
                    string messageText = alphabetRegex.Replace(e.Message.Content.ToUpper(), "");
                    string expectedAnswer = alphabetRegex.Replace(currentPuzzle.Phrase.ToUpper(), "");
                    if (messageText == expectedAnswer)
                    {
                        currentChannel = null;
                        eliminatedUsers.Clear();
                        lastWinningMessage = e.Message;
                        await this.discordClient.SendMessage(this, new SendMessageEventArgs
                        {
                            Message = $"Congratulations to {e.Author.Mention} for getting the correct answer! Thanks for playing!",
                            Channel = e.Channel,
                            LogMessage = "WheelOfFortuneGameWin"
                        });
                    }
                    else
                    {
                        eliminatedUsers.Add(e.Author);
                        await e.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❌"));
                    }
                }
            }
            semaphore.Release();
        }

        public async Task OnMessage(MessageCreateEventArgs e)
        {
            await semaphore.WaitAsync();
            if (currentChannel != null)
            {
                await this.discordClient.SendMessage(this, new SendMessageEventArgs
                {
                    Message = $"A game is already in session in {currentChannel.Mention}, please wait until it has finished!",
                    Channel = e.Channel,
                    LogMessage = "WheelOfFortuneGameInProgress"
                });
            }
            else
            {
                currentChannel = e.Channel;
                if (puzzles == null)
                {
                    await this.discordClient.SendMessage(this, new SendMessageEventArgs
                    {
                        Message = $"No puzzles available!",
                        Channel = e.Channel,
                        LogMessage = "WheelOfFortuneNullPuzzles"
                    });
                }

#pragma warning disable CS4014
                // Continue in background
                Task.Run(() => StartGame(e.Channel, e.Guild.Id));
#pragma warning restore CS4014
            }
            semaphore.Release();
        }

        private async Task StartGame(DiscordChannel channel, ulong guildId)
        {
            var message = await this.discordClient.SendMessage(this, new SendMessageEventArgs
            {
                Message = "Choosing a puzzle...",
                Channel = channel,
                LogMessage = "WheelOfFortuneGameStart"
            });

            await semaphore.WaitAsync();
            currentChannel = channel;
            eliminatedUsers.Clear();
            var random = new Random();
            if (puzzles is List<Puzzle>)
            {
                currentPuzzle = puzzles[random.Next(0, puzzles.Count)];
            }
            else
            {
                await message.ModifyAsync($"No puzzles to choose from!");
                semaphore.Release();
                return;
            }

            semaphore.Release();

            for (int i = 5; i > 0; --i)
            {
                await message.ModifyAsync($"Game starting in {i} second{(i != 1 ? "s" : string.Empty)}...");
                await Task.Delay(1000);
            }

            var messageListener = new WheelOfFortuneMessageListener(this);
            this.messageListenerRepository.Add(guildId, messageListener);

            string revealedPuzzle = currentPuzzle.Phrase.ToUpper();
            for (char c = 'A'; c <= 'Z'; ++c)
            {
                revealedPuzzle = revealedPuzzle.Replace(c, '˷');
            }

            await message.ModifyAsync($"{currentPuzzle.Category}\n\n{revealedPuzzle}".CodeBlock());

            int timeToWait = 30000 / revealedPuzzle.Count(c => c == '˷');

            while (currentChannel != null && revealedPuzzle != currentPuzzle.Phrase.ToUpper())
            {
                await Task.Delay(timeToWait);
                if (currentChannel != null)
                {
                    bool replacedUnderscore = false;
                    while (!replacedUnderscore)
                    {
                        int index = random.Next(0, revealedPuzzle.Length);
                        if (revealedPuzzle[index] == '˷')
                        {
                            revealedPuzzle = revealedPuzzle.Substring(0, index) + currentPuzzle.Phrase.ToUpper()[index] + revealedPuzzle.Substring(index + 1);
                            replacedUnderscore = true;
                        }
                    }
                    await message.ModifyAsync($"{currentPuzzle.Category}\n\n{revealedPuzzle}".CodeBlock());
                }
            }

            await semaphore.WaitAsync();
            if (currentChannel != null)
            {
                await this.discordClient.SendMessage(this, new SendMessageEventArgs
                {
                    Message = $"No one got the puzzle! The answer was {revealedPuzzle.Code()}. Thanks for playing!",
                    Channel = channel,
                    LogMessage = "WheelOfFortuneGameLose"
                });
            }
            else
            {
                await message.ModifyAsync($"{$"{currentPuzzle.Category}\n\n{revealedPuzzle}".CodeBlock()}\n{lastWinningMessage?.Author.Mention ?? "<null>"} got the correct answer {currentPuzzle.Phrase.ToUpper().Code()}");
            }

            currentChannel = null;
            eliminatedUsers.Clear();
            currentPuzzle = null;
            semaphore.Release();

            this.messageListenerRepository.Remove(guildId, messageListener);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (semaphore != null)
                {
                    semaphore.Dispose();
                }
            }
        }
    }
}

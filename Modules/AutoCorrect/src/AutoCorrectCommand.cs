namespace AutoCorrect
{
    using System;
    using System.Threading.Tasks;
    using BlendoBotLib;
    using BlendoBotLib.Interfaces;
    using DSharpPlus.EventArgs;
    using GrammarBot;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class AutoCorrectCommand : ICommand
    {
        public AutoCorrectCommand(
            ILogger<AutoCorrectCommand> logger,
            IDiscordClient discordClient,
            IAutoCorrectProvider autoCorrectProvider)
        {
            this.logger = logger;
            this.discordClient = discordClient;
            this.autoCorrectProvider = autoCorrectProvider;
        }

        public string Name => "AutoCorrect";
        public string Description => "Performs autocorrect on a message";
        public string Author => "mozzarella";
        public string Version => "0.0.2";

        public static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            GrammarBotConfig? grammarBotConfig = hostContext.Configuration
                .GetSection("AutoCorrect")
                .GetSection("GrammarBot")
                .Get<GrammarBotConfig>();
            services.AddSingleton<GrammarBotConfig?>(grammarBotConfig);
            services.AddHttpClient<IAutoCorrectProvider, GrammarBotAutoCorrectProvider>();
        }

        public string GetUsage(string term) => $"Usage: {$"{term} <message>".Code()}";

        public async Task OnMessage(MessageCreateEventArgs e)
        {
            await e.Channel.TriggerTypingAsync().ConfigureAwait(false);

            string[] splitInput = e.Message.Content.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (splitInput.Length < 2)
            {
                await this.discordClient.SendMessage(this, new SendMessageEventArgs
                    {
                        Message = $"Too few arguments specified",
                        Channel = e.Channel,
                        LogMessage = "AutoCorrectTooFewArgs"
                    }).ConfigureAwait(false);
                return;
            }

            string inputMessage = splitInput[1];
            string correctedMessage = await this.autoCorrectProvider.CorrectAsync(inputMessage).ConfigureAwait(false);

            if (string.IsNullOrEmpty(correctedMessage))
            {
                // uh oh
                await this.discordClient.SendMessage(this, new SendMessageEventArgs
                    {
                        Message = $"Failed to autocorrect '{inputMessage}'",
                        Channel = e.Channel,
                        LogMessage = "AutoCorrectFailure"
                    }).ConfigureAwait(false);
                return;
            }

            string commandOutput = $"'{inputMessage}' autocorrected to '{correctedMessage}'";
            await this.discordClient.SendMessage(this, new SendMessageEventArgs
                {
                    Message = commandOutput,
                    Channel = e.Channel,
                    LogMessage = "AutoCorrectSuccess"
                }).ConfigureAwait(false);
        }

        private ILogger<AutoCorrectCommand> logger;

        private IDiscordClient discordClient;

        private IAutoCorrectProvider autoCorrectProvider;
    }
}

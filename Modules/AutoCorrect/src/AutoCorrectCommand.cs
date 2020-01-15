using System;
using System.Threading.Tasks;
using BlendoBotLib;
using DSharpPlus.EventArgs;

namespace AutoCorrect {
    public class AutoCorrectCommand : CommandBase, IDisposable
    {
        public AutoCorrectCommand(ulong guildId, IBotMethods botMethods) : base(guildId, botMethods) { }

        public override string Term => "?ac";
        public override string Name => "AutoCorrect";
        public override string Description => "Performs autocorrect on a message";
        public override string Usage => $"Usage: {"?ac <message>".Code()}";
        public override string Author => "mozzarella";
        public override string Version => "0.0.1";

        private string ApiKey {
            get {
                string key = BotMethods.ReadConfig(this, Name, "ApiKey");
                return key ?? string.Empty;
            }
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
                this.AutoCorrectProvider?.Dispose();
            }
        }

        public override async Task<bool> Startup()
        {
            // api key is optional, increases daily call limit from 100 to 250
            this.AutoCorrectProvider = new GrammarBotAutoCorrectProvider(BotMethods, ApiKey);
            return await Task.FromResult(true).ConfigureAwait(false);
        }

        public override async Task OnMessage(MessageCreateEventArgs e)
        {
            await e.Channel.TriggerTypingAsync().ConfigureAwait(false);

            string[] splitInput = e.Message.Content.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (splitInput.Length < 2)
            {
                await BotMethods.SendMessage(this, new SendMessageEventArgs
                    {
                        Message = $"Too few arguments specified to {"?ac".Code()}",
                        Channel = e.Channel,
                        LogMessage = "AutoCorrectTooFewArgs"
                    }).ConfigureAwait(false);
                return;
            }

            string inputMessage = splitInput[1];
            string correctedMessage = await this.AutoCorrectProvider.CorrectAsync(inputMessage).ConfigureAwait(false);

            if (string.IsNullOrEmpty(correctedMessage))
            {
                // uh oh
                await BotMethods.SendMessage(this, new SendMessageEventArgs
                    {
                        Message = $"Failed to autocorrect '{inputMessage}'",
                        Channel = e.Channel,
                        LogMessage = "AutoCorrectFailure"
                    }).ConfigureAwait(false);
                return;
            }

            string commandOutput = $"'{inputMessage}' autocorrected to '{correctedMessage}'";
            await BotMethods.SendMessage(this, new SendMessageEventArgs
                {
                    Message = commandOutput,
                    Channel = e.Channel,
                    LogMessage = "AutoCorrectSuccess"
                }).ConfigureAwait(false);
        }

        private IAutoCorrectProvider AutoCorrectProvider { get; set; }
    }
}
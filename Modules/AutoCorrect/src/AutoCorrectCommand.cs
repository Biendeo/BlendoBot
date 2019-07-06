using System;
using System.IO;
using System.Threading.Tasks;
using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;

namespace AutoCorrect
{
    public class AutoCorrectCommand : ICommand, IDisposable
    {

        private string ApiKey {
            get {
                string key = Methods.ReadConfig(this, Properties.Name, "ApiKey");
                return key ?? string.Empty;
            }
        }

        public CommandProps Properties => new CommandProps
            {
                Term = "?ac",
                Name = "AutoCorrect",
                Description = "Performs autocorrect on a message",
                Usage = $"Usage: {"?ac <message>".Code()}",
                Author = "mozzarella",
                Version = "0.0.1",
                Startup = this.Startup,
                OnMessage = this.Execute
            };

        public void Dispose()
        {
            this.AutoCorrectProvider?.Dispose();
        }

        private async Task<bool> Startup()
        {
            // api key is optional, increases daily call limit from 100 to 250
            this.AutoCorrectProvider = new GrammarBotAutoCorrectProvider(ApiKey);
            return await Task.FromResult(true).ConfigureAwait(false);
        }

        private async Task Execute(MessageCreateEventArgs e)
        {
            await e.Channel.TriggerTypingAsync().ConfigureAwait(false);

            var splitInput = e.Message.Content.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (splitInput.Length < 2)
            {
                await Methods.SendMessage(null, new SendMessageEventArgs
                    {
                        Message = $"Too few arguments specified to {"?ac".Code()}",
                        Channel = e.Channel,
                        LogMessage = "AutoCorrectTooFewArgs"
                    }).ConfigureAwait(false);
                return;
            }

            var inputMessage = splitInput[1];
            var correctedMessage = await this.AutoCorrectProvider.CorrectAsync(inputMessage).ConfigureAwait(false);

            if (string.IsNullOrEmpty(correctedMessage))
            {
                // uh oh
                await Methods.SendMessage(null, new SendMessageEventArgs
                    {
                        Message = $"Failed to autocorrect '{inputMessage}'",
                        Channel = e.Channel,
                        LogMessage = "AutoCorrectFailure"
                    }).ConfigureAwait(false);
                return;
            }

            var commandOutput = $"'{inputMessage}' autocorrected to '{correctedMessage}'";
            await Methods.SendMessage(null, new SendMessageEventArgs
                {
                    Message = commandOutput,
                    Channel = e.Channel,
                    LogMessage = "AutoCorrectSuccess"
                }).ConfigureAwait(false);
        }

        private IAutoCorrectProvider AutoCorrectProvider { get; set; }
    }
}
namespace BlendoBot.Commands
{
    using BlendoBot.CommandDiscovery;
    using BlendoBotLib;
    using BlendoBotLib.Interfaces;
    using DSharpPlus.EventArgs;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// The help command, which simply prints out the <see cref="CommandProps.Usage"/> property of a <see cref="ICommand"/>, or
    /// </summary>
	[PrivilegedCommand]
    internal class Help : ICommand
    {
        public string Name => "Help";
        public string Description => "Posts what commands this bot can do, and additional help on how to use a command.";
        public string GetUsage(string term) => $"Use {term.Code()} to see a list of all commands on the server.\nUse {$"{term} [command]".Code()} to see help on a specific command, but you probably already know how to do that!";
        public string Author => "Biendeo";
        public string Version => "1.5.0";

        public Help(
            Guild guild,
            ICommandRegistry commandRegistry,
            ICommandRouter commandRouter,
            IDiscordClient discordClient)
        {
            this.guildId = guild.Id;
            this.commandRegistry = commandRegistry;
            this.commandRouter = commandRouter;
            this.discordClient = discordClient;
        }

        public async Task OnMessage(MessageCreateEventArgs e)
        {
            // The help command definitely prints out a string. Which string will be determined by the arguments.
            var sb = new StringBuilder();

            if (!e.Message.Content.Contains(' '))
            {
                // This block runs if the ?help is run with no arguments (fortunately Discord trims whitespace).
                // All the commands are iterated through and their terms are printed out so the user knows which
                // commands are available.
                sb.AppendLine($"Use {$"{e.Message.Content} [command]".Code()} for specific help.");
                sb.AppendLine("List of available commands:");
                foreach (var term in this.commandRouter.GetEnabledTerms())
                {
                    sb.AppendLine(term.Code());
                }
                await this.discordClient.SendMessage(this, new SendMessageEventArgs
                {
                    Message = sb.ToString(),
                    Channel = e.Channel,
                    LogMessage = "HelpGeneric"
                });
            }
            else
            {
                // This block runs if the ?help is run with an argument. The relevant command is searched and its usage
                // is printed out, or an error message if that command doesn't exist.
                string specifiedCommand = e.Message.Content.Split(' ')[1];
                if (!(this.commandRouter.TryTranslateTerm(specifiedCommand, out var commandType) &&
                    this.commandRegistry.TryGetCommandInstance(commandType, this.guildId, out var command)))
                {
                    await this.discordClient.SendMessage(this, new SendMessageEventArgs
                    {
                        Message = $"No command called {specifiedCommand.Code()}",
                        Channel = e.Channel,
                        LogMessage = "AboutErrorInvalidCommand"
                    });
                }
                else
                {
                    sb.AppendLine($"Help for {specifiedCommand.Code()}:");
                    var usageStr = command.GetUsage(specifiedCommand);
                    if (!string.IsNullOrEmpty(usageStr))
                    {
                        sb.AppendLine(usageStr);
                    }
                    else
                    {
                        sb.AppendLine("No help found for this command".Italics());
                    }
                    await this.discordClient.SendMessage(this, new SendMessageEventArgs
                    {
                        Message = sb.ToString(),
                        Channel = e.Channel,
                        LogMessage = "HelpSpecific"
                    });
                }
            }
        }

        public readonly ulong guildId;
        private readonly ICommandRegistry commandRegistry;
        private readonly ICommandRouter commandRouter;
        private readonly IDiscordClient discordClient;
    }
}

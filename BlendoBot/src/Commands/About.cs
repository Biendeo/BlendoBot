namespace BlendoBot.Commands
{
	using BlendoBot.CommandDiscovery;
	using BlendoBot.ConfigSchemas;
	using BlendoBotLib;
	using BlendoBotLib.Interfaces;
	using DSharpPlus.EventArgs;
	using System;
	using System.Text;
	using System.Threading.Tasks;

    /// <summary>
    /// The about command, which simply prints out the <see cref="CommandProps.Description"/> property of a <see cref="ICommand"/>, or on its own details about the bot.
    /// </summary>
	[PrivilegedCommand]
    internal class About : ICommand
    {
        public About(
			Guild guild,
            BlendoBotConfig botConfig,
			ICommandRouter commandRouter,
			ICommandRegistry commandRegistry,
            IDiscordClient discordClient)
        {
			this.guildId = guild.Id;
            this.botConfig = botConfig;
            this.commandRouter = commandRouter;
            this.commandRegistry = commandRegistry;
            this.discordClient = discordClient;
            this.startTime = DateTimeOffset.UtcNow;
        }

        public string Name => "About";
        public string Description => "Posts information about this version of the bot, or of any loaded module. You probably already know how to use this command by now.";
        public string GetUsage(string term) => $"Use {term.Code()} to see the information about the bot.\nUse {$"{term} [command]".Code()} to see information about another command.";
        public string Author => "Biendeo";
        public string Version => "1.5.0";

        public async Task OnMessage(MessageCreateEventArgs e)
        {
            // The about command definitely prints out a string. Which string will be determined by the arguments.
            var sb = new StringBuilder();

            if (!e.Message.Content.Contains(' '))
            {
                // This block runs if the ?about is run with no arguments (fortunately Discord trims whitespace). Simply
                // print out a message.
                sb.AppendLine($"{this.botConfig.Name} {this.botConfig.Version} ({this.botConfig.Description}) by {this.botConfig.Author}");
                sb.AppendLine($"Been running for {(DateTime.Now - this.startTime).Days} days, {(DateTime.Now - this.startTime).Hours} hours, {(DateTime.Now - this.startTime).Minutes} minutes, and {(DateTime.Now - this.startTime).Seconds} seconds.");
                await this.discordClient.SendMessage(this, new SendMessageEventArgs
                {
                    Message = sb.ToString(),
                    Channel = e.Channel,
                    LogMessage = "About"
                });
            }
            else
            {
                // This block runs if the ?about is run with an argument. Take the remaining length of the string and
                // figure out which command uses that. Then print their name, version, author, and description.
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
                    sb.AppendLine($"{command.Name.Bold()} ({command.Version?.Italics()}) by {command.Author?.Italics()}");
                    sb.AppendLine(command.Description);
                    await this.discordClient.SendMessage(this, new SendMessageEventArgs
                    {
                        Message = sb.ToString(),
                        Channel = e.Channel,
                        LogMessage = "AboutSpecific"
                    });
                }
            }
        }

		private ulong guildId;
        private BlendoBotConfig botConfig;
        private readonly ICommandRouter commandRouter;
        private readonly ICommandRegistry commandRegistry;
        private IDiscordClient discordClient;
        private DateTimeOffset startTime;

    }
}

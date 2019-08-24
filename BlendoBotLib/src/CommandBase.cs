using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBotLib {
	public abstract class CommandBase {
		public CommandBase(ulong guildId, IBotMethods botMethods) {
			GuildId = guildId;
			BotMethods = botMethods;
		}

		/// <summary>
		/// The guild ID that this command reacts to. This is useful for commands to load persistent memory related to
		/// a specific guild.
		/// </summary>
		public ulong GuildId { get; }

		/// <summary>
		/// References delegated functions that commands can use to interact with Discord and the program.
		/// </summary>
		public IBotMethods BotMethods { get; }

		/// <summary>
		/// The string that users will need to type in order to access this command.
		/// </summary>
		public abstract string Term { get; }

		/// <summary>
		/// The user-friendly name for this command. Appears in help.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// A description of this command. Appears in help.
		/// </summary>
		public abstract string Description { get; }

		/// <summary>
		/// A string representing the typical usage of the command. Appears in help.
		/// </summary>
		public abstract string Usage { get; }

		/// <summary>
		/// The author of the command. Appears in about.
		/// </summary>
		public abstract string Author { get; }

		/// <summary>
		/// The version of the command. Appears in about.
		/// </summary>
		public abstract string Version { get; }

		/// <summary>
		/// The functions that should setup this command. This is very useful for commands that require
		/// some persistent memory across usages. The return determines whether the startup was
		/// successful or not. If it is unsuccessful, the command will not be added to the bot.
		/// </summary>
		public abstract Task<bool> Startup();

		/// <summary>
		/// The function that handles this command. Since all commands are made by creating an
		/// event, all command handles must forward the MessageCreateEventArgs from when the
		/// message was received. They're also async, so they'll need to return Task.
		/// </summary>
		public abstract Task OnMessage(MessageCreateEventArgs e);
	}
}

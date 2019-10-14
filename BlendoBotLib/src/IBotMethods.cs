using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBotLib {
	public interface IBotMethods {
		/// <summary>
		/// Sends a message given a source object (for debugging) and an args object.
		/// </summary>
		Task<DiscordMessage> SendMessage(object o, SendMessageEventArgs e);

		/// <summary>
		/// Sends a file given a source object (for debugging) and an args object.
		/// </summary>
		Task<DiscordMessage> SendFile(object o, SendFileEventArgs e);

		/// <summary>
		/// Sends an exception given a source object (for debugging) and an args object.
		/// </summary>
		Task<DiscordMessage> SendException(object o, SendExceptionEventArgs e);

		/// <summary>
		/// Logs a message given a source object (for debugging) and an args object.
		/// </summary>
		void Log(object o, LogEventArgs e);

		/// <summary>
		/// Reads a string from the config given a source object (for debugging), a header name of the config section
		/// and a given key. The result is null if the key does not exist, either because the section does not exist,
		/// or whether the specific key does not exist.
		/// </summary>
		string ReadConfig(object o, string configHeader, string configKey);

		/// <summary>
		/// Returns whether a key exists in the config or not. This is useful for commands that wish to provide a
		/// default value whenever a value does not exist in the config.
		/// </summary>
		bool DoesKeyExist(object o, string configHeader, string configKey);

		/// <summary>
		/// Writes a string to the config given a source object (for debugging), a given key/value pair and the
		/// header name of the config section.
		/// </summary>
		void WriteConfig(object o, string configHeader, string configKey, string configValue);

		/// <summary>
		/// Adds a message listener to the program for this command.
		/// </summary>
		void AddMessageListener(object o, ulong guildId, IMessageListener messageListener);

		/// <summary>
		/// Removes a message listener from the program for this command.
		/// </summary>
		void RemoveMessageListener(object o, ulong guildId, IMessageListener messageListener);

		/// <summary>
		/// Gets an instance of a command for the given guildId.
		/// </summary>
		T GetCommand<T>(object o, ulong guildId) where T : CommandBase;

		/// <summary>
		/// Returns a path that this command can use to store persistent data. The command should give this particular
		/// instance a unique path, and the path should exist after this call.
		/// </summary>
		string GetCommandInstanceDataPath(object o, CommandBase command);

		/// <summary>
		/// Returns a path that this command can use to store persistent data. The command should give this particular
		/// instance the same path as every other instance of this command, and the path should exist after this call.
		/// </summary>
		string GetCommandCommonDataPath(object o, CommandBase command);

		/// <summary>
		/// Returns whether a user for a given guild is an admin. This is true if either the user is a guild admin in
		/// Discord, or if they've been manually granted admin.
		/// </summary>
		Task<bool> IsUserAdmin(object o, DiscordGuild guild, DiscordChannel channel, DiscordUser user);
	}
}

using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBotLib {
	/// <summary>
	/// A static class that is accessible by everything, allowing modules to call important functions.
	/// </summary>
	public static class Methods {
		public delegate Task<DiscordMessage> MessageSent(object o, SendMessageEventArgs e);
		public delegate Task<DiscordMessage> FileSent(object o, SendFileEventArgs e);
		public delegate Task<DiscordMessage> ExceptionSent(object o, SendExceptionEventArgs e);
		public delegate void MessageLogged(object o, LogEventArgs e);
		//? Is there an easy way of using generics here? It seems like it has to do the whole class instead.
		public delegate string ConfigRead(object o, string configHeader, string configKey);
		public delegate void ConfigWritten(object o, string configHeader, string configKey, string configValue);

		/// <summary>
		/// Sends a message given a source object (for debugging) and an args object.
		/// </summary>
		public static MessageSent SendMessage;

		/// <summary>
		/// Sends a file given a source object (for debugging) and an args object.
		/// </summary>
		public static FileSent SendFile;

		/// <summary>
		/// Sends an exception given a source object (for debugging) and an args object.
		/// </summary>
		public static ExceptionSent SendException;

		/// <summary>
		/// Logs a message given a source object (for debugging) and an args object.
		/// </summary>
		public static MessageLogged Log;

		/// <summary>
		/// Reads a string from the config given a source object (for debugging), a header name of the config section
		/// and a given key.
		/// </summary>
		public static ConfigRead ReadConfig;

		/// <summary>
		/// Writes a string to the config given a source object (for debugging), a given key/value pair and the
		/// header name of the config section.
		/// </summary>
		public static ConfigWritten WriteConfig;
	}
}

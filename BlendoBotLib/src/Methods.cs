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
	}
}

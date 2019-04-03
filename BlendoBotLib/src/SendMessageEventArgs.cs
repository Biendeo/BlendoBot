using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlendoBotLib {
	/// <summary>
	/// An object that contains various arguments involved with sending a message.
	/// </summary>
	public class SendMessageEventArgs : EventArgs {
		/// <summary>
		/// The message content itself.
		/// </summary>
		public string Message { get; set; }
		/// <summary>
		/// The channel to send this message to. This is often the channel that sent the command that raised this event.
		/// </summary>
		public DiscordChannel Channel { get; set; }
		/// <summary>
		/// The type of message sent. This can be given a useful name for quick debugging.
		/// </summary>
		public string LogMessage { get; set; }
	}
}

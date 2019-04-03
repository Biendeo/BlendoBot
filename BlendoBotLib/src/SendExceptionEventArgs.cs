using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlendoBotLib {
	/// <summary>
	/// An object that contains various arguments involved with an exception event.
	/// </summary>
	public class SendExceptionEventArgs : EventArgs {
		/// <summary>
		/// The exception itself.
		/// </summary>
		public Exception Exception { get; set; }
		/// <summary>
		/// A channel to alert in case an exception is raised. This is often the channel that sent the command that
		/// raised this event.
		/// </summary>
		public DiscordChannel Channel { get; set; }
		/// <summary>
		/// The type of exception. This can be given a useful name for quick debugging.
		/// </summary>
		public string LogExceptionType { get; set; }
	}
}

using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBotLib.Commands {
	/// <summary>
	/// A structure which lets you determine properties for a command. These should be stored in
	/// availableCommands and only referred to otherwise.
	/// </summary>
	public struct CommandProps {
		/// <summary>
		/// The command that users will need to type in order to access this command.
		/// </summary>
		public string Term { get; set; }
		/// <summary>
		/// The user-friendly name for this command. Appears in help.
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// A description of this command. Appears in help.
		/// </summary>
		public string Description { get; set; }
		/// <summary>
		/// The functions that should setup this function. This is very useful for commands that require
		/// some persistent memory across usages. The return determines whether the startup was
		/// successful or not. If it is unsuccessful, the module will not be added to the list.
		/// </summary>
		public Func<Task<bool>> Startup { get; set; }
		/// <summary>
		/// The function that handles this command. Since all commands are made by creating an
		/// event, all command handles must forward the MessageCreateEventArgs from when the
		/// message was received. They're also async, so they'll need to return Task.
		/// </summary>
		public Func<MessageCreateEventArgs, Task> OnMessage { get; set; }
		/// <summary>
		/// A string representing the typical usage of the command. Appears in help.
		/// </summary>
		public string Usage { get; set; }
		/// <summary>
		/// The author of the command. Appears in about.
		/// </summary>
		public string Author { get; set; }
		/// <summary>
		/// The version of the command. Appears in about.
		/// </summary>
		public string Version { get; set; }
	}
}

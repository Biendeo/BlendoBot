using BlendoBotLib.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlendoBotLib {
	/// <summary>
	/// The public interface for a command. All commands must implement this.
	/// </summary>
	public interface ICommand {
		/// <summary>
		/// The properties of a command. This is used by several internal components to know what to call, what the
		/// command is, etc.
		/// </summary>
		CommandProps Properties { get; }
	}
}

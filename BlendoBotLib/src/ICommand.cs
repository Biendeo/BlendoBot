using BlendoBotLib.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlendoBotLib {
	public interface ICommand {
		CommandProps Properties { get; }
	}
}

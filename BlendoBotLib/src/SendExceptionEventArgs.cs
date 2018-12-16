using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlendoBotLib {
	public class SendExceptionEventArgs : EventArgs {
		public Exception Exception { get; set; }
		public DiscordChannel Channel { get; set; }
		public string LogExceptionType { get; set; }
	}
}

using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlendoBotLib {
	public class SendFileEventArgs : EventArgs {
		public string FilePath { get; set; }
		public DiscordChannel Channel { get; set; }
		public string LogMessage { get; set; }
	}
}

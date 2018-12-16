using System;
using System.Collections.Generic;
using System.Text;

namespace BlendoBotLib {
	public enum LogType {
		Log,
		Warning,
		Error,
		Critical
	}

	public class LogEventArgs {
		public LogType Type { get; set; }
		public string Message { get; set; }
	}
}

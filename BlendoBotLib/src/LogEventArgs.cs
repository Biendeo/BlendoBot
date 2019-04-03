﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BlendoBotLib {
	/// <summary>
	/// Various types to help specify between log messages.
	/// </summary>
	public enum LogType {
		Log,
		Warning,
		Error,
		Critical
	}

	/// <summary>
	/// A log event consists of a log type and a message. This encapsulates both.
	/// </summary>
	public class LogEventArgs {
		public LogType Type { get; set; }
		public string Message { get; set; }
	}
}

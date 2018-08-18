using System;

namespace BlendoBot {
	public enum LogType {
		Log,
		Warning,
		Error,
		Critical
	}

	public static class Log {
		public static void LogMessage(LogType type, string message) {
			//? I dunno why but I can't one-line this.
			string typeString = Enum.GetName(typeof(LogType), type);
			Console.WriteLine($"[{typeString}] ({DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}) | {message}");
		}
	}
}

using System;
using System.IO;

namespace BlendoBot {
	public enum LogType {
		Log,
		Warning,
		Error,
		Critical
	}

	public static class Log {
		private static readonly string logFile = Path.Join("log", $"{DateTime.Now.ToString("yyyyMMddHHmmss")}.log");

		public static void LogMessage(LogType type, string message) {
			//? I dunno why but I can't one-line this.
			string typeString = Enum.GetName(typeof(LogType), type);
			string logMessage = $"[{typeString}] ({DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}) | {message}";
			Console.WriteLine(logMessage);
			if (!Directory.Exists("log")) Directory.CreateDirectory("log");
			File.AppendAllText(logFile, logMessage + "\n");
		}
	}
}

using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBotLib {
	public static class Methods {
		/*
		public static event EventHandler<SendMessageEventArgs> MessageSent;
		public static event EventHandler<SendFileEventArgs> FileSent;
		public static event EventHandler<SendExceptionEventArgs> ExceptionSent;
		public static event EventHandler<LogEventArgs> MessageLogged;

		public static async Task<DiscordMessage> SendMessage(SendMessageEventArgs e) {
			MessageSent?.Invoke(null, e);
			return await e.DiscordMessage;
		}

		public static async Task<DiscordMessage> SendFile(SendFileEventArgs e) {
			FileSent?.Invoke(null, e);
		}

		public static async Task<DiscordMessage> SendException(SendExceptionEventArgs e) {
			ExceptionSent?.Invoke(null, e);
		}

		public static void Log(LogEventArgs e) {
			MessageLogged?.Invoke(null, e);
		}
		*/

		public delegate Task<DiscordMessage> MessageSent(object o, SendMessageEventArgs e);
		public delegate Task<DiscordMessage> FileSent(object o, SendFileEventArgs e);
		public delegate Task<DiscordMessage> ExceptionSent(object o, SendExceptionEventArgs e);
		public delegate void MessageLogged(object o, LogEventArgs e);

		public static MessageSent SendMessage;
		public static FileSent SendFile;
		public static ExceptionSent SendException;
		public static MessageLogged Log;
	}
}

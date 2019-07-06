using BlendoBotLib;
using BlendoBotLib.MessageListeners;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MrPing {
	public class MrPingListener : IMessageListener {
		MessageListenerProps IMessageListener.Properties => properties;

		private static readonly MessageListenerProps properties = new MessageListenerProps {
			Name = "Mr. Ping Listener",
			Description = "Watches messages to make progress for existing Mr Ping challenges.",
			Author = "Biendeo",
			Version = "1.0.0",
			Startup = async () => { await Task.Delay(0); return true; },
			OnMessage = OnMessage
		};

		public static async Task OnMessage(MessageCreateEventArgs e) {
			if (MrPing.Database != null) {
				if (e.MentionedUsers.Count == 1) {
					await MrPing.Database.PingUser(e.MentionedUsers[0], e.Author, e.Guild, e.Channel);
				}
			}
		}
	}
}

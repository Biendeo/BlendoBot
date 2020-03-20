using BlendoBotLib;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace MrPing {
	public class MrPingListener : IMessageListener {
		private readonly MrPing mrPing;

		public MrPingListener(MrPing mrPing) {
			this.mrPing = mrPing;
		}

		public CommandBase Command { get { return mrPing; } }

		public async Task OnMessage(MessageCreateEventArgs e) {
			if (mrPing.Database != null) {
				if (e.MentionedUsers.Count == 1) {
					await mrPing.Database.PingUser(e.MentionedUsers[0], e.Author, e.Channel);
				}
			}
		}
	}
}

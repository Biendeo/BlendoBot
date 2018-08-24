using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands.Admin {
	public static class Allow {
		public static async Task AllowCommand(MessageCreateEventArgs e) {
			if (e.Message.Content.Split(' ').Length <= 2) {
				//TODO: Print maybe who's in the list?
				await Program.SendMessage($"Please tag some users to allow them!", e.Channel, "AdminAllowTooFewArguments");
				return;
			}

			var sb = new StringBuilder();

			var actedUsers = new List<DiscordUser>();
			var alreadyUsers = new List<DiscordUser>();

			foreach (var user in e.MentionedUsers) {
				if (!Program.Data.IsUserVerified(e.Guild, user)) {
					actedUsers.Add(user);
					Program.Data.AllowUser(e.Guild, user);
				} else {
					alreadyUsers.Add(user);
				}
			}

			Program.Data.Save();
			if (actedUsers.Count > 0) {
				sb.Append("Allowed users:");
				foreach (var user in actedUsers) {
					sb.Append($" {user.Mention}");
				}
			}
			sb.AppendLine();
			if (alreadyUsers.Count > 0) {
				sb.Append("Users already allowed:");
				foreach (var user in alreadyUsers) {
					sb.Append($" {user.Mention}");
				}
			}

			await Program.SendMessage(sb.ToString(), e.Channel, "AdminAllow");
		}
	}
}

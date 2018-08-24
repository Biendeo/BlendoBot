using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands.Admin {
	public static class Disallow {
		public static async Task DisallowCommand(MessageCreateEventArgs e) {
			if (e.Message.Content.Split(' ').Length <= 2) {
				//TODO: Print maybe who's in the list?
				await Program.SendMessage($"Please tag some users to disallow them!", e.Channel, "AdminDisallowTooFewArguments");
				return;
			}

			var sb = new StringBuilder();

			var actedUsers = new List<DiscordUser>();
			var alreadyUsers = new List<DiscordUser>();
			var adminUsers = new List<DiscordUser>();

			foreach (var user in e.MentionedUsers) {
				if (Program.Data.IsUserVerified(e.Guild, user)) {
					if (Program.Data.IsUserAdmin(e.Guild, user)) {
						adminUsers.Add(user);
					} else {
						actedUsers.Add(user);
						Program.Data.DisallowUser(e.Guild, user);
					}
				} else {
					alreadyUsers.Add(user);
				}
			}

			Program.Data.Save();
			if (actedUsers.Count > 0) {
				sb.Append("Disallowed users:");
				foreach (var user in actedUsers) {
					sb.Append($" {user.Mention}");
				}
			}
			sb.AppendLine();
			if (alreadyUsers.Count > 0) {
				sb.Append("Users already disallowed:");
				foreach (var user in alreadyUsers) {
					sb.Append($" {user.Mention}");
				}
			}
			sb.AppendLine();
			if (adminUsers.Count > 0) {
				sb.Append("Admins that cannot be revoked:");
				foreach (var user in adminUsers) {
					sb.Append($" {user.Mention}");
				}
			}

			await Program.SendMessage(sb.ToString(), e.Channel, "AdminDisallow");
		}
	}
}

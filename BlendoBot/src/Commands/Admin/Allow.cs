using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands.Admin {
	public static class Allow {
		public static readonly CommandProps Properties = new CommandProps {
			Term = "?admin allow",
			Name = "Allow",
			Description = "Allows a specified user to interact with disabled commands.\nUsage: ?admin allow [@users ...]",
			Func = AllowCommand
		};

		public static async Task AllowCommand(MessageCreateEventArgs e) {
			if (e.Message.Content.Split(' ').Length <= 2) {
				//TODO: Print maybe who's in the list?
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = $"Please tag some users to allow them!",
					Channel = e.Channel,
					LogMessage = "AdminAllowTooFewArguments"
				});
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

			await Methods.SendMessage(null, new SendMessageEventArgs {
				Message = sb.ToString(),
				Channel = e.Channel,
				LogMessage = "AdminAllow"
			});
		}
	}
}

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BlendoBot.Commands {
	public static class MrPing {
		public static async Task MrPingCommand(MessageCreateEventArgs e) {
			// Edit the Mr Ping image to randomly pick a user on the server, and a random number
			// of pings (up to 100).

			// First, choose a user from the server.
			var random = new Random();
			// Purge the list of anyone not valid:
			var filteredMembers = new List<DiscordMember>();
			foreach (var member in e.Channel.Users) {
				// Apparently your presence is null if you're offline, so that needs to be a check.
				if (!member.IsBot && member.Presence != null && (member.Presence.Status == UserStatus.Online || member.Presence.Status == UserStatus.Idle) && await DoesUserHaveChannelPermissions(member, e.Channel, Permissions.ReadMessageHistory)) {
					filteredMembers.Add(member);
				}
			}

			if (filteredMembers.Count == 0) {
				await Program.SendMessage($"No one is available for the Mr. Ping Challenge. 👀", e.Channel, "MrPingErrorNoUsers");
				return;
			}

			// Let's randomly pick someone from those filtered members.
			var chosenMember = filteredMembers[(int)(random.NextDouble() * filteredMembers.Count)];

			// A random number from 1 to 100 will be chosen.
			int numberOfPings = (int)(random.NextDouble() * 99 + 1);

			// Now to do the image modification.
			using (var image = Image.Load(Resources.ReadImage("mr.png"))) {
				//? It seems that memory goes up after multiple usages of this. Am I leaking something?
				Font memberNameFont = SystemFonts.CreateFont("Arial", 25);
				Font numberFont = SystemFonts.CreateFont("Arial", 35);
				using (var workingImage = image.Clone()) {
					var textGraphicsOptions = new TextGraphicsOptions(true) {
						ApplyKerning = true,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center,
						WrapTextWidth = 175.0f
					};
					workingImage.Mutate(ctx => ctx.DrawText(textGraphicsOptions, $"@{chosenMember.Username} #{chosenMember.Discriminator}", memberNameFont, Rgba32.DarkBlue, new PointF(0, 290)).DrawText(textGraphicsOptions, $"{numberOfPings}", numberFont, Rgba32.DarkRed, new PointF(-45, 357)));

					string filePath = $"mrping-{chosenMember.Username}.png";
					workingImage.Save(filePath);

					//? Is this necessary because I'm using using?
					workingImage.Dispose();

					await Program.SendFile(filePath, e.Channel, "MrPingFileSuccess");

					if (File.Exists(filePath)) {
						File.Delete(filePath);
					}
				}
			}
		}

		//? I would love to make this not async, can it be done?
		private static async Task<bool> DoesUserHaveChannelPermissions(DiscordMember member, DiscordChannel channel, Permissions permissions) {
			var memberRoles = new List<DiscordRole>(member.Roles);
			foreach (var permOverwrite in channel.PermissionOverwrites) {
				// Check whether the permission overwrite is a role-based or user-based overwrite.
				if (permOverwrite.Type == OverwriteType.Role) {
					// Check if the user contains this role.
					DiscordRole role = await permOverwrite.GetRoleAsync();
					if (memberRoles.Exists((DiscordRole d) => { return d == role; })) {
						// If the permission explicitly says you can/cannot do this permission, return that.
						if (permOverwrite.Allowed.HasPermission(permissions)) {
							return true;
						} else if (permOverwrite.Denied.HasPermission(permissions)) {
							return false;
						}
					}
				} else if (permOverwrite.Type == OverwriteType.Member) {
					DiscordMember roleMember = await permOverwrite.GetMemberAsync();
					if (member.Equals(roleMember)) {
						if (permOverwrite.Allowed.HasPermission(permissions)) {
							return true;
						} else if (permOverwrite.Denied.HasPermission(permissions)) {
							return false;
						}
					}
				}
			}
			// If we've hit here, that means the channel has zero role overrides that match both of these.
			// The user must have a role that enables this feature then.
			foreach (var role in memberRoles) {
				if (role.Permissions.HasPermission(permissions)) {
					return true;
				}
			}

			// Finally, we need to check the @everyone role.
			return channel.Guild.EveryoneRole.Permissions.HasPermission(permissions);
		}
	}
}

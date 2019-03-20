using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MrPing.Properties;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MrPing {
	public class MrPing : ICommand {
		CommandProps ICommand.Properties => properties;

		private static readonly CommandProps properties = new CommandProps {
			Term = "?mrping",
			Name = "Mr. Ping Challenge",
			Description = "Subjects someone to the Mr. Ping Challenge!",
			Usage = $"Usage: {"?mrping".Code()}\nNote: Any non-ASCII characters in a username will be replaced with {"¿".Code()}.",
			Author = "Biendeo",
			Version = "0.4.2",
			Startup = async () => { await Task.Delay(0); return true; },
			OnMessage = MrPingCommand
		};

		public const int MaxPings = 100;
		
		public static async Task MrPingCommand(MessageCreateEventArgs e) {
			// Edit the Mr Ping image to randomly pick a user on the server, and a random number
			// of pings (up to 100).

			DiscordMessage waitingMessage = await Methods.SendMessage(null, new SendMessageEventArgs {
				Message = "Randomly choosing a victim...",
				Channel = e.Channel,
				LogMessage = "MrPingWaiting"
			});

			// First, choose a user from the server.
			var random = new Random();
			// Purge the list of anyone not valid:
			var filteredMembers = new List<DiscordMember>();
			foreach (var member in e.Channel.Users) {
				// Apparently your presence is null if you're offline, so that needs to be a check.
				//! A previous version had an additional check to see if a user could read this channel.
				//! Later reading of the e.Channel.Users property indicates that's already sorted out.
				if (!member.IsBot && member.Presence != null && (member.Presence.Status == UserStatus.Online || member.Presence.Status == UserStatus.Idle)) {
					filteredMembers.Add(member);
				}
			}

			if (filteredMembers.Count == 0) {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = "No one is available for the Mr. Ping Challenge. 👀",
					Channel = e.Channel,
					LogMessage = "MrPingErrorNoUsers"
				});
				await waitingMessage.DeleteAsync();
				return;
			}

			// Let's randomly pick someone from those filtered members.
			var chosenMember = filteredMembers[(int)(random.NextDouble() * filteredMembers.Count)];

			// A random number from 1 to 100 will be chosen.
			int numberOfPings = (int)(random.NextDouble() * MaxPings + 1);

			// Now to do the image modification.
			//TODO: Figure out how to use a Resource on this, on ubuntu, the resource is interpreted
			// as the RESX string rather than a byte array, which doesn't work. Any fixes?
			using (var image = Image.Load(@"Modules/MrPing/res/mr.png")) {
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
					string cleanUsername = Regex.Replace(chosenMember.Username, @"[^\u0000-\u007F]+", "¿");
					workingImage.Mutate(ctx => ctx.DrawText(textGraphicsOptions, $"@{cleanUsername} #{chosenMember.Discriminator}", memberNameFont, Rgba32.DarkBlue, new PointF(0, 290)).DrawText(textGraphicsOptions, $"{numberOfPings}", numberFont, Rgba32.DarkRed, new PointF(-45, 357)));

					string filePath = $"mrping-{chosenMember.Username}.png";
					workingImage.Save(filePath);

					//? Is this necessary because I'm using using?
					workingImage.Dispose();

					await Methods.SendFile(null, new SendFileEventArgs {
						Channel = e.Channel,
						FilePath = filePath,
						LogMessage = "MrPingFileSuccess"
					});

					await waitingMessage.DeleteAsync();

					if (File.Exists(filePath)) {
						File.Delete(filePath);
					}
				}
			}
			await Task.Delay(0);
		}

		//? This is really slow and probably scales poorly.
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

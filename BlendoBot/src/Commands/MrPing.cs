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
			var members = new List<DiscordMember>(await e.Guild.GetAllMembersAsync());
			var random = new Random();
			// Purge the list of anyone not valid:
			var filteredMembers = new List<DiscordMember>();
			foreach (var member in members) {
				// Apparently your presence is null if you're offline, so that needs to be a check.
				if (e.Channel.PermissionsFor(member).HasFlag(DSharpPlus.Permissions.ReadMessageHistory) && !member.IsBot && member.Presence != null && (member.Presence.Status == UserStatus.Online || member.Presence.Status == UserStatus.Idle)) {
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
	}
}

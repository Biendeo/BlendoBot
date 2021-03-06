﻿using BlendoBotLib;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MrPing.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MrPing {
	public class MrPing : CommandBase {
		public MrPing(ulong guildId, IBotMethods botMethods) : base(guildId, botMethods) { }

		public override string DefaultTerm => "?mrping";
		public override string Name => "Mr. Ping Challenge";
		public override string Description => "Subjects someone to the Mr. Ping Challenge!";
		public override string Usage => $"{Term.Code()} ({"Creates a new Mr Ping challenge for a random victim".Italics()})\n{$"{Term} list".Code()} ({"Prints a list of all outstanding challenges".Italics()})\n{$"{Term} stats".Code()} ({"Posts some neat stats about the challenge".Italics()})";
		public override string Author => "Biendeo";
		public override string Version => "1.0.0";

		internal Database Database;

		public override async Task<bool> Startup() {
			if (Database == null) {
				Database = new Database(this, BotMethods);
			}

			BotMethods.AddMessageListener(this, GuildId, new MrPingListener(this));

			await Task.Delay(0);
			return true;
		}

		public const int MaxPings = 100;

		public override async Task OnMessage(MessageCreateEventArgs e) {
			string[] splitString = e.Message.Content.Split(' ');
			if (splitString.Length == 1) {
				// Edit the Mr Ping image to randomly pick a user on the server, and a random number
				// of pings (up to 100).

				//! New change, BlendoBot will appear as if it's typing in the channel while it's waiting.
				await e.Channel.TriggerTypingAsync();

				// First, choose a user from the server.
				var random = new Random();
				// Purge the list of anyone not valid:
				var filteredMembers = new List<DiscordMember>();
				foreach (var member in e.Channel.Users) {
					// Apparently your presence is null if you're offline, so that needs to be a check.
					//! A previous version had an additional check to see if a user could read this channel.
					//! Later reading of the e.Channel.Users property indicates that's already sorted out.
					if (!member.IsBot && member.Presence != null && (member.Presence.Status == UserStatus.Online || member.Presence.Status == UserStatus.Idle) && member.PermissionsIn(e.Channel).HasPermission(Permissions.SendMessages)) {
						filteredMembers.Add(member);
					}
				}

				if (filteredMembers.Count == 0) {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = "No one is available for the Mr. Ping Challenge. 👀",
						Channel = e.Channel,
						LogMessage = "MrPingErrorNoUsers"
					});
					//await waitingMessage.DeleteAsync();
					return;
				}

				// Let's randomly pick someone from those filtered members.
				var chosenMember = filteredMembers[(int)(random.NextDouble() * filteredMembers.Count)];

				// A random number from 1 to 100 will be chosen.
				int numberOfPings = (int)(random.NextDouble() * MaxPings + 1);

				// Now to do the image modification.
				//TODO: Figure out how to use a Resource on this, on ubuntu, the resource is interpreted
				// as the RESX string rather than a byte array, which doesn't work. Any fixes?
				using var image = Image.FromFile(@"Modules/MrPing/res/mr.png");
				using var wc = new WebClient();
				byte[] avatarBytes = wc.DownloadData(chosenMember.AvatarUrl);
				using var avatarStream = new MemoryStream(avatarBytes);
				using var userAvatar = Image.FromStream(avatarStream);
				using var userAvatarScaled = ResizeImage(userAvatar, 80, 80);
				using var graphics = Graphics.FromImage(image);
				using var intendedNameFont = new Font("Arial", 60);
				using var nameFont = ResizeFont(graphics, $"@{chosenMember.Username} #{chosenMember.Discriminator}", new RectangleF(130, 285, 260, 35), intendedNameFont);
				using var numberFont = new Font("Arial", 30);
				using var format = new StringFormat {
					Alignment = StringAlignment.Center,
					LineAlignment = StringAlignment.Center
				};
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				graphics.DrawImage(userAvatarScaled, new Point(30, 252));
				graphics.DrawString($"@{chosenMember.Username} #{chosenMember.Discriminator}", nameFont, Brushes.DarkBlue, new RectangleF(130, 285, 260, 35), format);
				graphics.DrawString($"{numberOfPings}", numberFont, Brushes.DarkRed, new RectangleF(-45, 317, 175, 70), format);
				graphics.Flush();

				string filePath = $"mrping-{Guid.NewGuid()}.png";
				image.Save(filePath);

				await BotMethods.SendFile(this, new SendFileEventArgs {
					Channel = e.Channel,
					FilePath = filePath,
					LogMessage = "MrPingFileSuccess"
				});

				Database.NewChallenge(chosenMember, e.Author, numberOfPings, e.Channel);

				if (File.Exists(filePath)) {
					File.Delete(filePath);
				}
			} else if (splitString.Length == 2 && splitString[1] == "list") {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = Database.GetActiveChallenges(e.Channel),
					Channel = e.Channel,
					LogMessage = "MrPingList"
				});
			} else if (splitString.Length == 2 && splitString[1] == "stats") {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = Database.GetStatsMessage(),
					Channel = e.Channel,
					LogMessage = "MrPingStats"
				});
			} else {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"Incorrect usage of mr ping. Simply type {Term.Code()} to challenge someone, or type {$"{BotMethods.GetHelpCommandTerm(this, GuildId)} {Term}".Code()} for more commands.",
					Channel = e.Channel,
					LogMessage = "MrPingErrorBadArguments"
				});
			}
		}

		private static Font ResizeFont(Graphics g, string s, RectangleF r, Font font) {
			SizeF realSize = g.MeasureString(s, font);
			float heightRatio = r.Height / realSize.Height;
			float widthRatio = r.Width / realSize.Width;

			float scaleRatio = (heightRatio < widthRatio) ? heightRatio : widthRatio;

			float scaleSize = font.Size * scaleRatio;

			return new Font(font.FontFamily, scaleSize);
		}

		/// <summary>
		/// Resize the image to the specified width and height.
		/// </summary>
		/// <param name="image">The image to resize.</param>
		/// <param name="width">The width to resize to.</param>
		/// <param name="height">The height to resize to.</param>
		/// <returns>The resized image.</returns>
		private static Bitmap ResizeImage(Image image, int width, int height) {
			var destRect = new Rectangle(0, 0, width, height);
			var destImage = new Bitmap(width, height);

			destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

			using (var graphics = Graphics.FromImage(destImage)) {
				graphics.CompositingMode = CompositingMode.SourceCopy;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

				using var wrapMode = new ImageAttributes();
				wrapMode.SetWrapMode(WrapMode.TileFlipXY);
				graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
			}

			return destImage;
		}
	}
}

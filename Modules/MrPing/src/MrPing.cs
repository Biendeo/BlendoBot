using BlendoBotLib;
using BlendoBotLib.DataStore;
using BlendoBotLib.Interfaces;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MrPing.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MrPing {
	[CommandDefaults(defaultTerm: "mrping")]
	public class MrPing : ICommand {
		public string Name => "Mr. Ping Challenge";
		public string Description => "Subjects someone to the Mr. Ping Challenge!";
		public string GetUsage(string term) => $"{term.Code()} ({"Creates a new Mr Ping challenge for a random victim".Italics()})\n{$"{term} list".Code()} ({"Prints a list of all outstanding challenges".Italics()})\n{$"{term} stats".Code()} ({"Posts some neat stats about the challenge".Italics()})";
		public string Author => "Biendeo";
		public string Version => "1.5.0";

		internal Database Database;

		public MrPing(
			Guild guild,
			IDataStore<MrPing, List<Challenge>> challengeStore,
			IDataStore<MrPing, ServerStats> statsStore,
			IDiscordClient discordClient,
			ILogger<MrPing> logger,
			ILoggerFactory loggerFactory,
			IMessageListenerRepository messageListenerRepository)
		{
			this.guildId = guild.Id;
            this.discordClient = discordClient;
            this.logger = logger;
            this.messageListenerRepository = messageListenerRepository;

			this.Database = new Database(this.guildId, challengeStore, statsStore, discordClient, loggerFactory.CreateLogger<Database>());
			this.messageListenerRepository.Add(this.guildId, new MrPingListener(this));
        }

		public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
		{
			services.AddSingleton<
				IDataStore<MrPing, List<Challenge>>,
				JsonFileDataStore<MrPing, List<Challenge>>>();
			services.AddSingleton<
				IDataStore<MrPing, ServerStats>,
				JsonFileDataStore<MrPing, ServerStats>>();
		}

		public const int MaxPings = 1;
        private readonly ulong guildId;
        private readonly IDiscordClient discordClient;
        private readonly ILogger<MrPing> logger;
        private readonly IMessageListenerRepository messageListenerRepository;

        public async Task OnMessage(MessageCreateEventArgs e) {
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
					if (!member.IsBot &&
						member.Presence != null &&
						(member.Presence.Status == UserStatus.Online || member.Presence.Status == UserStatus.Idle) &&
						member.PermissionsIn(e.Channel).HasPermission(Permissions.SendMessages)) {
						filteredMembers.Add(member);
					}
				}

				if (filteredMembers.Count == 0) {
					await this.discordClient.SendMessage(this, new SendMessageEventArgs {
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

				await this.discordClient.SendFile(this, new SendFileEventArgs {
					Channel = e.Channel,
					FilePath = filePath,
					LogMessage = "MrPingFileSuccess"
				});

				await Database.NewChallenge(chosenMember, e.Author, numberOfPings, e.Channel);

				if (File.Exists(filePath)) {
					File.Delete(filePath);
				}
			} else if (splitString.Length == 2 && splitString[1] == "list") {
				await this.discordClient.SendMessage(this, new SendMessageEventArgs {
					Message = await Database.GetActiveChallenges(e.Channel),
					Channel = e.Channel,
					LogMessage = "MrPingList"
				});
			} else if (splitString.Length == 2 && splitString[1] == "stats") {
				await this.discordClient.SendMessage(this, new SendMessageEventArgs {
					Message = await Database.GetStatsMessage(),
					Channel = e.Channel,
					LogMessage = "MrPingStats"
				});
			} else {
				await this.discordClient.SendMessage(this, new SendMessageEventArgs {
					Message = $"Incorrect usage of mr ping, try {"?help".Code()} to learn more.",
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

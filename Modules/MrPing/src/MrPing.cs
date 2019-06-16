using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MrPing.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MrPing {
	public class MrPing : ICommand {
		CommandProps ICommand.Properties => properties;

		private static readonly CommandProps properties = new CommandProps {
			Term = "?mrping",
			Name = "Mr. Ping Challenge",
			Description = "Subjects someone to the Mr. Ping Challenge!",
			Usage = $"{"?mrping".Code()} ({"Creates a new Mr Ping challenge for a random victim".Italics()})\n{"?mrping list".Code()} ({"Prints a list of all outstanding challenges".Italics()})\n{"?mrping stats".Code()} ({"Posts some neat stats about the challenge".Italics()})",
			Author = "Biendeo",
			Version = "1.0.0",
			Startup = Startup,
			OnMessage = MrPingCommand
		};

		internal static Database Database;

		private static async Task<bool> Startup() {
			if (Database == null) {
				Database = new Database();
			}

			await Task.Delay(0);
			return true;
		}

		public const int MaxPings = 10;

		public static async Task MrPingCommand(MessageCreateEventArgs e) {
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
				using (var image = Image.FromFile(@"Modules/MrPing/res/mr.png")) {
					using (var wc = new WebClient()) {
						byte[] avatarBytes = wc.DownloadData(chosenMember.AvatarUrl);
						using (var avatarStream = new MemoryStream(avatarBytes)) {
							using (var userAvatar = Image.FromStream(avatarStream)) {
								using (var userAvatarScaled = ResizeImage(userAvatar, 80, 80)) {
									using (var graphics = Graphics.FromImage(image)) {
										using (var intendedNameFont = new Font("Arial", 60)) {
											using (var nameFont = ResizeFont(graphics, $"@{chosenMember.Username} #{chosenMember.Discriminator}", new RectangleF(130, 285, 260, 35), intendedNameFont)) {
												using (var numberFont = new Font("Arial", 30)) {
													using (var format = new StringFormat()) {
														format.Alignment = StringAlignment.Center;
														format.LineAlignment = StringAlignment.Center;
														graphics.SmoothingMode = SmoothingMode.AntiAlias;
														graphics.DrawImage(userAvatarScaled, new Point(30, 252));
														graphics.DrawString($"@{chosenMember.Username} #{chosenMember.Discriminator}", nameFont, Brushes.DarkBlue, new RectangleF(130, 285, 260, 35), format);
														graphics.DrawString($"{numberOfPings}", numberFont, Brushes.DarkRed, new RectangleF(-45, 317, 175, 70), format);
														graphics.Flush();

														string filePath = $"mrping-{Guid.NewGuid()}.png";
														image.Save(filePath);

														await Methods.SendFile(null, new SendFileEventArgs {
															Channel = e.Channel,
															FilePath = filePath,
															LogMessage = "MrPingFileSuccess"
														});

														Database.NewChallenge(chosenMember, e.Author, numberOfPings, e.Guild, e.Channel);

														if (File.Exists(filePath)) {
															File.Delete(filePath);
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			} else if (splitString.Length == 2 && splitString[1] == "list") {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = Database.GetActiveChallenges(e.Guild),
					Channel = e.Channel,
					LogMessage = "MrPingList"
				});
			} else if (splitString.Length == 2 && splitString[1] == "stats") {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = Database.GetStatsMessage(e.Guild),
					Channel = e.Channel,
					LogMessage = "MrPingStats"
				});
			} else {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = $"Incorrect usage of mr ping. Simply type {"?mrping".Code()} to challenge someone, or type {"?help mrping".Code()} for more commands.",
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

				using (var wrapMode = new ImageAttributes()) {
					wrapMode.SetWrapMode(WrapMode.TileFlipXY);
					graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
				}
			}

			return destImage;
		}

		//? This is really slow and probably scales poorly.
		[Obsolete("This function only served its purpose to find users that were online/afk and had write permissions in the channel. It has been superceded by just checking presence.", true)]
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

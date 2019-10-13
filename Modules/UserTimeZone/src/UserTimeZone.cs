using BlendoBotLib;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace UserTimeZone {
	public class UserTimeZone : CommandBase {
		public UserTimeZone(ulong guildId, IBotMethods botMethods) : base(guildId, botMethods) { }

		public override string Term => "?usertimezone";
		public override string Name => "User Timezone";
		public override string Description => "Saves a timezone so that commands can represent times in your timezone.";
		public override string Usage => $"Usage:\n{"?usertimezone".Code()} {"(prints what time zone is set for your account)".Italics()}\n{"?usertimezone [timezone]".Code()} {"(sets your timezone to the given timezone)".Italics()}\nA timezone is represented as an offset from UTC, in the format of {"+3:00".Code()}, {"10:00".Code()}, {"-4:30".Code()} etc.\n{"UTC".Code()} is a valid timezone.";
		public override string Author => "Biendeo";
		public override string Version => "0.1.0";

		private string JsonPath => Path.Combine(BotMethods.GetCommandInstanceDataPath(this, this), "blendobot-usertimezone.json");

		internal List<UserSetting> settings;

		private bool LoadDatabase() {
			if (File.Exists(JsonPath)) {
				settings = JsonConvert.DeserializeObject<List<UserSetting>>(File.ReadAllText(JsonPath));
			} else {
				settings = new List<UserSetting>();
			}
			return true;
		}

		private void SaveDatabase() {
			File.WriteAllText(JsonPath, JsonConvert.SerializeObject(settings));
		}

		public override async Task<bool> Startup() {
			await Task.Delay(0);
			return LoadDatabase();
		}

		public override async Task OnMessage(MessageCreateEventArgs e) {
			string[] splitMessage = e.Message.Content.Split(' ');
			if (splitMessage.Length == 1) {
				TimeZoneInfo timezone = GetUserTimeZone(e.Author);
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"Your timezone is currently set to {TimeZoneOffsetToString(timezone).Code()}",
					Channel = e.Channel,
					LogMessage = "UserTimezoneList"
				});
			} else if (splitMessage.Length == 2) {
				try {
					TimeZoneInfo timezone = StringToCustomTimeZone(splitMessage[1]);
					var currentEntry = settings.Find(s => s.Username == e.Author.Username && s.Discriminator == e.Author.Discriminator);
					if (currentEntry != null) {
						currentEntry.TimeZone = timezone;
					} else {
						settings.Add(new UserSetting {
							Discriminator = e.Author.Discriminator,
							Username = e.Author.Username,
							TimeZone = timezone
						});
					}
					SaveDatabase();
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"Your timezone is now {TimeZoneOffsetToString(timezone).Code()}",
						Channel = e.Channel,
						LogMessage = "UserTimezoneSet"
					});
				} catch (FormatException) {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"Could not parse your timezone offset of {splitMessage[1]}. Make sure it follows a valid format (i.e. {"+3:00".Code()}, {"10:00".Code()}, {"-4:30".Code()}, etc.).",
						Channel = e.Channel,
						LogMessage = "UserTimezoneSetError"
					});
				} catch (ArgumentOutOfRangeException) {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"Your timezone does not exist, make sure it is between {"-14:00".Code()} and {"+14:00".Code()}.",
						Channel = e.Channel,
						LogMessage = "UserTimezoneSetError"
					});
				}
			} else {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"Too many arguments! Please refer to {"?help usertimezone".Code()} for usage information.",
					Channel = e.Channel,
					LogMessage = "UserTimezoneTooManyArguments"
				});
			}
		}

		private static TimeZoneInfo StringToCustomTimeZone(string s) {
			if (s.ToLower() == "utc") {
				return TimeZoneInfo.Utc;
			} else {
				if (s.StartsWith('+')) {
					s = s.Substring(1);
				}
				var span = TimeSpan.Parse(s);
				string name = $"(UTC{TimeSpanOffsetToString(span)}) Generic {TimeSpanOffsetToString(span)} TimeZone";
				return TimeZoneInfo.CreateCustomTimeZone(name, span, name, name);
			}
		}

		public static string TimeZoneOffsetToString(TimeZoneInfo timezone) {
			return TimeSpanOffsetToString(timezone.BaseUtcOffset);
		}

		public static string TimeSpanOffsetToString(TimeSpan span) {
			return $"{(span.Hours >= 0 ? "+" : "")}{span.Hours.ToString().PadLeft(2, '0')}:{Math.Abs(span.Minutes).ToString().PadLeft(2, '0')}";
		}

		/// <summary>
		/// Returns the user time zone for a given user. It returns UTC if the user has not input a custom time zone.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public TimeZoneInfo GetUserTimeZone(DiscordUser user) {
			UserSetting setting = settings.Find(u => u.Username == user.Username && u.Discriminator == user.Discriminator);
			if (setting == null) {
				return TimeZoneInfo.Utc;
			} else {
				return setting.TimeZone;
			}
		}

		/// <summary>
		/// Returns the user time zone for a user given the command that requests it. This should get the appropriate
		/// guild's instance of the UserTimeZone command and return either the input field, or UTC if not found.
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		public static TimeZoneInfo GetUserTimeZone(CommandBase command, DiscordUser user) {
			UserTimeZone userTimeZone = command.BotMethods.GetCommand<UserTimeZone>(command, command.GuildId);
			if (userTimeZone != null) {
				return userTimeZone.GetUserTimeZone(user);
			} else {
				return TimeZoneInfo.Utc;
			}
		}

		public static string ToShortString(TimeZoneInfo timeZone) {
			var sb = new StringBuilder();
			if (timeZone.BaseUtcOffset.Hours >= 0) {
				sb.Append("+");
			} else {
				sb.Append("-");
			}

			sb.Append($"{Math.Abs(timeZone.BaseUtcOffset.Hours).ToString().PadLeft(2, '0')}:{Math.Abs(timeZone.BaseUtcOffset.Minutes).ToString().PadLeft(2, '0')}");

			return sb.ToString();
		}
	}
}

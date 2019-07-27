using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace UserTimeZone {
	public class UserTimeZone : ICommand {
		CommandProps ICommand.Properties => properties;

		private static readonly CommandProps properties = new CommandProps {
			Term = "?usertimezone",
			Name = "User Timezone",
			Description = "Saves a timezone so that commands can represent times in your timezone.",
			Usage = $"Usage:\n{"?usertimezone".Code()} {"(prints what time zone is set for your account)".Italics()}\n{"?usertimezone [timezone]".Code()} {"(sets your timezone to the given timezone)".Italics()}\nA timezone is represented as an offset from UTC, in the format of {"+3:00".Code()}, {"10:00".Code()}, {"-4:30".Code()} etc.\n{"UTC".Code()} is a valid timezone.",
			Author = "Biendeo",
			Version = "0.1.0",
			Startup = Startup,
			OnMessage = UserTimezoneCommand
		};

		private static readonly string JsonPath = "blendobot-usertimezone.json";

		internal static List<UserSetting> settings;

		private static bool LoadDatabase() {
			if (File.Exists(JsonPath)) {
				settings = JsonConvert.DeserializeObject<List<UserSetting>>(File.ReadAllText(JsonPath));
			} else {
				settings = new List<UserSetting>();
			}
			return true;
		}

		private static void SaveDatabase() {
			File.WriteAllText(JsonPath, JsonConvert.SerializeObject(settings));
		}

		private static async Task<bool> Startup() {
			await Task.Delay(0);
			return LoadDatabase();
		}

		public static async Task UserTimezoneCommand(MessageCreateEventArgs e) {
			string[] splitMessage = e.Message.Content.Split(' ');
			if (splitMessage.Length == 1) {
				TimeZoneInfo timezone = GetUserTimeZone(e.Author);
				await Methods.SendMessage(null, new SendMessageEventArgs {
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
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = $"Your timezone is now {TimeZoneOffsetToString(timezone).Code()}",
						Channel = e.Channel,
						LogMessage = "UserTimezoneSet"
					});
				} catch (FormatException) {
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = $"Could not parse your timezone offset of {splitMessage[1]}. Make sure it follows a valid format (i.e. {"+3:00".Code()}, {"10:00".Code()}, {"-4:30".Code()}, etc.).",
						Channel = e.Channel,
						LogMessage = "UserTimezoneSetError"
					});
				} catch (ArgumentOutOfRangeException) {
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = $"Your timezone does not exist, make sure it is between {"-14:00".Code()} and {"+14:00".Code()}.",
						Channel = e.Channel,
						LogMessage = "UserTimezoneSetError"
					});
				}
			} else {
				await Methods.SendMessage(null, new SendMessageEventArgs {
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

		public static TimeZoneInfo GetUserTimeZone(DiscordUser user) {
			UserSetting setting = settings.Find(u => u.Username == user.Username && u.Discriminator == user.Discriminator);
			if (setting == null) {
				return TimeZoneInfo.Utc;
			} else {
				return setting.TimeZone;
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

using BlendoBotLib;
using BlendoBotLib.DataStore;
using BlendoBotLib.Interfaces;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserTimeZone {
	[CommandDefaults(defaultTerm: "usertimezone", enabled: true)]
	public class UserTimeZone : ICommand {
		public string Name => "User Timezone";
		public string Description => "Saves a timezone so that commands can represent times in your timezone.";
		public string GetUsage(string term) => $"Usage:\n{term.Code()} {"(prints what time zone is set for your account)".Italics()}\n{$"{term} [timezone]".Code()} {"(sets your timezone to the given timezone)".Italics()}\nA timezone is represented as an offset from UTC, in the format of {"+3:00".Code()}, {"10:00".Code()}, {"-4:30".Code()} etc.\n{"UTC".Code()} is a valid timezone.";
		public string Author => "Biendeo";
		public string Version => "0.5.0";

        private readonly IDiscordClient discordClient;
        private readonly ILogger<UserTimeZone> logger;
        private readonly IUserTimeZoneProvider timeZoneProvider;

        public UserTimeZone(
			IDiscordClient discordClient,
			ILogger<UserTimeZone> logger,
			IUserTimeZoneProvider timeZoneProvider)
		{
            this.discordClient = discordClient;
            this.logger = logger;
            this.timeZoneProvider = timeZoneProvider;
        }

		public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
		{
			services.AddSingleton<IUserTimeZoneProvider, UserTimeZoneProvider>();
			services.AddSingleton<
				IDataStore<UserTimeZoneProvider, Dictionary<string, TimeZoneInfo>>,
				JsonFileDataStore<UserTimeZoneProvider, Dictionary<string, TimeZoneInfo>>>();
		}

		public async Task OnMessage(MessageCreateEventArgs e) {
			string[] splitMessage = e.Message.Content.Split(' ');
			if (splitMessage.Length == 1) {
				TimeZoneInfo timezone = await this.timeZoneProvider.GetTimeZone(e.Guild.Id, e.Author.Id);
				await this.discordClient.SendMessage(this, new SendMessageEventArgs {
					Message = $"Your timezone is currently set to {TimeZoneOffsetToString(timezone).Code()}",
					Channel = e.Channel,
					LogMessage = "UserTimezoneList"
				});
			} else if (splitMessage.Length == 2) {
				try {
					TimeZoneInfo timezone = StringToCustomTimeZone(splitMessage[1]);
					await this.timeZoneProvider.SetTimeZone(e.Guild.Id, e.Author.Id, timezone);
					await this.discordClient.SendMessage(this, new SendMessageEventArgs {
						Message = $"Your timezone is now {TimeZoneOffsetToString(timezone).Code()}",
						Channel = e.Channel,
						LogMessage = "UserTimezoneSet"
					});
				} catch (FormatException) {
					await this.discordClient.SendMessage(this, new SendMessageEventArgs {
						Message = $"Could not parse your timezone offset of {splitMessage[1]}. Make sure it follows a valid format (i.e. {"+3:00".Code()}, {"10:00".Code()}, {"-4:30".Code()}, etc.).",
						Channel = e.Channel,
						LogMessage = "UserTimezoneSetError"
					});
				} catch (ArgumentOutOfRangeException) {
					await this.discordClient.SendMessage(this, new SendMessageEventArgs {
						Message = $"Your timezone does not exist, make sure it is between {"-14:00".Code()} and {"+14:00".Code()}.",
						Channel = e.Channel,
						LogMessage = "UserTimezoneSetError"
					});
				}
			} else {
				await this.discordClient.SendMessage(this, new SendMessageEventArgs {
					Message = $"Too many arguments! Please refer to {$"?help usertimezone".Code()} for usage information.",
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

		private static string TimeZoneOffsetToString(TimeZoneInfo timezone) {
			return TimeSpanOffsetToString(timezone.BaseUtcOffset);
		}

		private static string TimeSpanOffsetToString(TimeSpan span) {
			return $"{(span.Hours >= 0 ? "+" : "")}{span.Hours.ToString().PadLeft(2, '0')}:{Math.Abs(span.Minutes).ToString().PadLeft(2, '0')}";
		}
	}
}

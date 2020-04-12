﻿using BlendoBotLib;
using BlendoBotLib.DataStore;
using BlendoBotLib.Interfaces;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using UserTimeZone;

namespace RemindMe {
	[CommandDefaults(defaultTerm: "remind")]
	public class RemindMe : ICommand {

		public string Name => "Remind Me";
		public string Description => "Reminds you about something later on! Please note that I currently do not remember messages if I am restarted.";
		public string GetUsage(string term) => $"Usage:\n{$"{term} at [date and/or time] to [message]".Code()} {"(this reminds you at a certain point in time)".Italics()}\n{$"{term} in [timespan] to [message]".Code()} {"(this reminds you after a certain interval)".Italics()}\n\n{"Valid date formats".Bold()}\n{"dd/mm/yyyy".Code()} ({"e.g. 1/03/2020".Italics()})\n{"dd/mm/yy".Code()} ({"e.g. 20/05/19".Italics()})\n{"dd/mm".Code()} ({"e.g. 30/11 (the year is implied)".Italics()})\n\n{"Valid time formats".Bold()}\n{"hh:mm:ss".Code()} ({"e.g. 13:40:00".Italics()})\n{"hh:mm".Code()} ({"e.g. 21:12".Italics()})\n{"All times are in 24-hour time!".Bold()}\n\n{"Valid timespan formats".Bold()}\n{"hh:mm:ss".Code()} ({"e.g. 1:20:00".Italics()})\n{"mm:ss".Code()} ({"e.g. 00:01".Italics()})\n\nFor {"{term} at".Code()}, you may choose to either write either a date, a time, or both! Some examples:\n{"{term} at 1/01/2020".Code()}\n{"{term} at 12:00:00".Code()}\n{"{term} at 1/01/2020 12:00:00".Code()}\n{"{term} at 12:00:00 1/01/2020".Code()}\n\nPlease note that all date/time strings are interpreted as UTC time unless you have set a custom time zone.\nThe output is always formatted as {TimeFormatString.Code()}";
		public string Author => "Biendeo";
		public string Version => "0.1.3";

		private string DatabasePath => Path.Combine(this.guildId.ToString(), "remindme");
		private const string TimeFormatString = "d/MM/yyyy h:mm:ss tt";

		private List<Reminder> OutstandingReminders;
		private Timer DailyReminderCheck;
        private readonly IDataStore<RemindMe, List<Reminder>> dataStore;
        private readonly IDiscordClient discordClient;
        private readonly ILogger<RemindMe> logger;
        private readonly IUserTimeZoneProvider timeZoneProvider;
		private readonly ulong guildId;

		public RemindMe(
			Guild guild,
			IDataStore<RemindMe, List<Reminder>> dataStore,
			IDiscordClient discordClient,
			ILogger<RemindMe> logger,
			IUserTimeZoneProvider timeZoneProvider)
		{
			this.guildId = guild.Id;

            this.dataStore = dataStore;
            this.discordClient = discordClient;
            this.logger = logger;
            this.timeZoneProvider = timeZoneProvider;

			this.Startup().Wait();
		}

		public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
		{
			services.AddSingleton<
				IDataStore<RemindMe, List<Reminder>>,
				JsonFileDataStore<RemindMe, List<Reminder>>>();
		}

        public async Task<bool> Startup() {
			var sw = Stopwatch.StartNew();
			this.logger.LogInformation("Loading reminders for guild {}", this.guildId);

			try
			{
				OutstandingReminders = await this.dataStore.ReadAsync(DatabasePath);
			}
			catch (Exception ex) when (ex is DirectoryNotFoundException || ex is FileNotFoundException)
			{
				this.logger.LogInformation("Reminders not found in data store for guild {}, creating new", this.guildId);
				OutstandingReminders = new List<Reminder>();
				await SaveReminders();
			}
			this.logger.LogInformation("Reminders loaded for guild {}, took {}ms", this.guildId, sw.Elapsed.TotalMilliseconds);

			foreach (var reminder in OutstandingReminders) {
				if (reminder.Time < DateTime.UtcNow) {
					var channel = await this.discordClient.GetChannel(reminder.ChannelId);
					try {
						await this.discordClient.SendMessage(this, new SendMessageEventArgs {
							Message = $"I just woke up and forgot to send <@{reminder.UserId}> this alert on time!\n{reminder.Message}",
							Channel = channel,
							LogMessage = "ReminderLateAlert"
						});
					} catch (UnauthorizedException) {
						this.logger.LogWarning($"Tried doing a wakeup message {reminder.Message} which should've sent at {reminder.Time}, but a 403 was received! This tried to send to user {reminder.UserId} in channel {reminder.ChannelId}.");
					}
				} else {
					reminder.Activate(ReminderElapsed);
				}
			}

			OutstandingReminders.RemoveAll(r => r.Time < DateTime.UtcNow);
			OutstandingReminders.Sort();
			await SaveReminders();
			// Check every 12 hours to see if any dormant reminders can be activated. This is because the number of
			// milliseconds to the reminder date may be too large otherwise. The reminders themselves will only activate
			// if they are within 24 hours of the event, so this should be plenty.
			DailyReminderCheck = new Timer(43_200_000.0);
			DailyReminderCheck.Elapsed += DailyReminderCheckElapsed;
			DailyReminderCheck.AutoReset = true;
			DailyReminderCheck.Enabled = true;
			return true;
		}

		private void DailyReminderCheckElapsed(object sender, ElapsedEventArgs e) {
			OutstandingReminders.ForEach(r => {
				r.Activate(ReminderElapsed);
			});
		}
		private async void ReminderElapsed(object sender, ElapsedEventArgs e, Reminder r) {
			var channel = await this.discordClient.GetChannel(r.ChannelId);
			await this.discordClient.SendMessage(this, new SendMessageEventArgs {
				Message = $"<@{r.UserId}> wanted to know this message now!\n{r.Message}",
				Channel = channel,
				LogMessage = "ReminderAlert"
			});
			OutstandingReminders.Remove(r);
			r.Dispose();
			await SaveReminders();
		}

		// This command does not have the same copy -> modify -> replace semantics as the rest of the revamped commands!
		// TODO fix
		private Task SaveReminders() => this.dataStore.WriteAsync(DatabasePath, this.OutstandingReminders);

		public async Task OnMessage(MessageCreateEventArgs e) {
			// Try and decipher the output.
			string[] splitMessage = e.Message.Content.Split(' ');
			TimeZoneInfo userTimeZone = await this.timeZoneProvider.GetTimeZone(e.Guild.Id, e.Author.Id);

			// Try and look for the "to" index.
			int toIndex = 0;
			while (toIndex < splitMessage.Length && splitMessage[toIndex] != "to") {
				++toIndex;
			}

			if (toIndex == splitMessage.Length) {
				await this.discordClient.SendMessage(this, new SendMessageEventArgs {
					Message = "Incorrect syntax, make sure you use the word \"to\" after you indicate the time you want the reminder!",
					Channel = e.Channel,
					LogMessage = "ReminderErrorNoTo"
				});
				return;
			} else if (toIndex == splitMessage.Length - 1) {
				await this.discordClient.SendMessage(this, new SendMessageEventArgs {
					Message = "Incorrect syntax, make sure you type a message after that \"to\"!",
					Channel = e.Channel,
					LogMessage = "ReminderErrorNoMessage"
				});
				return;
			}

			// Now decipher the time.
			DateTime foundTime = DateTime.UtcNow;
			string[] writtenTime = splitMessage.Skip(2).Take(toIndex - 2).ToArray();
			if (splitMessage[1] == "at") {
				bool successfulFormat = true;
				DateTime foundDate = DateTime.UtcNow.Add(userTimeZone.BaseUtcOffset).Date;
				var foundTimeSpan = new TimeSpan();
				bool didUserInputDate = false;
				bool didUserInputTime = false;
				if (writtenTime.Length > 0 && writtenTime.Length <= 2) {
					foreach (string item in writtenTime) {
						if (item.Contains(":") && !didUserInputTime) {
							try {
								int[] splitDigits = item.Split(":").Select(s => int.Parse(s)).ToArray();
								if (splitDigits.Length == 2) {
									foundTimeSpan = new TimeSpan(splitDigits[0], splitDigits[1], 0);
								} else if (splitDigits.Length == 3) {
									foundTimeSpan = new TimeSpan(splitDigits[0], splitDigits[1], splitDigits[2]);
								} else {
									successfulFormat = false;
								}
							} catch (FormatException) {
								successfulFormat = false;
							}
							didUserInputTime = true;
						} else if (item.Contains("/") && !didUserInputDate) {
							try {
								int[] splitDigits = item.Split("/").Select(s => int.Parse(s)).ToArray();
								if (splitDigits.Length == 2) {
									foundDate = new DateTime(foundTime.Year, splitDigits[1], splitDigits[0]);
								} else if (splitDigits.Length == 3) {
									if (splitDigits[2] < 100) {
										splitDigits[2] += 2000; // Won't work when we hit 2100 but that shouldn't be hard to spot.
									}
									foundDate = new DateTime(splitDigits[2], splitDigits[1], splitDigits[0]);
								} else {
									successfulFormat = false;
								}
							} catch (FormatException) {
								successfulFormat = false;
							}
							didUserInputDate = true;
						} else {
							successfulFormat = false;
						}
					}
					// Basically, interpet the date that the user wants, then factor the timezone to UTC.
					foundTime = DateTime.SpecifyKind(foundDate + foundTimeSpan - userTimeZone.BaseUtcOffset, DateTimeKind.Utc);
				} else {
					successfulFormat = false;
				}
				if (!successfulFormat) {
					await this.discordClient.SendMessage(this, new SendMessageEventArgs {
						Message = $"The date/time you input could not be parsed! See {$"?help remind".Code()} for how to format your date/time!",
						Channel = e.Channel,
						LogMessage = "ReminderErrorInvalidTime"
					});
					return;
				}
			} else  if (splitMessage[1] == "in") {
				bool successfulFormat = true;
				if (writtenTime.Length == 1) {
					try {
						int[] splitDigits = writtenTime[0].Split(":").Select(s => int.Parse(s)).ToArray();
						var timespan = new TimeSpan();
						if (splitDigits.Length == 2) {
							timespan = new TimeSpan(0, splitDigits[0], splitDigits[1]);
						} else if (splitDigits.Length == 3) {
							timespan = new TimeSpan(splitDigits[0], splitDigits[1], splitDigits[2]);
						} else {
							successfulFormat = false;
						}
						if (successfulFormat) {
							foundTime += timespan;
						}
					} catch (FormatException) {
						successfulFormat = false;
					}
				} else {
					successfulFormat = false;
				}
				if (!successfulFormat) {
					await this.discordClient.SendMessage(this, new SendMessageEventArgs {
						Message = $"The timespan you input could not be parsed! See {$"?help remind".Code()} for how to format your timespan!",
						Channel = e.Channel,
						LogMessage = "ReminderErrorInvalidTimespan"
					});
					return;
				}
			} else {
				await this.discordClient.SendMessage(this, new SendMessageEventArgs {
					Message = "Incorrect syntax, make sure you use the word \"in\" or \"at\" to specify a time for the reminder!",
					Channel = e.Channel,
					LogMessage = "ReminderErrorNoAt"
				});
				return;
			}
			if (foundTime < DateTime.UtcNow) {
				await this.discordClient.SendMessage(this, new SendMessageEventArgs {
					Message = $"The time you input was parsed as {TimeZoneInfo.ConvertTime(foundTime, userTimeZone).ToString(TimeFormatString)} {userTimeZone.ToShortString()}, which is in the past! Make your time a little more specific!",
					Channel = e.Channel,
					LogMessage = "ReminderErrorPastTime"
				});
				return;
			}
			// Finally extract the message.
			string message = string.Join(' ', splitMessage.Skip(toIndex + 1));

			// Make the reminder.
			var reminder = new Reminder(TimeZoneInfo.ConvertTimeToUtc(foundTime), message, e.Channel.Id, e.Author.Id);
			reminder.Activate(ReminderElapsed);
			OutstandingReminders.Add(reminder);
			await SaveReminders();

			await this.discordClient.SendMessage(this, new SendMessageEventArgs {
				Message = $"Okay, I'll tell you this message at {TimeZoneInfo.ConvertTime(foundTime, userTimeZone).ToString(TimeFormatString)} {userTimeZone.ToShortString()}",
				Channel = e.Channel,
				LogMessage = "ReminderConfirm"
			});
		}
	}
}

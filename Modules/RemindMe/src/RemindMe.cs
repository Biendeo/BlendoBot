using BlendoBotLib;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace RemindMe {
	public class RemindMe : CommandBase {
		public RemindMe(ulong guildId, IBotMethods botMethods) : base(guildId, botMethods) { }

		public override string DefaultTerm => "?remind";
		public override string Name => "Remind Me";
		public override string Description => "Reminds you about something later on! Please note that I currently do not remember messages if I am restarted.";
		public override string Usage => $"Usage:\n{$"{Term} at [date and/or time] to [message]".Code()} {"(this reminds you at a certain point in time)".Italics()}\n{$"{Term} in [timespan] to [message]".Code()} {"(this reminds you after a certain interval)".Italics()}\n\n{"Valid date formats".Bold()}\n{"dd/mm/yyyy".Code()} ({"e.g. 1/03/2020".Italics()})\n{"dd/mm/yy".Code()} ({"e.g. 20/05/19".Italics()})\n{"dd/mm".Code()} ({"e.g. 30/11 (the year is implied)".Italics()})\n\n{"Valid time formats".Bold()}\n{"hh:mm:ss".Code()} ({"e.g. 13:40:00".Italics()})\n{"hh:mm".Code()} ({"e.g. 21:12".Italics()})\n{"All times are in 24-hour time!".Bold()}\n\n{"Valid timespan formats".Bold()}\n{"hh:mm:ss".Code()} ({"e.g. 1:20:00".Italics()})\n{"mm:ss".Code()} ({"e.g. 00:01".Italics()})\n\nFor {$"{Term} at".Code()}, you may choose to either write either a date, a time, or both! Some examples:\n{$"{Term} at 1/01/2020".Code()}\n{$"{Term} at 12:00:00".Code()}\n{$"{Term} at 1/01/2020 12:00:00".Code()}\n{$"{Term} at 12:00:00 1/01/2020".Code()}\n\nPlease note that all date/time strings are interpreted as UTC time unless you have set a {BotMethods.GetCommand<UserTimeZone.UserTimeZone>(this, GuildId).Term.Code()}.\nThe output is always formatted as {TimeFormatString.Code()}";
		public override string Author => "Biendeo";
		public override string Version => "0.1.3";

		private string DatabasePath => Path.Combine(BotMethods.GetCommandInstanceDataPath(this, this), "blendobot-remindme-database.json");
		private const string TimeFormatString = "d/MM/yyyy h:mm:ss tt";

		private List<Reminder> OutstandingReminders;
		private Timer DailyReminderCheck;

		private Reminder AddRepeatReminder(Reminder r) {
			if (r.IsRepeating) {
				DateTime nextTime = r.Time;
				while (nextTime <= DateTime.UtcNow) {
					nextTime = nextTime.AddSeconds(r.Frequency);
				}
				var nextReminder = new Reminder(nextTime, r.Message, r.ChannelId, r.UserId, r.Frequency);
				nextReminder.Activate(ReminderElapsed);
				OutstandingReminders.Add(nextReminder);
				return nextReminder;
			}
			return null;
		}

		public override async Task<bool> Startup() {
			OutstandingReminders = new List<Reminder>();
			if (File.Exists(DatabasePath)) {
				OutstandingReminders = JsonConvert.DeserializeObject<List<Reminder>>(File.ReadAllText(DatabasePath));
				foreach (var reminder in OutstandingReminders) {
					if (reminder.Time < DateTime.UtcNow) {
						var channel = await BotMethods.GetChannel(this, reminder.ChannelId);
						try {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"I just woke up and forgot to send <@{reminder.UserId}> this alert on time!\n{reminder.Message}",
								Channel = channel,
								LogMessage = "ReminderLateAlert"
							});
						} catch (UnauthorizedException) {
							BotMethods.Log(this, new LogEventArgs {
								Type = LogType.Warning,
								Message = $"Tried doing a wakeup message {reminder.Message} which should've sent at {reminder.Time}, but a 403 was received! This tried to send to user {reminder.UserId} in channel {reminder.ChannelId}."
							});
						}
						AddRepeatReminder(reminder);
					} else {
						reminder.Activate(ReminderElapsed);
					}
				}

				OutstandingReminders.RemoveAll(r => r.Time < DateTime.UtcNow);
				OutstandingReminders.Sort();
				SaveReminders();
			}
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
			var channel = await BotMethods.GetChannel(this, r.ChannelId);
			var sb = new StringBuilder();
			sb.AppendLine($"<@{r.UserId}> wanted to know this message now!");
			sb.AppendLine(r.Message);
			if (r.IsRepeating) {
				var nextReminder = AddRepeatReminder(r);
				sb.AppendLine($"This reminder will repeat on {nextReminder.Time}.");
			}
			await BotMethods.SendMessage(this, new SendMessageEventArgs {
				Message = sb.ToString(),
				Channel = channel,
				LogMessage = "ReminderAlert"
			});
			OutstandingReminders.Remove(r);
			r.Dispose();
			SaveReminders();
		}

		private void SaveReminders() {
			File.WriteAllText(DatabasePath, JsonConvert.SerializeObject(OutstandingReminders));
		}

		public override async Task OnMessage(MessageCreateEventArgs e) {
			// Try and decipher the output.
			string[] splitMessage = e.Message.Content.Split(' ');
			TimeZoneInfo userTimeZone = UserTimeZone.UserTimeZone.GetUserTimeZone(this, e.Author);

			// Try and look for the "every" index.
			int everyIndex = 0;
			while (everyIndex < splitMessage.Length && splitMessage[everyIndex].ToLower() != "every") {
				++everyIndex;
			}

			// Try and look for the "to" index.
			int toIndex = 0;
			while (toIndex < splitMessage.Length && splitMessage[toIndex].ToLower() != "to") {
				++toIndex;
			}

			// If the every index exists after the to, then it's part of the message and shouldn't count.
			if (everyIndex >= toIndex) {
				everyIndex = -1;
			}

			if (toIndex == splitMessage.Length) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = "Incorrect syntax, make sure you use the word \"to\" after you indicate the time you want the reminder!",
					Channel = e.Channel,
					LogMessage = "ReminderErrorNoTo"
				});
				return;
			} else if (toIndex == splitMessage.Length - 1) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = "Incorrect syntax, make sure you type a message after that \"to\"!",
					Channel = e.Channel,
					LogMessage = "ReminderErrorNoMessage"
				});
				return;
			}

			// Now decipher the time.
			DateTime foundTime = DateTime.UtcNow;
			string[] writtenTime = splitMessage.Skip(2).Take((everyIndex > -1 ? everyIndex : toIndex) - 2).ToArray();
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
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"The date/time you input could not be parsed! See {$"{BotMethods.GetHelpCommandTerm(this, GuildId)} remind".Code()} for how to format your date/time!",
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
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"The timespan you input could not be parsed! See {$"{BotMethods.GetHelpCommandTerm(this, GuildId)} remind".Code()} for how to format your timespan!",
						Channel = e.Channel,
						LogMessage = "ReminderErrorInvalidTimespan"
					});
					return;
				}
			} else {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = "Incorrect syntax, make sure you use the word \"in\" or \"at\" to specify a time for the reminder!",
					Channel = e.Channel,
					LogMessage = "ReminderErrorNoAt"
				});
				return;
			}
			if (foundTime < DateTime.UtcNow) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"The time you input was parsed as {TimeZoneInfo.ConvertTime(foundTime, userTimeZone).ToString(TimeFormatString)} {UserTimeZone.UserTimeZone.ToShortString(userTimeZone)}, which is in the past! Make your time a little more specific!",
					Channel = e.Channel,
					LogMessage = "ReminderErrorPastTime"
				});
				return;
			}
			// Handle the every portion.
			ulong frequency = 0ul;
			if (everyIndex > -1) {
				// At this point it is guaranteed the every is before the to, which means there are at least two terms here.
				bool successfulFormat = true;
				bool quantityParsed = ulong.TryParse(splitMessage[everyIndex + 1], out frequency);
				if (!quantityParsed) {
					frequency = 1;
				}
				int scaleIndex = everyIndex + 1 + (quantityParsed ? 1 : 0);
				string scaleText = splitMessage[scaleIndex].ToLower();
				if (scaleText.EndsWith('s')) {
					scaleText = scaleText[0..^1];
				}
				switch (scaleText) {
					case "second":
						break;
					case "minute":
						frequency *= 60ul;
						break;
					case "hour":
						frequency *= 3600ul;
						break;
					case "day":
						frequency *= 86400ul;
						break;
					default:
						successfulFormat = false;
						break;
				}
				if (frequency < 10ul) {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"The every frequency you input was less than 10 seconds! Please use a longer frequency!",
						Channel = e.Channel,
						LogMessage = "ReminderErrorInvalidEveryTooLow"
					});
					return;
				}
				if (!successfulFormat) {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"The every frequency you input could not be parsed! See {$"{BotMethods.GetHelpCommandTerm(this, GuildId)} remind".Code()} for how to format your repeat interval!",
						Channel = e.Channel,
						LogMessage = "ReminderErrorInvalidEvery"
					});
					return;
				}
			}

			// Finally extract the message.
			string message = string.Join(' ', splitMessage.Skip(toIndex + 1));

			// Make the reminder.
			var reminder = new Reminder(TimeZoneInfo.ConvertTimeToUtc(foundTime), message, e.Channel.Id, e.Author.Id, frequency);
			reminder.Activate(ReminderElapsed);
			OutstandingReminders.Add(reminder);
			SaveReminders();

			await BotMethods.SendMessage(this, new SendMessageEventArgs {
				Message = $"Okay, I'll tell you this message at {TimeZoneInfo.ConvertTime(foundTime, userTimeZone).ToString(TimeFormatString)} {UserTimeZone.UserTimeZone.ToShortString(userTimeZone)}",
				Channel = e.Channel,
				LogMessage = "ReminderConfirm"
			});
		}
	}
}

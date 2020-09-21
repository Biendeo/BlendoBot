using BlendoBotLib;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace RemindMe {
	public class RemindMe : CommandBase, IDisposable {
		public RemindMe(ulong guildId, IBotMethods botMethods) : base(guildId, botMethods) { }

		public override string DefaultTerm => "?remind";
		public override string Name => "Remind Me";
		public override string Description => "Reminds you about something later on!";
		public override string Usage {
			get {
				var sb = new StringBuilder();
				var db = GetContext();

				sb.AppendLine("Usage:");
				sb.AppendLine($"{$"{Term} at [date and/or time] (every [repeat interval]) to [message]".Code()} {"(this reminds you at a certain point in time)".Italics()}");
				sb.AppendLine($"{$"{Term} in [timespan] (every [repeat interval]) to [message]".Code()} {"(this reminds you after a certain interval)".Italics()}");
				sb.AppendLine($"{$"{Term} list".Code()} {"(an interactive view of all of your reminders, and lets you delete reminders)".Italics()}");
				sb.AppendLine($"{$"{Term} admin list".Code()} {$"(same as {$"{Term} list".Code()} but for all reminders in the guild)".Italics()}");
				sb.AppendLine($"{$"{Term} admin minrepeattime [num]".Code()} {"(sets the minimum interval for repeat reminders (0 disables the repeat feature))".Italics()}");
				sb.AppendLine($"{$"{Term} admin maxreminders".Code()} {$"(sets the maximum number of reminders each user can set)".Italics()}");
				sb.AppendLine();

				sb.AppendLine("Valid date formats".Bold());
				sb.AppendLine($"{"dd/mm/yyyy".Code()} ({"e.g. 1/03/2020".Italics()})");
				sb.AppendLine($"{"dd/mm/yy".Code()} ({"e.g. 20/05/19".Italics()})");
				sb.AppendLine($"{"dd/mm".Code()} ({"e.g. 30/11 (the year is implied)".Italics()})");
				sb.AppendLine();

				sb.AppendLine("Valid time formats".Bold());
				sb.AppendLine($"{"hh:mm:ss".Code()} ({"e.g. 13:40:00".Italics()})");
				sb.AppendLine($"{"hh:mm".Code()} ({"e.g. 00:01".Italics()})");
				sb.AppendLine("All times are in 24-hour time!".Bold());
				sb.AppendLine();

				sb.AppendLine("Valid timespan formats".Bold());
				sb.AppendLine($"{"hh:mm:ss".Code()} ({"e.g. 1:20:00".Italics()})");
				sb.AppendLine($"{"mm:ss".Code()} ({"e.g. 00:01".Italics()})");
				sb.AppendLine();

				sb.AppendLine($"For {$"{Term} at".Code()}, you may choose to either write either a date, a time, or both! Some examples:");
				sb.AppendLine($"{Term} at 1/01/2020".Code());
				sb.AppendLine($"{Term} at 12:00:00".Code());
				sb.AppendLine($"{Term} at 1/01/2020 12:00:00".Code());
				sb.AppendLine($"{Term} at 12:00:00 1/01/2020".Code());
				sb.AppendLine();

				sb.AppendLine("Valid repeat interval formats".Bold());
				sb.AppendLine($"{"(second(s) | minute(s) | hour(s) | day(s))".Code()} ({"e.g. every hour".Italics()})");
				sb.AppendLine($"{"x (second(s) | minute(s) | hour(s) | day(s))".Code()} ({"e.g. every 4 days".Italics()})");
				sb.AppendLine();

				sb.AppendLine("Some other examples:");
				sb.AppendLine($"{Term} at 1/01/2020 every year to congratulate the new year".Code());
				sb.AppendLine($"{Term} at 11:11:11 every 1 day to 11:11 ✨".Code());
				sb.AppendLine();

				sb.AppendLine($"Please note that all date/time strings are interpreted as UTC time unless you have set a {BotMethods.GetCommand<UserTimeZone.UserTimeZone>(this, GuildId).Term.Code()}.");
				sb.AppendLine($"The output is always formatted as {TimeFormatString.Code()}.");
				sb.AppendLine($"You can only have at most {db.Settings.MaximumRemindersPerPerson} reminders, and repeat reminders {(db.Settings.MinimumRepeatTime == 0ul ? "are current disabled" : $"must be at least every {Reminder.FrequencyToReadableString(db.Settings.MinimumRepeatTime)}")}.");

				return sb.ToString();
			}
		}
		public override string Author => "Biendeo";
		public override string Version => "0.3.0";

		private string DatabasePath => Path.Combine(BotMethods.GetCommandInstanceDataPath(this, this), "blendobot-remindme-database.db");
		internal const string TimeFormatString = "d/MM/yyyy h:mm:ss tt";

		private Dictionary<Reminder, Timer> ReminderTimers;
		private Timer DailyReminderCheck;

		private ReminderDatabaseContext GetContext() {
			var optionsBuilder = new DbContextOptionsBuilder<ReminderDatabaseContext>();
			optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
			return new ReminderDatabaseContext(optionsBuilder.Options);
		}

		public override async Task<bool> Startup() {
			ReminderTimers = new Dictionary<Reminder, Timer>();
			using var db = GetContext();
			await db.Database.EnsureCreatedAsync();

			await db.Reminders.Where(r => r.Time < DateTime.UtcNow).ForEachAsync(r => ReminderElapsed(r, true));

			// Check every 12 hours to see if any dormant reminders can be activated. This is because the number of
			// milliseconds to the reminder date may be too large otherwise. The reminders themselves will only activate
			// if they are within 24 hours of the event, so this should be plenty.
			await Task.Factory.StartNew(() => DailyReminderCheckElapsed(this, null));
			DailyReminderCheck = new Timer(43_200_000.0);
			DailyReminderCheck.Elapsed += DailyReminderCheckElapsed;
			DailyReminderCheck.AutoReset = true;
			DailyReminderCheck.Enabled = true;
			return true;
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (DailyReminderCheck != null) {
					DailyReminderCheck.Dispose();
				}
			}
		}

		private async void DailyReminderCheckElapsed(object sender, ElapsedEventArgs e) {
			using var db = GetContext();
			await db.Reminders.Where(r => r.Time < DateTime.UtcNow.AddDays(1)).ForEachAsync(r => CheckAndAddReminderTimer(r));
		}

		private async void ReminderElapsed(Reminder r, bool sleptIn) {
			lock (ReminderTimers) {
				if (ReminderTimers.TryGetValue(r, out Timer timer)) {
					timer.Stop();
					timer.Dispose();
					ReminderTimers.Remove(r);
				}
			}
			await r.UpdateCachedData(BotMethods);
			var sb = new StringBuilder();
			if (!sleptIn) {
				sb.AppendLine($"{r.User.Mention} wanted to know this message now!");
			} else {
				sb.AppendLine($"I just woke up and forgot to send {r.User.Mention} this alert on time!");
			}
			sb.AppendLine(r.Message);
			if (r.IsRepeating) {
				await UpdateReminderTime(r);
				TimeZoneInfo userTimeZone = UserTimeZone.UserTimeZone.GetUserTimeZone(this, r.User);
				sb.AppendLine($"This reminder will repeat on {TimeZoneInfo.ConvertTime(r.Time, userTimeZone).ToString(TimeFormatString)}.");
				//TODO: Dependencies between modules because this fires before UserTimeZone is initialised on startup, resulting in UTC time on sleptIn.
			}
			try {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = sb.ToString(),
					Channel = r.Channel,
					LogMessage = "ReminderAlert"
				});
			} catch (UnauthorizedException) {
				BotMethods.Log(this, new LogEventArgs {
					Type = LogType.Warning,
					Message = $"Tried sending a reminder message {r.Message} which should've sent at {r.Time}, but a 403 was received! This tried to send to user {r.User.Mention} in channel {r.Channel.Mention}."
				});
			}
			if (!r.IsRepeating) {
				await DeleteReminder(r);
			} else {
				CheckAndAddReminderTimer(r);
			}
		}

		public override async Task OnMessage(MessageCreateEventArgs e) {
			// Try and decipher the output.
			string[] splitMessage = e.Message.Content.Split(' ');

			using var db = GetContext();
			
			// The list functionality is separate.
			if (splitMessage.Length >= 2 && splitMessage[1].ToLower() == "list") {
				await SendListMessage(e, false);
				return;
			} else if (splitMessage.Length >= 2 && splitMessage[1].ToLower() == "admin") {
				await SendAdminMessage(e, splitMessage);
				return;
			}

			// If the user has too many reminders, don't let them make another.
			if (db.Reminders.Where(r => r.UserId == e.Author.Id).Count() >= db.Settings.MaximumRemindersPerPerson) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"You have too many outstanding reminders! Please use {Term} list to delete some.",
					Channel = e.Channel,
					LogMessage = "ReminderErrorTooManyReminders"
				});
				return;
			}

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

			// If the every index actually exists but the feature is disabled, users should know.
			if (everyIndex > -1 && db.Settings.MinimumRepeatTime == 0ul) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = "The \"every\" feature is disabled, please try again without that part of the reminder.",
					Channel = e.Channel,
					LogMessage = "ReminderErrorEveryDisabled"
				});
				return;
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
						Message = $"The date/time you input could not be parsed! See {$"{BotMethods.GetHelpCommandTerm(this, GuildId)} {Term}".Code()} for how to format your date/time!",
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
						Message = $"The timespan you input could not be parsed! See {$"{BotMethods.GetHelpCommandTerm(this, GuildId)} {Term}".Code()} for how to format your timespan!",
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
				ulong minimumRepeatTime = db.Settings.MinimumRepeatTime;
				if (frequency < minimumRepeatTime) {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"The every frequency you input was less than {Reminder.FrequencyToReadableString(minimumRepeatTime)}! Please use a longer frequency!",
						Channel = e.Channel,
						LogMessage = "ReminderErrorInvalidEveryTooLow"
					});
					return;
				}
				if (!successfulFormat) {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"The every frequency you input could not be parsed! See {$"{BotMethods.GetHelpCommandTerm(this, GuildId)} {Term}".Code()} for how to format your repeat interval!",
						Channel = e.Channel,
						LogMessage = "ReminderErrorInvalidEvery"
					});
					return;
				}
			}

			// Finally extract the message.
			string message = string.Join(' ', splitMessage.Skip(toIndex + 1));

			// Make the reminder.
			var reminder = new Reminder(TimeZoneInfo.ConvertTimeToUtc(foundTime), message, e.Channel.Id, e.Author.Id, frequency) {
				Channel = e.Channel,
				User = e.Author
			};
			CheckAndAddReminderTimer(reminder);
			db.Reminders.Add(reminder);
			await db.SaveChangesAsync();

			await BotMethods.SendMessage(this, new SendMessageEventArgs {
				Message = $"Okay, I'll tell you this message at {TimeZoneInfo.ConvertTime(foundTime, userTimeZone).ToString(TimeFormatString)} {UserTimeZone.UserTimeZone.ToShortString(userTimeZone)}",
				Channel = e.Channel,
				LogMessage = "ReminderConfirm"
			});
		}

		private async Task SendListMessage(MessageCreateEventArgs e, bool isAdmin) {
			var db = GetContext();
			TimeZoneInfo userTimeZone = UserTimeZone.UserTimeZone.GetUserTimeZone(this, e.Author);
			IQueryable<Reminder> scopedReminders;
			if (isAdmin) {
				scopedReminders = db.Reminders.OrderBy(r => r.Time);
			} else {
				scopedReminders = db.Reminders.Where(r => r.UserId == e.Author.Id).OrderBy(r => r.Time);
			}
			var listListener = new ReminderListListener(this, e.Channel, e.Author, scopedReminders, userTimeZone, isAdmin, db);
			await listListener.CreateMessage();
		}

		private async Task SendAdminMessage(MessageCreateEventArgs e, string[] splitMessage) {
			if (await BotMethods.IsUserAdmin(this, e.Guild, e.Channel, e.Author)) {
				using var db = GetContext();
				if (splitMessage.Length >= 3 && splitMessage[2].ToLower() == "list") {
					await SendListMessage(e, true);
				} else if (splitMessage.Length >= 4 && splitMessage[2].ToLower() == "minrepeattime") {
					if (ulong.TryParse(splitMessage[3], out ulong minRepeatTime)) {
						if (minRepeatTime == 0ul) {
							db.Settings.MinimumRepeatTime = minRepeatTime;
							await db.SaveChangesAsync();
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"Repeat reminders are now disabled. New messages can't use the \"every\" feature.",
								Channel = e.Channel,
								LogMessage = "ReminderSetMinFrequencyOff"
							});
						} else if (minRepeatTime < 300ul) {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"Cannot set a frequency to less than 5 minutes.",
								Channel = e.Channel,
								LogMessage = "ReminderSetMinFrequencyOff"
							});
						} else {
							db.Settings.MinimumRepeatTime = minRepeatTime;
							await db.SaveChangesAsync();
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"Repeat reminders are enabled and the minimum repeat interval is now {Reminder.FrequencyToReadableString(minRepeatTime)}.",
								Channel = e.Channel,
								LogMessage = "ReminderSetMinFrequencyOn"
							});
						}
					} else {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"Could not parse your new minimum frequency.",
							Channel = e.Channel,
							LogMessage = "ReminderErrorInvalidMinFrequency"
						});
					}
				} else if (splitMessage.Length >= 4 && splitMessage[2].ToLower() == "maxreminders") {
					if (int.TryParse(splitMessage[3], out int maxReminders)) {
						if (maxReminders > 0) {
							db.Settings.MaximumRemindersPerPerson = maxReminders;
							await db.SaveChangesAsync();
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"Maximum reminders per person is now {maxReminders}.",
								Channel = e.Channel,
								LogMessage = "ReminderSetMaxReminders"
							});
						} else {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"Maximum reminders cannot be negative.",
								Channel = e.Channel,
								LogMessage = "ReminderErrorMaxRemindersNegative"
							});
						}
					} else {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"Could not parse your new minimum frequency.",
							Channel = e.Channel,
							LogMessage = "ReminderErrorInvalidMinFrequency"
						});
					}
				} else {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"Incorrect syntax! See {$"{BotMethods.GetHelpCommandTerm(this, GuildId)} {Term}".Code()} for what you can do with this command!",
						Channel = e.Channel,
						LogMessage = "ReminderErrorInvalidAdminCommand"
					});
				}
			} else {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"Only administrators can use this feature!",
					Channel = e.Channel,
					LogMessage = "ReminderNotAuthorised"
				});
			}
		}

		private async Task UpdateReminderTime(Reminder reminder) {
			using var db = GetContext();
			reminder.UpdateReminderTime();
			db.Reminders.Single(r => r.ReminderId == reminder.ReminderId).Time = reminder.Time;
			await db.SaveChangesAsync();
		}

		public void CheckAndAddReminderTimer(Reminder reminder) {
			lock (ReminderTimers) {
				if ((reminder.Time > DateTime.UtcNow) && (reminder.Time - DateTime.UtcNow) < new TimeSpan(1, 0, 0, 0) && !ReminderTimers.ContainsKey(reminder)) {
					var timer = new Timer((reminder.Time - DateTime.UtcNow).TotalMilliseconds) {
						AutoReset = false
					};
					timer.Elapsed += (sender, e) => ReminderElapsed(reminder, false);
					timer.Start();
					ReminderTimers.Add(reminder, timer);
				}
			}
		}

		public async Task DeleteReminder(Reminder reminder) {
			using var db = GetContext();
			lock (ReminderTimers) {
				if (ReminderTimers.TryGetValue(reminder, out Timer timer)) {
					timer.Stop();
					timer.Dispose();
					ReminderTimers.Remove(reminder);
				}
			}
			// This check is just in case a reminder is triggered, but a list view later triggers it to be deleted.
			if (db.Reminders.Any(r => r.ReminderId == reminder.ReminderId)) {
				db.Reminders.Remove(reminder);
			}
			await db.SaveChangesAsync();
		}
	}
}

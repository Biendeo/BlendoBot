using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RemindMe {
	public class RemindMe : ICommand {
		CommandProps ICommand.Properties => properties;

		public static readonly CommandProps properties = new CommandProps {
			Term = "?remind",
			Name = "Remind Me",
			Description = "Reminds you about something later on! Please note that I currently do not remember messages if I am restarted.",
			Usage = $"Usage:\n{$"?remind at [date/time] to [message]".Code()} {"(this reminds you at a certain point in time)".Italics()}\n{$"?remind in [timespan] to [message]".Code()} {"(this reminds you after a certain interval)".Italics()}\nValid date/time formats are described here: https://docs.microsoft.com/en-us/dotnet/api/system.datetime.parse?view=netcore-2.1#StringToParse\nValid timespan formats are described here: https://docs.microsoft.com/en-us/dotnet/api/system.timespan.parse?view=netcore-2.1\nPlease note that all date/time strings are interpreted as UTC time unless explicitly stated (i.e. adding {"+11:00".Code()} or such to the format).\nThe output is always formatted as {"d/MM/yyyy h:mm:ss tt K".Code()}",
			Author = "Biendeo",
			Version = "0.1.2",
			Startup = Startup,
			OnMessage = RemindCommand
		};

		private static readonly string DatabasePath = "blendobot-remindme-database.json";

		private static List<Reminder> OutstandingReminders;

		private static async Task<bool> Startup() {
			OutstandingReminders = new List<Reminder>();
			if (File.Exists(DatabasePath)) {
				dynamic json = JsonConvert.DeserializeObject(File.ReadAllText(DatabasePath));
				foreach (var item in json.reminders) {
					var reminder = new Reminder(DateTime.FromFileTimeUtc(item.Time), item.Message, item.Channel, item.User, new Action<Reminder>((Reminder r) => { CleanupReminder(r); }));

					if (reminder.Time < DateTime.UtcNow) {
						await Methods.SendMessage(null, new SendMessageEventArgs {
							Message = $"I just woke up and forgot to send you this alert on time!\n{reminder.Message}",
							Channel = reminder.Channel,
							LogMessage = "ReminderLateAlert"
						});
					} else {
						reminder.Activate();
						OutstandingReminders.Add(reminder);
					}
				}

				OutstandingReminders.Sort();
				SaveReminders();
			}
			return true;
		}

		private static void SaveReminders() {
			JsonConvert.SerializeObject(OutstandingReminders);
		}

		private static void CleanupReminder(Reminder reminder) {
			OutstandingReminders.Remove(reminder);
			SaveReminders();
		}

		public static async Task RemindCommand(MessageCreateEventArgs e) {
			// Try and decipher the output.
			var splitMessage = e.Message.Content.Split(' ');

			// Try and look for the "to" index.
			int toIndex = 0;
			while (toIndex < splitMessage.Length && splitMessage[toIndex] != "to") {
				++toIndex;
			}

			if (toIndex == splitMessage.Length) {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = "Incorrect syntax, make sure you use the word \"to\" after you indicate the time you want the reminder!",
					Channel = e.Channel,
					LogMessage = "ReminderErrorNoTo"
				});
				return;
			} else if (toIndex == splitMessage.Length - 1) {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = "Incorrect syntax, make sure you type a message after that \"to\"!",
					Channel = e.Channel,
					LogMessage = "ReminderErrorNoMessage"
				});
				return;
			}

			// Now decipher the time.
			DateTime foundTime = DateTime.Now;
			if (splitMessage[1] == "at") {
				try {
					foundTime = DateTime.Parse(string.Join(' ', splitMessage.Skip(2).Take(toIndex - 2)));
				} catch (FormatException) {
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = $"The date/time you input could not be parsed! See {"?help remind".Code()} for how to format your date/time!",
						Channel = e.Channel,
						LogMessage = "ReminderErrorInvalidTime"
					});
					return;
				}
				if (foundTime < DateTime.Now) {
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = $"The time you input was parsed as {foundTime.ToString("d/MM/yyyy h:mm:ss tt K")}, which is in the past! Make your time a little more specific!",
						Channel = e.Channel,
						LogMessage = "ReminderErrorPastTime"
					});
					return;
				}
			} else  if (splitMessage[1] == "in") {
				try {
					var span = TimeSpan.Parse(string.Join(' ', splitMessage.Skip(2).Take(toIndex - 2)));
					foundTime = DateTime.Now + span;
				} catch (FormatException) {
					await Methods.SendMessage(null, new SendMessageEventArgs {
						Message = $"The timespan you input could not be parsed! See {"?help remind".Code()} for how to format your timespan!",
						Channel = e.Channel,
						LogMessage = "ReminderErrorInvalidTimespan"
					});
					return;
				}
			} else {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = "Incorrect syntax, make sure you use the word \"in\" or \"at\" to specify a time for the reminder!",
					Channel = e.Channel,
					LogMessage = "ReminderErrorNoAt"
				});
				return;
			}

			// Finally extract the message.
			string message = string.Join(' ', splitMessage.Skip(toIndex + 1));

			// Make the reminder.
			var reminder = new Reminder(foundTime, message, e.Channel, e.Author, new Action<Reminder>((Reminder r) => { CleanupReminder(r); }));
			reminder.Activate();
			OutstandingReminders.Add(reminder);
			//SaveReminders();

			await Methods.SendMessage(null, new SendMessageEventArgs {
				Message = $"Okay, I'll tell you this message at {foundTime.ToString("d/MM/yyyy h:mm:ss tt K")}",
				Channel = e.Channel,
				LogMessage = "ReminderConfirm"
			});
		}
	}
}

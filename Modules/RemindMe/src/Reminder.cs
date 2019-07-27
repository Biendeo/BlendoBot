using BlendoBotLib;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace RemindMe {
	public class Reminder : IComparable<Reminder> {
		public DateTime Time { get; set; }
		public string Message { get; set; }
		public DiscordChannel Channel { get; set; }
		public DiscordUser User { get; set; }
		public Timer CallbackTimer { get; set; }
		public Action<Reminder> ParentContainerCallback { get; set; }

		public Reminder(DateTime time, string message, DiscordChannel channel, DiscordUser user, Action<Reminder> parentContainerCallback) {
			Time = time;
			Message = message;
			Channel = channel;
			User = user;
			if (time > DateTime.Now) {
				CallbackTimer = new Timer((time - DateTime.Now).TotalMilliseconds);
			} else {
				CallbackTimer = new Timer(int.MaxValue);
			}
			CallbackTimer.AutoReset = false;
			CallbackTimer.Elapsed += TimerElapsed;
			ParentContainerCallback = parentContainerCallback;
		}

		public void Activate() {
			CallbackTimer.Start();
		}

		private async void TimerElapsed(object sender, ElapsedEventArgs e) {
			await Methods.SendMessage(null, new SendMessageEventArgs {
				Message = $"<@{User.Id}> wanted to know this message now!\n{Message}",
				Channel = Channel,
				LogMessage = "ReminderAlert"
			});
			CallbackTimer.Dispose();
			ParentContainerCallback(this);
		}

		public int CompareTo(Reminder other) {
			return Time.CompareTo(other.Time);
		}
	}
}

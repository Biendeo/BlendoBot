using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Timers;

namespace RemindMe {
	[JsonObject(MemberSerialization.OptIn)]
	public class Reminder : IComparable<Reminder>, IDisposable {
		[JsonProperty(Required = Required.Always)]
		public DateTime Time { get; set; }
		[JsonProperty(Required = Required.Always)]
		public string Message { get; set; }
		[JsonProperty(Required = Required.Always)]
		public ulong ChannelId { get; set; }
		[JsonProperty(Required = Required.Always)]
		public ulong UserId { get; set; }
		public Timer CallbackTimer { get; set; }

		protected Reminder() {}

		public Reminder(DateTime time, string message, ulong channelId, ulong userId) {
			Time = time;
			Message = message;
			ChannelId = channelId;
			UserId = userId;
			CallbackTimer = null;
		}

		public void Activate(Action<object, ElapsedEventArgs, Reminder> action) {
			if (CallbackTimer == null && (Time > DateTime.UtcNow) && (Time - DateTime.UtcNow) < new TimeSpan(1, 0, 0, 0)) {
				CallbackTimer = new Timer((Time - DateTime.UtcNow).TotalMilliseconds);
				CallbackTimer.AutoReset = false;
				CallbackTimer.Elapsed += (sender, e) => action(sender, e, this);
				CallbackTimer.Start();
			}
		}

		public int CompareTo(Reminder other) {
			return Time.CompareTo(other.Time);
		}
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (CallbackTimer != null) {
					CallbackTimer.Dispose();
				}
			}
		}
	}
}

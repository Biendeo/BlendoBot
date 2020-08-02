using BlendoBotLib;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
		public DiscordChannel Channel { get; set; }
		[JsonProperty(Required = Required.Always)]
		public ulong UserId { get; set; }
		public DiscordUser User { get; set; }
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
		public ulong Frequency { get; set; } = 0ul;
		public string FrequencyString => FrequencyToReadableString(Frequency);
		public bool IsRepeating => Frequency > 0ul;
		public Timer CallbackTimer { get; set; }

		protected Reminder() {}

		public Reminder(DateTime time, string message, ulong channelId, ulong userId, ulong frequency) {
			Time = time;
			Message = message;
			ChannelId = channelId;
			UserId = userId;
			Frequency = frequency;
			CallbackTimer = null;
			Channel = null;
			User = null;
		}

		public async Task UpdateCachedData(IBotMethods botMethods) {
			Channel = await botMethods.GetChannel(this, ChannelId);
			User = await botMethods.GetUser(this, UserId);
		}

		public void Activate(Action<object, ElapsedEventArgs, Reminder> action) {
			if (CallbackTimer == null && (Time > DateTime.UtcNow) && (Time - DateTime.UtcNow) < new TimeSpan(1, 0, 0, 0)) {
				CallbackTimer = new Timer((Time - DateTime.UtcNow).TotalMilliseconds) {
					AutoReset = false
				};
				CallbackTimer.Elapsed += (sender, e) => action(sender, e, this);
				CallbackTimer.Start();
			}
		}

		public int CompareTo(Reminder other) {
			return Time.CompareTo(other?.Time);
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

		public override bool Equals(object obj) {
			if (ReferenceEquals(this, obj)) {
				return true;
			}

			if (obj is null) {
				return false;
			}

			return CompareTo(obj as Reminder) == 0;
		}

		public override int GetHashCode() {
			return Time.GetHashCode();
		}

		public static bool operator ==(Reminder left, Reminder right) {
			if (left is null) {
				return right is null;
			}

			return left.Equals(right);
		}

		public static bool operator !=(Reminder left, Reminder right) {
			return !(left == right);
		}

		public static bool operator <(Reminder left, Reminder right) {
			return left is null ? right is object : left.CompareTo(right) < 0;
		}

		public static bool operator <=(Reminder left, Reminder right) {
			return left is null || left.CompareTo(right) <= 0;
		}

		public static bool operator >(Reminder left, Reminder right) {
			return left is object && left.CompareTo(right) > 0;
		}

		public static bool operator >=(Reminder left, Reminder right) {
			return left is null ? right is null : left.CompareTo(right) >= 0;
		}

		public static string FrequencyToReadableString(ulong frequency) {
			if (frequency % 86400 == 0) {
				ulong quantity = frequency / 86400;
				return $"{quantity} day{(quantity == 1 ? string.Empty : "s")}";
			} else if (frequency % 3600 == 0) {
				ulong quantity = frequency / 3600;
				return $"{quantity} hour{(quantity == 1 ? string.Empty : "s")}";
			} else if (frequency % 60 == 0) {
				ulong quantity = frequency / 60;
				return $"{quantity} minute{(quantity == 1 ? string.Empty : "s")}";
			} else {
				return $"{frequency} second{(frequency == 1 ? string.Empty : "s")}";
			}
		}
	}
}

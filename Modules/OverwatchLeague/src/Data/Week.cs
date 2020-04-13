using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OverwatchLeague.Data {
	public class Week {
		public int WeekNumber { get; private set; }
		public string Name { get; private set; }
		private readonly List<Event> events;
		public ReadOnlyCollection<Event> Events => events.AsReadOnly();

		public bool IsCurrent => events.Exists(e => e.IsCurrent);

		public DateTime FirstStartTime => events.Count == 0 ? DateTime.MaxValue : events.Min(e => e.FirstStartTime);

		public DateTime LastEndTime => events.Count == 0 ? DateTime.MinValue : events.Max(e => e.LastEndTime);

		public Week(int weekNumber, string name) {
			WeekNumber = weekNumber;
			Name = name;
			events = new List<Event>();
		}

		public void AddEvent(Event e) {
			events.Add(e);
		}
	}
}

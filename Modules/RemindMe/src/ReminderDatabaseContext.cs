using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemindMe {
	public class ReminderDatabaseContext : DbContext {
		public ReminderDatabaseContext(DbContextOptions<ReminderDatabaseContext> options) : base(options) { }
		public DbSet<Reminder> Reminders { get; set; }
		private DbSet<Settings> SettingsSet { get; set; }
		public Settings Settings {
			get {
				if (SettingsSet.Count() == 0) {
					SettingsSet.Add(new Settings() {
						MinimumRepeatTime = 300ul,
						MaximumRemindersPerPerson = 20
					});
					SaveChanges();
				}
				return SettingsSet.Single();
			}
		}
	}
}

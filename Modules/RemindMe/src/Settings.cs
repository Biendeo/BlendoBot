using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RemindMe {
	public class Settings {
		[Key]
		public int SettingsId { get; set; }
		public ulong MinimumRepeatTime { get; set; }
		public int MaximumRemindersPerPerson { get; set; }
	}
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RemindMe {
	public class Settings {
		[JsonProperty(Required = Required.Always)]
		public ulong MinimumRepeatTime { get; set; }
		[JsonProperty(Required = Required.Always)]
		public int MaximumRemindersPerPerson { get; set; }
	}
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlendoBotTCG.Data {
	[JsonObject(MemberSerialization.OptIn)]
	internal class Pack {
		public Pack(string name, TimeSpan availableInterval) {
			Name = name;
			AvailableInterval = availableInterval;
		}

		[JsonProperty(Required = Required.Always)]
		public string Name { get; set; }
		[JsonProperty(Required = Required.Always)]
		public TimeSpan AvailableInterval { get; set; }
	}
}

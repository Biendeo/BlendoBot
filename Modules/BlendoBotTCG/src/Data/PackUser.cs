using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlendoBotTCG.Data {
	[JsonObject(MemberSerialization.OptIn)]
	internal class PackUser {
		public PackUser(ulong userId) {
			UserId = userId;
			LastOpen = DateTime.Now;
		}

		[JsonProperty(Required = Required.Always)]
		public ulong UserId { get; }
		[JsonProperty(Required = Required.Always)]
		public DateTime LastOpen { get; private set; }

		public void UpdateOpenNow() {
			LastOpen = DateTime.Now;
		}
	}
}

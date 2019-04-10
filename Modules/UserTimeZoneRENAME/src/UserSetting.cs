using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace UserTimeZone {
	[JsonObject(MemberSerialization.OptIn)]
	internal class UserSetting {
		[JsonProperty(Required = Required.Always)]
		internal string Username;
		[JsonProperty(Required = Required.Always)]
		internal string Discriminator;
		[JsonProperty(Required = Required.Always)]
		internal TimeZoneInfo TimeZone;
	}
}

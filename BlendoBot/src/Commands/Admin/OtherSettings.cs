using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlendoBot.Commands.Admin {
	[JsonObject(MemberSerialization.OptIn)]
	class OtherSettings {
		[JsonProperty(Required = Required.Default)]
		public string UnknownCommandPrefix { get; set; } = "?";
		[JsonProperty(Required = Required.Default)]
		public bool IsUnknownCommandEnabled { get; set; } = true;
	}
}

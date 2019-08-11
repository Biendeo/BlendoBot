using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlendoBot.Commands.Admin {
	[JsonObject(MemberSerialization.OptIn)]
	class DisabledCommand {
		public DisabledCommand(string term, string className) {
			Term = term;
			ClassName = className;
		}

		[JsonProperty(Required = Required.Always)]
		public string Term { get; }
		[JsonProperty(Required = Required.Always)]
		public string ClassName { get; }
	}
}

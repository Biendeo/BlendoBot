using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBotLib.Attributes {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class CommandAttribute : Attribute {
		public string DefaultTerm;
		public string Name;
		public string Description;
		public string Author;
		public string Version;

		public CommandAttribute(string defaultTerm, string name, string description, string author, string version) {
			DefaultTerm = defaultTerm;
			Name = name;
			Description = description;
			Author = author;
			Version = version;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Text;

namespace BlendoBotLib {
	public static class Formatting {
		public static string Bold(this string s) {
			return $"**{s}**";
		}

		public static string Italics(this string s) {
			return $"*{s}*";
		}

		public static string Underline(this string s) {
			return $"__{s}__";
		}

		public static string Strikeout(this string s) {
			return $"~~{s}~~";
		}

		public static string Code(this string s) {
			return $"`{s}`";
		}

		public static string CodeBlock(this string s) {
			return $"```{s}```";
		}
	}
}

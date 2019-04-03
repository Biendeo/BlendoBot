using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlendoBot {
	//TODO: This is probably a pointless file because most of the bot now interacts with the lib.
	public static class Resources {
		public static readonly string ResFolder = Path.Join(Directory.GetCurrentDirectory(), "BlendoBot/res");

		public static byte[] ReadImage(string imagePath) {
			return File.ReadAllBytes(Path.Join(ResFolder, imagePath));
		}
	}
}

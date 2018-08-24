using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlendoBot {
	public static class Resources {
		public static readonly string ResFolder = Path.Join(Directory.GetCurrentDirectory(), "BlendoBot/res");

		public static byte[] ReadImage(string imagePath) {
			return File.ReadAllBytes(Path.Join(ResFolder, imagePath));
		}

		public static byte[] MrPingTemplate {
			get { return ReadImage("mr.png"); }
		}
	}
}

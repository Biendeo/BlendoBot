using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BlendoBotTCG.Data {
	[JsonObject(MemberSerialization.OptIn)]
	internal class Card {
		public Card(string name, string imagePath, string id) {
			Name = name;
			ImagePath = imagePath;
			ID = id;
		}

		[JsonProperty(Required = Required.Always)]
		public string Name { get; }
		[JsonProperty(Required = Required.Always)]
		public string ImagePath { get; }
		[JsonProperty(Required = Required.Always)]
		public string ID { get; }
		public string DeleteHash {
			get {
				using (var md5 = MD5.Create()) {
					var sb = new StringBuilder();
					byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(Name + ID));
					foreach (byte b in hash) {
						sb.Append(b.ToString("x2"));
					}
					return sb.ToString().Substring(0, 8);
				}
			}
		}
	}
}

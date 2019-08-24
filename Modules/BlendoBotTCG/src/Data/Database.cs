using BlendoBotLib;
using BlendoBotTCG.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace BlendoBotTCG.Data {
	internal class Database {
		public Database(BlendoBotTCG tcg) {
			cards = new List<Card>();

			this.tcg = tcg;

			LoadDatabase();
		}

		public ReadOnlyCollection<Card> Cards { get { return cards.AsReadOnly(); } }
		private List<Card> cards;

		private BlendoBotTCG tcg;

		private void LoadDatabase() {
			if (File.Exists(Path.Combine(tcg.BotMethods.GetCommandDataPath(this, tcg), "cards.json"))) {
				cards = JsonConvert.DeserializeObject<List<Card>>(File.ReadAllText(Path.Combine(tcg.BotMethods.GetCommandDataPath(this, tcg), "cards.json")));
			}
		}

		private void SaveDatabase() {
			File.WriteAllText(Path.Combine(tcg.BotMethods.GetCommandDataPath(this, tcg), "cards.json"), JsonConvert.SerializeObject(cards));
		}

		public string GetNewCardID() {
			var sb = new StringBuilder();
			var random = new Random();
			char[] validChars = new char[] { 'B', 'C', 'D', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'Q', 'R', 'S', 'T', 'V', 'W', 'X', 'Y', 'Z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
			do {
				sb.Clear();
				for (int i = 0; i < 5; ++i) {
					sb.Append(validChars[random.Next(0, validChars.Length)]);
				}
			} while (cards.Exists(c => c.ID == sb.ToString()));

			return sb.ToString();
		}

		public bool AddCard(string name, string imagePath, string id) {
			if (!cards.Exists(c => c.ID == id)) {
				var card = new Card(name, imagePath, id);
				cards.Add(card);
				SaveDatabase();
				return true;
			} else {
				return false;
			}
		}

		public bool DeleteCard(Card card) {
			if (cards.Contains(card)) {
				cards.Remove(card);
				SaveDatabase();
				return true;
			} else {
				return false;
			}
		}
	}
}
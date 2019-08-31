using BlendoBotLib;
using BlendoBotTCG.Data;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace BlendoBotTCG.Data {
	internal class Database {
		public Database(BlendoBotTCG tcg) {
			cards = new List<Card>();
			packs = new List<Pack>();

			this.tcg = tcg;

			LoadDatabase();
		}

		public ReadOnlyCollection<Card> Cards { get { return cards.AsReadOnly(); } }
		private List<Card> cards;
		public ReadOnlyCollection<Pack> Packs { get { return packs.AsReadOnly(); } }
		private List<Pack> packs;

		private BlendoBotTCG tcg;

		public enum AddCardResult {
			Success,
			CardAlreadyExists
		}
		public enum DeleteCardResult {
			Success,
			CardDoesNotExist
		}
		public enum AddPackResult {
			Success,
			PackNameAlreadyExists,
			InvalidInterval
		}
		public enum RemovePackResult {
			Success,
			PackDoesNotExist,
		}
		public enum AddPackCardResult {
			Success,
			PackDoesNotExist,
			CardAlreadyInPack,
			CardDoesNotExist
		}
		public enum OpenPackResult {
			Success,
			PackDoesNotExist,
			CooldownStillActive
		}

		private void LoadDatabase() {
			if (File.Exists(Path.Combine(tcg.BotMethods.GetCommandDataPath(this, tcg), "cards.json"))) {
				cards = JsonConvert.DeserializeObject<List<Card>>(File.ReadAllText(Path.Combine(tcg.BotMethods.GetCommandDataPath(this, tcg), "cards.json")));
			}
			if (File.Exists(Path.Combine(tcg.BotMethods.GetCommandDataPath(this, tcg), "packs.json"))) {
				packs = JsonConvert.DeserializeObject<List<Pack>>(File.ReadAllText(Path.Combine(tcg.BotMethods.GetCommandDataPath(this, tcg), "packs.json")));
			}
		}

		private void SaveDatabase() {
			File.WriteAllText(Path.Combine(tcg.BotMethods.GetCommandDataPath(this, tcg), "cards.json"), JsonConvert.SerializeObject(cards));
			File.WriteAllText(Path.Combine(tcg.BotMethods.GetCommandDataPath(this, tcg), "packs.json"), JsonConvert.SerializeObject(packs));
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

		public AddCardResult AddCard(string name, string imagePath, string id) {
			if (!cards.Exists(c => c.ID == id)) {
				var card = new Card(name, imagePath, id);
				cards.Add(card);
				SaveDatabase();
				return AddCardResult.Success;
			} else {
				return AddCardResult.CardAlreadyExists;
			}
		}

		public DeleteCardResult DeleteCard(Card card) {
			if (cards.Contains(card)) {
				cards.Remove(card);
				foreach (var pack in packs) {
					pack.RemoveCard(card.ID);
				}
				SaveDatabase();
				return DeleteCardResult.Success;
			} else {
				return DeleteCardResult.CardDoesNotExist;
			}
		}

		public DeleteCardResult DeleteCard(string cardId) {
			var card = cards.Find(c => c.ID == cardId);
			if (card != null) {
				cards.Remove(card);
				foreach (var pack in packs) {
					pack.RemoveCard(card.ID);
				}
				SaveDatabase();
				return DeleteCardResult.Success;
			} else {
				return DeleteCardResult.CardDoesNotExist;
			}
		}

		public AddPackResult AddPack(string name) {
			return AddPack(name, new TimeSpan(0));
		}

		public AddPackResult AddPack(string name, TimeSpan availabilityInterval) {
			if (packs.Exists(p => p.Name == name)) {
				return AddPackResult.PackNameAlreadyExists;
			} else if (availabilityInterval < new TimeSpan(0)) {
				return AddPackResult.InvalidInterval;
			} else {
				packs.Add(new Pack(name, availabilityInterval));
				SaveDatabase();
				return AddPackResult.Success;
			}
		}

		public RemovePackResult RemovePack(Pack pack) {
			if (packs.Contains(pack)) {
				SaveDatabase();
				return RemovePackResult.Success;
			} else {
				return RemovePackResult.PackDoesNotExist;
			}
		}

		public RemovePackResult RemovePack(int packId) {
			if (packId <= 0 || packId > packs.Count) {
				return RemovePackResult.PackDoesNotExist;
			} else {
				packs.RemoveAt(packId - 1);
				SaveDatabase();
				return RemovePackResult.Success;
			}
		}

		public AddPackCardResult AddPackCard(string cardId, int packId, int rarity) {
			if (packId <= 0 || packId > packs.Count) {
				return AddPackCardResult.PackDoesNotExist;
			}
			var pack = packs[packId - 1];
			var card = cards.Find(c => c.ID.ToLower() == cardId.ToLower());
			if (card == null) {
				return AddPackCardResult.CardDoesNotExist;
			}
			var drop = pack.Drops.FirstOrDefault(d => d.CardID == cardId);
			if (drop != default) {
				drop.Rarity = (rarity < 0) ? 0 : rarity;
				return AddPackCardResult.CardAlreadyInPack;
			} else {
				pack.AddCard(card, (rarity < 0) ? 0 : rarity);
				return AddPackCardResult.Success;
			}
		}

		public OpenPackResult OpenPack(DiscordUser user, int packId, out Card card) {
			if (packId <= 0 || packId > packs.Count) {
				card = null;
				return OpenPackResult.PackDoesNotExist;
			}
			var pack = packs[packId - 1];
			if (pack.CanOpenPack(user)) {
				card = pack.OpenPack(user);
				//TODO: Add inventory.
				SaveDatabase();
				return OpenPackResult.Success;
			} else {
				card = null;
				return OpenPackResult.CooldownStillActive;
			}
		}
	}
}
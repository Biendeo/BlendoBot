using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlendoBotTCG.Data {
	[JsonObject(MemberSerialization.OptIn)]
	internal class PackCardRarity {
		public PackCardRarity(string cardID, int rarity) {
			CardID = cardID;
			Rarity = rarity;
			Card = null;
		}
		public PackCardRarity(Card card, int rarity) {
			CardID = card.ID;
			Rarity = rarity;
			Card = card;
		}

		[JsonProperty(Required = Required.Always)]
		public string CardID { get; }
		[JsonProperty(Required = Required.Always)]
		public int Rarity { get; set; }
		public Card Card { get; private set; }

		public void AssociateCard(Card card) {
			Card = card;
		}
	}
}

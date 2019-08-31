using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace BlendoBotTCG.Data {
	[JsonObject(MemberSerialization.OptIn)]
	internal class Pack {
		public Pack(string name, TimeSpan availabilityInterval) {
			Name = name;
			AvailabilityInterval = availabilityInterval;
			drops = new List<PackCardRarity>();
			users = new List<PackUser>();
		}

		[JsonProperty(Required = Required.Always)]
		public string Name { get; }
		[JsonProperty(Required = Required.Always)]
		public TimeSpan AvailabilityInterval { get; }
		public ReadOnlyCollection<PackCardRarity> Drops { get => drops.AsReadOnly(); }
		[JsonProperty(Required = Required.Always)]
		public List<PackCardRarity> drops;
		public ReadOnlyCollection<PackUser> Users { get => users.AsReadOnly(); }
		[JsonProperty(Required = Required.Always)]
		public List<PackUser> users;

		public enum AddCardResult {
			Success,
			CardAlreadyExists,
			InvalidRarity
		}

		public enum RemoveCardResult {
			Success,
			CardDoesNotExist
		}

		public AddCardResult AddCard(Card card, int rarity) {
			if (drops.Exists(d => d.CardID == card.ID)) {
				return AddCardResult.CardAlreadyExists;
			} else if (rarity <= 0) {
				return AddCardResult.InvalidRarity;
			} else {
				drops.Add(new PackCardRarity(card, rarity));
				return AddCardResult.Success;
			}
		}

		public RemoveCardResult RemoveCard(string cardID) {
			var drop = drops.Find(d => d.CardID == cardID);
			if (drop == null) {
				return RemoveCardResult.CardDoesNotExist;
			} else {
				drops.Remove(drop);
				return RemoveCardResult.Success;
			}
		}

		public TimeSpan CooldownTimeRemaining(DateTime lastUse) {
			return lastUse + AvailabilityInterval - DateTime.Now;
		}

		public bool IsAvailable(DateTime lastUse) {
			return AvailabilityInterval > new TimeSpan(0) && CooldownTimeRemaining(lastUse) < new TimeSpan(0);
		}

		public void AssociateCards(List<Card> cards) {
			foreach (var drop in drops) {
				drop.AssociateCard(cards.Find(c => c.ID == drop.CardID));
			}
		}

		public bool CanOpenPack(DiscordUser user) {
			var packUser = users.Find(u => u.UserId == user.Id);
			if (packUser == null) {
				return true;
			} else {
				return IsAvailable(packUser.LastOpen);
			}
		}

		public Card OpenPack(DiscordUser user) {
			if (!CanOpenPack(user)) {
				return null;
			} else {
				var packUser = users.Find(u => u.UserId == user.Id);
				if (packUser == null) {
					users.Add(new PackUser(user.Id));
				} else {
					packUser.UpdateOpenNow();
				}
				return GetRandomCard();
			}
		}

		public Card GetRandomCard() {
			var cardDistribution = new List<Card>();
			foreach (var drop in drops) {
				for (int i = 0; i < drop.Rarity; ++i) {
					//? This might be a little inefficient.
					cardDistribution.Add(drop.Card);
				}
			}
			var r = new Random();
			return cardDistribution[r.Next(0, cardDistribution.Count)];
		}
	}
}

using BlendoBotLib;
using BlendoBotTCG.Data;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBotTCG {
	public class BlendoBotTCG : CommandBase {
		public BlendoBotTCG(ulong guildId, IBotMethods botMethods) : base(guildId, botMethods) { }
		public override string Term => "?bbtcg";
		public override string Name => "BlendoBot Trading Card Game";
		public override string Description => "Collect and trade cards for your server!";
		public override string Usage => $"DO THIS WHEN FINISHED!";
		public override string Author => "Biendeo";
		public override string Version => "1.0.0";

		private Database database;

		public override async Task<bool> Startup() {
			database = new Database(this);
			await Task.Delay(0);
			return true;
		}

		public override async Task OnMessage(MessageCreateEventArgs e) {
			string[] splitString = e.Message.Content.Split(' ');
			if (splitString.Length >= 2) {
				if (splitString[1] == "card") {
					await OnMessageCard(e);
				} else if (splitString[1] == "pack") {
					await OnMessagePack(e);
				} else {
					await InvalidCommand(e.Channel);
				}
			} else {
				await InvalidCommand(e.Channel);
			}
		}

		private async Task OnMessageCard(MessageCreateEventArgs e) {
			string[] splitString = e.Message.Content.Split(' ');
			if (splitString.Length >= 3) {
				if (splitString[2] == "add") {
					await OnMessageCardAdd(e);
				} else if (splitString[2] == "delete") {
					await OnMessageCardDelete(e);
				} else if (splitString[2] == "list") {
					await OnMessageCardList(e);
				} else if (splitString[2] == "view") {
					await OnMessageCardView(e);
				} else {
					await InvalidCommand(e.Channel);
				}
			} else {
				await InvalidCommand(e.Channel);
			}
		}

		private async Task OnMessageCardAdd(MessageCreateEventArgs e) {
			string[] splitString = e.Message.Content.Split(' ');
			if (await BotMethods.IsUserAdmin(this, e.Guild, e.Channel, e.Author)) {
				if (splitString.Length <= 3) {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"Please add a name for the card after {"?bbtcg card add".Code()}",
						Channel = e.Channel,
						LogMessage = "BBTCGCardAddNoName"
					});
				} else if (e.Message.Attachments.Count == 1) {
					if (IsFileValidImageType(e.Message.Attachments[0].FileName)) {
						if (!Directory.Exists(Path.Combine(BotMethods.GetCommandDataPath(this, this), "image"))) {
							Directory.CreateDirectory(Path.Combine(BotMethods.GetCommandDataPath(this, this), "image"));
						}
						string cardId = database.GetNewCardID();
						string fileOutputPath = Path.Combine(Path.Combine(BotMethods.GetCommandDataPath(this, this), "image"), $"{cardId}{Path.GetExtension(e.Message.Attachments[0].FileName)}");
						using (var wc = new WebClient()) {
							using (var imageOut = File.Open(fileOutputPath, FileMode.OpenOrCreate)) {
								using (var writer = new BinaryWriter(imageOut)) {
									writer.Write(wc.DownloadData(e.Message.Attachments[0].Url));
								}
							}
							if (database.AddCard(string.Join(' ', splitString.Skip(3)), fileOutputPath, cardId) == Database.AddCardResult.Success) {
								await BotMethods.SendMessage(this, new SendMessageEventArgs {
									Message = $"Successfully added a new card {cardId.Code()}",
									Channel = e.Channel,
									LogMessage = "BBTCGCardAddSuccess"
								});
							} else {
								await BotMethods.SendMessage(this, new SendMessageEventArgs {
									Message = $"Uh oh, I couldn't add the card for some internal reason...",
									Channel = e.Channel,
									LogMessage = "BBTCGCardAddUnknownError"
								});
							}
						}
					} else {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"The image uploaded did not end with {".gif".Code()}, {".jpg".Code()}, {".jpeg".Code()}, or {".png".Code()}. Please try again with a different file.",
							Channel = e.Channel,
							LogMessage = "BBTCGCardAddInvalidFileFormat"
						});
					}
				} else {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"Please upload a single image, and use {e.Message.Content.ToString().Code()} as the comment",
						Channel = e.Channel,
						LogMessage = "BBTCGCardAddNoAttachments"
					});
				}
			} else {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"This command can only be used by administrators of this guild",
					Channel = e.Channel,
					LogMessage = "BBTCGCardAddNotAdmin"
				});
			}
		}

		private async Task OnMessageCardDelete(MessageCreateEventArgs e) {
			string[] splitString = e.Message.Content.Split(' ');
			if (await BotMethods.IsUserAdmin(this, e.Guild, e.Channel, e.Author)) {
				if (splitString.Length <= 3) {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"Please add an ID for the card after {"?bbtcg card delete".Code()}",
						Channel = e.Channel,
						LogMessage = "BBTCGCardDeleteNoID"
					});
				} else {
					var selectedCard = database.Cards.FirstOrDefault(c => c.ID.ToLower() == splitString[3].ToLower());
					if (selectedCard == default) {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"Card of ID {splitString[3].Code()} could not be found, make sure the card ID exists in {"?bbtcg card list".Code()}",
							Channel = e.Channel,
							LogMessage = "BBTCGCardDeleteInvalidID"
						});
					} else {
						if (splitString.Length == 4 || splitString[4].ToLower() != selectedCard.DeleteHash.ToLower()) {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"To confirm deleting the card {selectedCard.Name.Italics()}, please send {$"{string.Join(' ', splitString.Take(4))} {selectedCard.DeleteHash}".Code()}",
								Channel = e.Channel,
								LogMessage = "BBTCGCardDeleteConfirmation"
							});
						} else {
							if (database.DeleteCard(selectedCard) == Database.DeleteCardResult.Success) {
								await BotMethods.SendMessage(this, new SendMessageEventArgs {
									Message = $"Successfully deleted card {selectedCard.Name.Italics()}",
									Channel = e.Channel,
									LogMessage = "BBTCGCardDeleteSuccess"
								});
							} else {
								await BotMethods.SendMessage(this, new SendMessageEventArgs {
									Message = $"For some reason an error occurred and card {selectedCard.Name.Italics()} was not successfully deleted",
									Channel = e.Channel,
									LogMessage = "BBTCGCardDeleteUnknownError"
								});
							}
						}
					}
				}
			} else {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"This command can only be used by administrators of this guild",
					Channel = e.Channel,
					LogMessage = "BBTCGCardDeleteNotAdmin"
				});
			}
		}

		private async Task OnMessageCardList(MessageCreateEventArgs e) {
			var sb = new StringBuilder();
			if (database.Cards.Any()) {
				sb.AppendLine("Available cards:");
				foreach (var card in database.Cards) {
					sb.AppendLine($"{$"[{card.ID}]".Code()} {card.Name}");
				}
			} else {
				sb.AppendLine("No cards have been added yet");
			}
			await BotMethods.SendMessage(this, new SendMessageEventArgs {
				Message = sb.ToString(),
				Channel = e.Channel,
				LogMessage = "BBTCGCardList"
			});
		}

		private async Task OnMessageCardView(MessageCreateEventArgs e) {
			string[] splitString = e.Message.Content.Split(' ');
			if (splitString.Length >= 4) {
				var card = database.Cards.FirstOrDefault(c => c.ID.ToLower() == splitString[3].ToLower());
				if (card == default) {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"Card of ID {splitString[3].Code()} could not be found, make sure the card ID exists in {"?bbtcg card list".Code()}",
						Channel = e.Channel,
						LogMessage = "BBTCGCardViewInvalidID"
					});
				} else {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"Card {$"[{card.ID}]".Code()} - {card.Name}",
						Channel = e.Channel,
						LogMessage = "BBTCGCardViewSuccessMessage"
					});
					await BotMethods.SendFile(this, new SendFileEventArgs {
						FilePath = card.ImagePath,
						Channel = e.Channel,
						LogMessage = "BBTCGCardViewSuccessImage"
					});
				}
			} else {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"Please supply a card ID to view with {"?bbtcg card view [ID]".Code()}",
					Channel = e.Channel,
					LogMessage = "BBTCGCardViewNoID"
				});
			}
		}

		private async Task OnMessagePack(MessageCreateEventArgs e) {
			string[] splitString = e.Message.Content.Split(' ');
			if (splitString.Length >= 3) {
				if (splitString[2] == "add") {
					await OnMessagePackAdd(e);
				} else if (splitString[2] == "delete") {
					await OnMessagePackDelete(e);
				} else if (splitString[2] == "card") {
					await OnMessagePackCard(e);
				} else if (splitString[2] == "list") {
					await OnMessagePackList(e);
				} else if (splitString[2] == "open") {
					await OnMessagePackOpen(e);
				} else {
					await InvalidCommand(e.Channel);
				}
			} else {
				await InvalidCommand(e.Channel);
			}
		}

		private async Task OnMessagePackAdd(MessageCreateEventArgs e) {
			string[] splitString = e.Message.Content.Split(' ');
			if (await BotMethods.IsUserAdmin(this, e.Guild, e.Channel, e.Author)) {
				if (splitString.Length >= 4) {
					string packName = string.Join(' ', splitString.Skip(3));
					var result = database.AddPack(packName);
					if (result == Database.AddPackResult.Success) {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"New pack with ID {database.Packs.Count} added",
							Channel = e.Channel,
							LogMessage = "BBTCGPackAddSuccess"
						});
					} else if (result == Database.AddPackResult.PackNameAlreadyExists) {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"A pack with that name already exists!",
							Channel = e.Channel,
							LogMessage = "BBTCGPackAddExistingName"
						});
					}
				} else {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"Please supply a name for this pack with {"?bbtcg pack add [pack name]".Code()}",
						Channel = e.Channel,
						LogMessage = "BBTCGPackAddNoName"
					});
				}
			} else {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"This command can only be used by administrators of this guild",
					Channel = e.Channel,
					LogMessage = "BBTCGPackAddNotAdmin"
				});
			}
		}

		private async Task OnMessagePackDelete(MessageCreateEventArgs e) {
			string[] splitString = e.Message.Content.Split(' ');
			if (await BotMethods.IsUserAdmin(this, e.Guild, e.Channel, e.Author)) {
				if (splitString.Length >= 4) {
						if (int.TryParse(splitString[3], out int packId)) {
						var result = database.RemovePack(packId);
						if (result == Database.RemovePackResult.Success) {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"Successfully deleted pack {packId}",
								Channel = e.Channel,
								LogMessage = "BBTCGPackDeleteSuccess"
							});
						} else if (result == Database.RemovePackResult.PackDoesNotExist) {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"There is no pack with ID {packId}",
								Channel = e.Channel,
								LogMessage = "BBTCGPackDeleteBadID"
							});
						}
					} else {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"Could not parse {splitString[3].Code()} as a valid ID",
							Channel = e.Channel,
							LogMessage = "BBTCGPackDeleteInvalidIDParse"
						});
					}
				} else {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"Please supply a pack ID you wish to delete with {"?bbtcg pack delete [PackID]".Code()}",
						Channel = e.Channel,
						LogMessage = "BBTCGPackDeleteNoID"
					});
				}
			} else {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"This command can only be used by administrators of this guild",
					Channel = e.Channel,
					LogMessage = "BBTCGPackDeleteNotAdmin"
				});
			}
		}

		private async Task OnMessagePackCard(MessageCreateEventArgs e) {
			string[] splitString = e.Message.Content.Split(' ');
			if (await BotMethods.IsUserAdmin(this, e.Guild, e.Channel, e.Author)) {
				if (splitString.Length >= 6) {
					if (int.TryParse(splitString[3], out int packId)) {
						string cardId = splitString[4];
						if (int.TryParse(splitString[5], out int rarity)) {
							var result = database.AddPackCard(cardId, packId, rarity);
							if (result == Database.AddPackCardResult.Success) {
								await BotMethods.SendMessage(this, new SendMessageEventArgs {
									Message = $"Successfully added card to pack",
									Channel = e.Channel,
									LogMessage = "BBTCGPackCardSuccess"
								});
							} else if (result == Database.AddPackCardResult.CardAlreadyInPack) {
								await BotMethods.SendMessage(this, new SendMessageEventArgs {
									Message = $"Successfully updated card's rarity",
									Channel = e.Channel,
									LogMessage = "BBTCGPackCardSuccessCardInPack"
								});
							} else if (result == Database.AddPackCardResult.CardDoesNotExist) {
								await BotMethods.SendMessage(this, new SendMessageEventArgs {
									Message = $"Card {splitString[4]} does not exist",
									Channel = e.Channel,
									LogMessage = "BBTCGPackCardCardDoesNotExist"
								});
							} else if (result == Database.AddPackCardResult.PackDoesNotExist) {
								await BotMethods.SendMessage(this, new SendMessageEventArgs {
									Message = $"Pack {splitString[3]} does not exist",
									Channel = e.Channel,
									LogMessage = "BBTCGPackCardPackDoesNotExist"
								});
							}
						} else {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"Could not parse {splitString[5].Code()} as a valid integer",
								Channel = e.Channel,
								LogMessage = "BBTCGPackCardInvalidRarityParse"
							});
						}
					} else {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"Could not parse {splitString[3].Code()} as a valid ID",
							Channel = e.Channel,
							LogMessage = "BBTCGPackCardInvalidPackIDParse"
						});
					}
				} else {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"Please supply a pack Id, card ID and a rarity value with {"?bbtcg pack card [PackID] [CardID] [RarityValue]".Code()}",
						Channel = e.Channel,
						LogMessage = "BBTCGPackCardMissingArguments"
					});
				}
			} else {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"This command can only be used by administrators of this guild",
					Channel = e.Channel,
					LogMessage = "BBTCGPackCardNotAdmin"
				});
			}
		}

		private async Task OnMessagePackList(MessageCreateEventArgs e) {
			string[] splitString = e.Message.Content.Split(' ');
			if (splitString.Length >= 4) {
				if (int.TryParse(splitString[3], out int packId)) {
					if (packId <= 0 || packId > database.Packs.Count) {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"{splitString[3]} is an invalid pack ID",
							Channel = e.Channel,
							LogMessage = "BBTCGPackListInvalidPackIDOutOfRange"
						});
					}
					var pack = database.Packs[packId - 1];
					var sb = new StringBuilder();
					if (pack.Drops.Any()) {
						sb.AppendLine($"Cards in pack {pack.Name.Italics()}:");
						foreach (var drop in pack.Drops) {
							sb.AppendLine($"{$"[{drop.Card.ID}]".Code()} {drop.Card.Name} ({drop.Rarity.ToString().Italics()})");
						}
					} else {
						sb.AppendLine("No cards have been added to this pack yet");
					}

					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = sb.ToString(),
						Channel = e.Channel,
						LogMessage = "BBTCGPackListSpecific"
					});
				} else {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"Could not parse {splitString[3].Code()} as a valid ID",
						Channel = e.Channel,
						LogMessage = "BBTCGPackListInvalidPackIDParse"
					});
				}
			} else {
				var sb = new StringBuilder();
				if (database.Packs.Any()) {
					sb.AppendLine($"Packs available:");
					foreach (var pack in database.Packs.Select((pack, index) => new { pack, index })) {
						sb.AppendLine($"{$"[{(pack.index + 1).ToString().PadLeft(2)}]".Code()} {pack.pack.Name} ({$"{pack.pack.Drops.Count} cards".Italics()})");
					}
				} else {
					sb.AppendLine("No packs have been added yet");
				}

				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = sb.ToString(),
					Channel = e.Channel,
					LogMessage = "BBTCGPackList"
				});
			}
		}

		private async Task OnMessagePackOpen(MessageCreateEventArgs e) {
			string[] splitString = e.Message.Content.Split(' ');
			if (splitString.Length >= 4) {
				if (int.TryParse(splitString[3], out int packId)) {
					var result = database.OpenPack(e.Author, packId, out Card card);
					if (result == Database.OpenPackResult.Success) {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"Successfully opened {card.Name.Italics()}",
							Channel = e.Channel,
							LogMessage = "BBTCGPackOpenSuccess"
						});
						await BotMethods.SendFile(this, new SendFileEventArgs {
							FilePath = card.ImagePath,
							Channel = e.Channel,
							LogMessage = "BBTCGPackOpenSuccessImage"
						});
					} else if (result == Database.OpenPackResult.CooldownStillActive) {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"You still have {"blah".Code()} time remaining before you can open this pack again!",
							Channel = e.Channel,
							LogMessage = "BBTCGPackOpenCooldownActive"
						});
					} else if (result == Database.OpenPackResult.PackDoesNotExist) {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"Pack {splitString[3]} does not exist",
							Channel = e.Channel,
							LogMessage = "BBTCGPackOpenPackDoesNotExist"
						});
					}
				} else {
					await BotMethods.SendMessage(this, new SendMessageEventArgs {
						Message = $"Could not parse {splitString[3].Code()} as a valid ID",
						Channel = e.Channel,
						LogMessage = "BBTCGPackOpenInvalidPackIDParse"
					});
				}
			} else {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"Please supply a pack ID {"?bbtcg pack open [PackID]".Code()}",
					Channel = e.Channel,
					LogMessage = "BBTCGPackOpenMissingArguments"
				});
			}
		}

		private async Task InvalidCommand(DiscordChannel channel) {
			await BotMethods.SendMessage(this, new SendMessageEventArgs {
				Message = $"Invalid command, please check {"?help bbtcg".Code()} for usage details",
				Channel = channel,
				LogMessage = "BBTCGInvalidCommand"
			});
		}

		private bool IsFileValidImageType(string fileName) {
			foreach (string ending in new string[] { ".gif", ".jpg", ".jpeg", ".png" }) {
				if (fileName.EndsWith(ending)) {
					return true;
				}
			}
			return false;
		}
	}

}

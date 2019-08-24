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
							if (database.AddCard(string.Join(' ', splitString.Skip(3)), fileOutputPath, cardId)) {
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
							if (database.DeleteCard(selectedCard)) {
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
			foreach (var card in database.Cards) {
				sb.AppendLine($"{$"[{card.ID}]".Code()} {card.Name}");
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

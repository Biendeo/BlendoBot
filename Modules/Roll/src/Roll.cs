using BlendoBotLib;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roll {
	public class Roll : CommandBase {
		public Roll(ulong guildId, IBotMethods botMethods) : base(guildId, botMethods) { }

		public override string DefaultTerm => "?roll";
		public override string Name => "Roll";
		public override string Description => "Simulates dice rolls and coin flips";
		public override string Usage => $"Usage ({$"where {"x".Code()} and {"y".Code()} are positive integers".Italics()}):\n{$"{Term} [y]".Code()} ({$"rolls a {"y".Code()}-sided dice, giving a value between 1 and {"y".Code()}".Italics()})\n{$"{Term} d[y]".Code()} ({$"same as {$"{Term} y".Code()}".Italics()})\n{$"{Term} [x]d[y]".Code()} ({$"rolls a {"y".Code()}-sided dice {"x".Code()} number of times".Italics()})\n{$"{Term} coin".Code()} ({"returns either heads or tails".Italics()})";
		public override string Author => "Biendeo";
		public override string Version => "0.3.0";

		private Random random;

		public override async Task<bool> Startup() {
			random = new Random();
			await Task.Delay(0);
			return true;
		}

		public override async Task OnMessage(MessageCreateEventArgs e) {
			string[] splitMessage = e.Message.Content.Split(' ');
			// There must be exactly two terms.
			if (splitMessage.Length == 2) {
				if (splitMessage[1].ToLower() == "coin") {
					await FlipCoin(e);
				} else {
					string[] splitRoll = splitMessage[1].Split('d');
					if (splitRoll.Length == 1 || (splitRoll.Length == 2 && string.IsNullOrWhiteSpace(splitRoll[0]))) {
						bool success = int.TryParse(splitRoll[splitRoll.Length - 1], out int diceValue);
						if (success) {
							if (diceValue > 1000000) {
								await BotMethods.SendMessage(this, new SendMessageEventArgs {
									Message = $"You can't roll a {diceValue}-sided die! Please use a lower number (at most 1,000,000).",
									Channel = e.Channel,
									LogMessage = "RollErrorSingleTooHigh"
								});
							} else if (diceValue >= 2) {
								await RollDice(e, 1, diceValue);
							} else {
								await BotMethods.SendMessage(this, new SendMessageEventArgs {
									Message = $"You can't roll a {diceValue}-sided die! Please use a higher number (at least 2).",
									Channel = e.Channel,
									LogMessage = "RollErrorSingleTooLow"
								});
							}
						} else {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"{splitRoll[splitRoll.Length - 1]} is not a valid number!",
								Channel = e.Channel,
								LogMessage = "RollErrorSingleInvalidNumber"
							});
						}
					} else if (splitRoll.Length == 2) {
						bool success1 = int.TryParse(splitRoll[0], out int numDice);
						if (success1) {
							if (numDice > 50) {
								await BotMethods.SendMessage(this, new SendMessageEventArgs {
									Message = $"You can't roll {numDice} dice! Please use a lower number (at most 50).",
									Channel = e.Channel,
									LogMessage = "RollErrorMultipleNumTooHigh"
								});
							} else if (numDice >= 1) {
								bool success2 = int.TryParse(splitRoll[1], out int diceValue);
								if (success2) {
									if (diceValue > 1000000) {
										await BotMethods.SendMessage(this, new SendMessageEventArgs {
											Message = $"You can't roll a {diceValue}-sided die! Please use a lower number (at most 1,000,000).",
											Channel = e.Channel,
											LogMessage = "RollErrorMultipleValueTooHigh"
										});
									} else if (diceValue >= 2) {
										await RollDice(e, numDice, diceValue);
									} else {
										await BotMethods.SendMessage(this, new SendMessageEventArgs {
											Message = $"You can't roll a {diceValue}-sided die! Please use a higher number (at least 2).",
											Channel = e.Channel,
											LogMessage = "RollErrorMultipleValueTooLow"
										});
									}
								} else {
									await BotMethods.SendMessage(this, new SendMessageEventArgs {
										Message = $"{splitRoll[1]} is not a valid number!",
										Channel = e.Channel,
										LogMessage = "RollErrorMultipleValueInvalidNumber"
									});
								}
							} else {
								await BotMethods.SendMessage(this, new SendMessageEventArgs {
									Message = $"You can't roll {numDice} dice! Please use a higher number (at least 1).",
									Channel = e.Channel,
									LogMessage = "RollErrorMultipleNumTooLow"
								});
							}
						} else {
							await BotMethods.SendMessage(this, new SendMessageEventArgs {
								Message = $"{splitRoll[0]} is not a valid number!",
								Channel = e.Channel,
								LogMessage = "RollErrorMultipleNumInvalidNumber"
							});
						}
					} else {
						await BotMethods.SendMessage(this, new SendMessageEventArgs {
							Message = $"I couldn't determine what you wanted. Check {$"{BotMethods.GetHelpCommandTerm(this, GuildId)} roll".Code()} for ways to use this command.",
							Channel = e.Channel,
							LogMessage = "RollErrorTooManyDs"
						});
					}
				}
			} else {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"I couldn't determine what you wanted. Check {$"{BotMethods.GetHelpCommandTerm(this, GuildId)} roll".Code()} for ways to use this command.",
					Channel = e.Channel,
					LogMessage = "RollErrorInvalidArgumentCount"
				});
			}

			await Task.Delay(0);
		}

		private async Task RollDice(MessageCreateEventArgs e, int numRolls, int diceValue) {
			var results = Enumerable.Range(0, numRolls).Select(_ => random.Next(diceValue) + 1).ToList();
			if (results.Count == 1) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = IntToRegionalIndicator(results.Single()),
					Channel = e.Channel,
					LogMessage = "RollSuccessSingle"
				});
			} else {
				var sb = new StringBuilder();
				sb.AppendLine($"The results of the {numRolls} dice-rolls are:");
				sb.AppendLine("```");
				for (int i = 0; i < numRolls; ++i) {
					sb.Append(results[i].ToString().PadLeft(8, ' '));
					if (i % 5 == 4) {
						sb.AppendLine();
					}
				}
				sb.AppendLine("\n```");
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = sb.ToString(),
					Channel = e.Channel,
					LogMessage = "RollSuccessMultiple"
				});
			}
		}

		private string IntToRegionalIndicator(int x) {
			if (x >= 10) {
				return IntToRegionalIndicator(x / 10) + IntToRegionalIndicator(x % 10);
			} else {
				return x switch
				{
					0 => ":zero:",
					1 => ":one:",
					2 => ":two:",
					3 => ":three:",
					4 => ":four:",
					5 => ":five:",
					6 => ":six:",
					7 => ":seven:",
					8 => ":eight:",
					9 => ":nine:",
					_ => "?",
				};
			}
		}

		private async Task FlipCoin(MessageCreateEventArgs e) {
			int result = random.Next(2);
			if (result == 0) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = ":regional_indicator_h::regional_indicator_e::regional_indicator_a::regional_indicator_d::regional_indicator_s:",
					Channel = e.Channel,
					LogMessage = "RollSuccessCoinHeads"
				});
			} else {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = ":regional_indicator_t::regional_indicator_a::regional_indicator_i::regional_indicator_l::regional_indicator_s:",
					Channel = e.Channel,
					LogMessage = "RollSuccessCoinTails"
				});
			}
		}
	}
}

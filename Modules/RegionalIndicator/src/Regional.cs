using BlendoBotLib;
using BlendoBotLib.Commands;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlendoBot.Commands {
	public class Regional : ICommand {
		CommandProps ICommand.Properties => properties;

		public static readonly CommandProps properties = new CommandProps {
			Term = "?regional",
			Name = "Regional Indicator",
			Description = "Converts a message into lovely regional indicator text.",
			Usage = $"Usage: {"?regional [message]".Code()}",
			Author = "Biendeo",
			Version = "0.1.0",
			Startup = async () => { await Task.Delay(0); return true; },
			OnMessage = RegionalCommand
		};

		private static readonly Dictionary<char, string> characterMappings = new Dictionary<char, string> {
			{ 'a', ":regional_indicator_a:" },
			{ 'b', ":regional_indicator_b:" },
			{ 'c', ":regional_indicator_c:" },
			{ 'd', ":regional_indicator_d:" },
			{ 'e', ":regional_indicator_e:" },
			{ 'f', ":regional_indicator_f:" },
			{ 'g', ":regional_indicator_g:" },
			{ 'h', ":regional_indicator_h:" },
			{ 'i', ":regional_indicator_i:" },
			{ 'j', ":regional_indicator_j:" },
			{ 'k', ":regional_indicator_k:" },
			{ 'l', ":regional_indicator_l:" },
			{ 'm', ":regional_indicator_m:" },
			{ 'n', ":regional_indicator_n:" },
			{ 'o', ":regional_indicator_o:" },
			{ 'p', ":regional_indicator_p:" },
			{ 'q', ":regional_indicator_q:" },
			{ 'r', ":regional_indicator_r:" },
			{ 's', ":regional_indicator_s:" },
			{ 't', ":regional_indicator_t:" },
			{ 'u', ":regional_indicator_u:" },
			{ 'v', ":regional_indicator_v:" },
			{ 'w', ":regional_indicator_w:" },
			{ 'x', ":regional_indicator_x:" },
			{ 'y', ":regional_indicator_y:" },
			{ 'z', ":regional_indicator_z:" },
			{ '1', ":one:" },
			{ '2', ":two:" },
			{ '3', ":three:" },
			{ '4', ":four:" },
			{ '5', ":five:" },
			{ '6', ":six:" },
			{ '7', ":seven:" },
			{ '8', ":eight:" },
			{ '9', ":nine:" },
			{ '0', ":zero:" },
			{ ' ', ":black_large_square:" },
			{ '!', ":grey_exclamation:" },
			{ '?', ":grey_question:" },
			{ '#', ":hash:" },
			{ '$', ":heavy_dollar_sign:" },
			{ '+', ":heavy_plus_sign:" },
			{ '-', ":heavy_minus_sign:" }
		};

		public static async Task RegionalCommand(MessageCreateEventArgs e) {
			// First covert the original message to lower-case, and remove the original command.
			if (e.Message.Content.Length <= 10) {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = $"You must add something after the `?regional`!",
					Channel = e.Channel,
					LogMessage = "RegionalErrorNoMessage"
				});
				return;
			}
			string message = e.Message.Content.ToLower().Substring(10);
			var newString = new StringBuilder();
			foreach (char c in message) {
				if (characterMappings.ContainsKey(c)) {
					newString.Append(characterMappings[c]);
					newString.Append(' '); // Stops platforms from actually rendering flags.
				} else {
					newString.Append(c);
				}
			}
			if (newString.Length <= 2000) {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = newString.ToString(),
					Channel = e.Channel,
					LogMessage = "RegionalSuccess"
				});
			} else {
				await Methods.SendMessage(null, new SendMessageEventArgs {
					Message = $"Regionalified message exceeds maximum character count by {newString.Length - 2000}. Shorten your message!",
					Channel = e.Channel,
					LogMessage = "RegionalErrorTooLong"
				});
			}
			await Task.Delay(0);
		}
	}
}

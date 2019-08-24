using BlendoBotLib;
using DSharpPlus.EventArgs;
using System.Linq;
using System.Threading.Tasks;

namespace DecimalSpiral {
	public class DecimalSpiral : CommandBase {
		public DecimalSpiral(ulong guildId, IBotMethods botMethods) : base(guildId, botMethods) { }

		public override string Term => "?ds";
		public override string Name => "Decimal Spiral";
		public override string Description => "Makes a pretty spiral.";
		public override string Usage => $"Usage: {"?ds [size]".Code()} {"".Italics()}\nThe size must be an odd number between 5 and 43!";
		public override string Author => "Biendeo";
		public override string Version => "0.1.0";

		private enum Direction {
			Up,
			Right,
			Down,
			Left
		};

		private string CreateSpiral(int size) {
			char[] spiral = Enumerable.Repeat(' ', size * (size + 1)).ToArray();
			for (int row = 0; row < size; ++row) {
				spiral[(size + 1) * row + size] = '\n';
			}

			int x = (size / 4) * 2;
			int y = (((size / 2) + 1) / 2) * 2;
			char num = '0';
			Direction direction = (size / 2) % 2 == 0 ? Direction.Left : Direction.Right;
			int stride = 2;
			int currentStride = 0;

			while (x != -1 || y != 0) {
				spiral[(size + 1) * y + x] = num;
				++num;
				if (num > '9') {
					num = '0';
				}
				++currentStride;
				switch (direction) {
					case Direction.Right:
						++x;
						if (currentStride >= stride) {
							currentStride = 0;
							direction = Direction.Up;
						}
						break;
					case Direction.Up:
						--y;
						if (currentStride >= stride) {
							currentStride = 0;
							direction = Direction.Left;
							stride += 2;
						}
						break;
					case Direction.Left:
						--x;
						if (currentStride >= stride) {
							currentStride = 0;
							direction = Direction.Down;
						}
						break;
					case Direction.Down:
						++y;
						if (currentStride >= stride) {
							currentStride = 0;
							direction = Direction.Right;
							stride += 2;
						}
						break;
				}
			}

			return new string(spiral);
		}

		public override async Task<bool> Startup() {
			await Task.Delay(0);
			return true;
		}

		public override async Task OnMessage(MessageCreateEventArgs e) {
			string[] splitInput = e.Message.Content.Split(' ');

			if (splitInput.Length != 2) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"You must specify two arguments to {"?ds".Code()}",
					Channel = e.Channel,
					LogMessage = "DecimalSpiralErrorIncorrectNumArgs"
				});
				return;
			}

			if (!int.TryParse(splitInput[1], out int size)) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"The argument is not a number!",
					Channel = e.Channel,
					LogMessage = "DecimalSpiralErrorNonNumericValue"
				});
				return;
			}

			if (size < 5 || size > 43 || size % 2 == 0) {
				await BotMethods.SendMessage(this, new SendMessageEventArgs {
					Message = $"The argument must be between 5 and 43 inclusive and be odd!",
					Channel = e.Channel,
					LogMessage = "DecimalSpiralErrorIncorrectValue"
				});
				return;
			}

			await BotMethods.SendMessage(this, new SendMessageEventArgs {
				Message = $"\n{CreateSpiral(size)}".CodeBlock(),
				Channel = e.Channel,
				LogMessage = "DecimalSpiralSuccess"
			});

			await Task.Delay(0);
		}
	}
}

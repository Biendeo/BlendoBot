using BlendoBotLib.Interfaces;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace WheelOfFortune {
	public class WheelOfFortuneMessageListener : IMessageListener {
		private readonly WheelOfFortune wheelOfFortune;

		public WheelOfFortuneMessageListener(WheelOfFortune wheelOfFortune) {
			this.wheelOfFortune = wheelOfFortune;
		}

		public async Task OnMessage(MessageCreateEventArgs e) => await wheelOfFortune.HandleMessageListener(e);
	}
}

using BlendoBotLib;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WheelOfFortune {
	public class WheelOfFortuneMessageListener : IMessageListener {
		private WheelOfFortune wheelOfFortune;

		public WheelOfFortuneMessageListener(WheelOfFortune wheelOfFortune) {
			this.wheelOfFortune = wheelOfFortune;
		}

		public CommandBase Command { get { return wheelOfFortune; } }

		public async Task OnMessage(MessageCreateEventArgs e) => await wheelOfFortune.HandleMessageListener(e);
	}
}

using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace BlendoBotLib {
	/// <summary>
	/// The public interface for a message listener. All message listeners must implement this.
	/// </summary>
	public interface IMessageListener {
		Task OnMessage(MessageCreateEventArgs e);
	}
}

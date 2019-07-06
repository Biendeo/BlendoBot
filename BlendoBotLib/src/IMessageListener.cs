using BlendoBotLib.MessageListeners;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlendoBotLib {
	/// <summary>
	/// The public interface for a message listener. All message listeners must implement this.
	/// </summary>
	public interface IMessageListener {
		/// <summary>
		/// The properties of a message listener. This is used by several internal components to know what to call, what
		/// the listener is called, etc.
		/// </summary>
		MessageListenerProps Properties { get; }
	}
}

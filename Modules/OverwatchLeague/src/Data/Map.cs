using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace OverwatchLeague.Data {
	public class Map {
		public ulong Guid { get; private set; }
		public string Name { get; private set; }
		private List<GameMode> gameModes;
		public ReadOnlyCollection<GameMode> GameModes { get { return gameModes.AsReadOnly(); } }

		public Map(ulong guid, string name) {
			Guid = guid;
			Name = name;
			gameModes = new List<GameMode>();
		}

		public void AddGameMode(GameMode mode) {
			gameModes.Add(mode);
		}
	}
}

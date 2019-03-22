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
		private List<MatchGame> games;
		public ReadOnlyCollection<MatchGame> Games { get { return games.AsReadOnly(); } }

		public Map(ulong guid, string name) {
			Guid = guid;
			Name = name;
			gameModes = new List<GameMode>();
			games = new List<MatchGame>();
		}

		public void AddGameMode(GameMode mode) {
			gameModes.Add(mode);
		}

		public void AddGame(MatchGame game) {
			games.Add(game);
		}
	}
}

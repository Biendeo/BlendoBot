using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace OverwatchLeague.Data {
	public class GameMode {
		public ulong Id { get; private set; }
		public string Name { get; private set; }
		private readonly List<Map> maps;
		public ReadOnlyCollection<Map> Maps { get { return maps.AsReadOnly(); } }

		public GameMode(ulong id, string name) {
			Id = id;
			Name = name;
			maps = new List<Map>();
		}

		public void AddMap(Map map) {
			maps.Add(map);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace OverwatchLeague.Data {
	public class Division {
		public int Id { get; private set; }
		public string Name { get; private set; }
		public string AbbreviatedName { get; private set; }
		private List<Team> teams;
		public ReadOnlyCollection<Team> Teams { get { return teams.AsReadOnly(); } }

		public Division(int id, string name, string abbreviatedName) {
			Id = id;
			Name = name;
			AbbreviatedName = abbreviatedName;
			teams = new List<Team>();
		}

		public void AddTeam(Team t) {
			teams.Add(t);
		}
	}
}

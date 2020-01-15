using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text;

namespace OverwatchLeague.Data {
	public class Team {
		public int Id { get; private set; }
		public string Name { get; private set; }
		public string AbbreviatedName { get; private set; }
		
		public Division Division { get; private set; }
		public Color PrimaryColor { get; private set; }
		public Color SecondaryColor { get; private set; }
		private readonly List<Match> matches;
		public ReadOnlyCollection<Match> Matches { get { return matches.AsReadOnly(); } }

		public Team(int id, string name, string abbreviatedName, Color primaryColor, Color secondaryColor) {
			Id = id;
			Name = name;
			AbbreviatedName = abbreviatedName;
			Division = null;
			PrimaryColor = primaryColor;
			SecondaryColor = secondaryColor;
			matches = new List<Match>();
		}

		public void SetDivision(Division d) {
			Division = d;
		}

		public void AddMatch(Match m) {
			matches.Add(m);
			matches.Sort((a, b) => {
				return a.StartTime.CompareTo(b.StartTime);
			});
		}
	}
}

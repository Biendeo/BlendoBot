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
		public Color PrimaryColor { get; private set; }
		public Color SecondaryColor { get; private set; }
		public Color TertiaryColor { get; private set; }
		private readonly List<Match> matches;
		public ReadOnlyCollection<Match> Matches => matches.AsReadOnly();

		public Team(int id, string name, string abbreviatedName, Color primaryColor, Color secondaryColor, Color tertiaryColor) {
			Id = id;
			Name = name;
			AbbreviatedName = abbreviatedName;
			PrimaryColor = primaryColor;
			SecondaryColor = secondaryColor;
			TertiaryColor = tertiaryColor;
			matches = new List<Match>();
		}


		public void AddMatch(Match m) {
			matches.Add(m);
			matches.Sort((a, b) => {
				return a.StartTime.CompareTo(b.StartTime);
			});
		}
	}
}

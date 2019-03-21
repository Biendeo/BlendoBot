using System;
using System.Collections.Generic;
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

		public Team(int id, string name, string abbreviatedName, Color primaryColor, Color secondaryColor) {
			Id = id;
			Name = name;
			AbbreviatedName = abbreviatedName;
			Division = null;
			PrimaryColor = primaryColor;
			SecondaryColor = secondaryColor;
		}

		public void SetDivision(Division d) {
			Division = d;
		}
	}
}

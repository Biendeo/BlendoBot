using System;
using System.Collections.Generic;
using System.Text;

namespace OverwatchLeague.Data {
	public struct Standing {
		public int Position;
		public Team Team;
		public int MatchWins;
		public int MatchLosses;
		public int MapWins;
		public int MapDraws;
		public int MapLosses;
		public int MapDiff { get { return MapWins - MapLosses; } }
	}
}

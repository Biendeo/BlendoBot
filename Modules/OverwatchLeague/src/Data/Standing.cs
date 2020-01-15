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

		public override bool Equals(object obj) {
			if (ReferenceEquals(this, obj)) {
				return true;
			}

			if (obj is null) {
				return false;
			}

			if (!(obj is Standing)) {
				return false;
			} else {
				return Position == ((Standing)obj).Position;
			}
		}

		public override int GetHashCode() {
			return Position.GetHashCode();
		}

		public static bool operator ==(Standing left, Standing right) {
			return left.Equals(right);
		}

		public static bool operator !=(Standing left, Standing right) {
			return !(left == right);
		}
	}
}

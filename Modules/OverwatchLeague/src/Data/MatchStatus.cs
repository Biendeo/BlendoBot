using System;
using System.Collections.Generic;
using System.Text;

namespace OverwatchLeague.Data {
	public enum MatchStatus {
		Unknown,
		Pending,
		InProgress,
		Concluded
	}

	public static class MatchStatusExtensions {
		public static MatchStatus FromString(string s) {
			switch (s) {
				case "PENDING":
					return MatchStatus.Pending;
				case "IN_PROGRESS":
					return MatchStatus.InProgress;
				case "CONCLUDED":
					return MatchStatus.Concluded;
				default:
					return MatchStatus.Unknown;
			}
		}

		public static string ToString(this MatchStatus ms) {
			switch (ms) {
				case MatchStatus.Unknown:
				default:
					return "???";
				case MatchStatus.Pending:
					return "PENDING";
				case MatchStatus.InProgress:
					return "IN_PROGRESS";
				case MatchStatus.Concluded:
					return "CONCLUDED";
			}
		}
	}
}

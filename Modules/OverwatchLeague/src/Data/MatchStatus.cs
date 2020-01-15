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
		public static MatchStatus FromString(string s) => s switch {
			"PENDING" => MatchStatus.Pending,
			"IN_PROGRESS" => MatchStatus.InProgress,
			"CONCLUDED" => MatchStatus.Concluded,
			_ => MatchStatus.Unknown,
		};

		public static string ToString(this MatchStatus ms) => ms switch {
			MatchStatus.Pending => "PENDING",
			MatchStatus.InProgress => "IN_PROGRESS",
			MatchStatus.Concluded => "CONCLUDED",
			_ => "???",
		};
	}
}

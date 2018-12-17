using System;
using System.Collections.Generic;
using System.Text;

namespace Weather {
	struct Temperature {
		public decimal Kelvin {
			get {
				return Celsius + 273.15m;
			}
			set {
				Celsius = value - 273.15m;
			}
		}

		public decimal Celsius { get; set; }

		public decimal Farenheit {
			get {
				return Celsius * 9.0m / 5.0m + 32.0m;
			}
			set {
				Celsius = (value - 32.0m) * 5.0m / 9.0m;
			}
		}

		public static implicit operator decimal(Temperature d) {
			return d.Celsius;
		}

		public static implicit operator Temperature(decimal d) {
			return FromCelsius(d);
		}

		public static Temperature FromCelsius(decimal d) {
			return new Temperature { Celsius = d };
		}

		public static Temperature FromFarenheit(decimal d) {
			return new Temperature { Farenheit = d };
		}

		public static Temperature FromKelvin(decimal d) {
			return new Temperature { Kelvin = d };
		}

	}
}

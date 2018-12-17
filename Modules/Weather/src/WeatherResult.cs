using System;
using System.Collections.Generic;
using System.Text;

namespace Weather {
	class WeatherResult {
		public int ResultCode { get; set; }
		public string ResultMessage { get; set; }
		public string Location { get; set; }
		public string Country { get; set; }
		public string Condition { get; set; }
		public decimal Latitude { get; set; }
		public decimal Longitude { get; set; }
		public Temperature TemperatureCurrent { get; set; }
		public decimal PressureHPA { get; set; }
		public Temperature TemperatureMin { get; set; }
		public Temperature TemperatureMax { get; set; }
		public decimal WindSpeed { get; set; }
		public decimal WindDirection { get; set; }
		public DateTime Sunrise { get; set; }
		public DateTime Sunset { get; set; }
		public string TimeZone { get; set; }
	}
}

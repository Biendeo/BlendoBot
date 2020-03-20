using DSharpPlus.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Salaros.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BlendoBot {

	public class Config {
		private Config() {
			Values = new Dictionary<string, Dictionary<string, string>>();
			ConfigPath = "///NO PATH SET!///";
		}

		public Dictionary<string, Dictionary<string, string>> Values;
		public string ConfigPath { get; private set; }

		public string ReadString(object _, string configHeader, string configKey) {
			if (Values.ContainsKey(configHeader) && Values[configHeader].ContainsKey(configKey)) {
				return Values[configHeader][configKey];
			} else {
				return null;
			}
		}

		public bool DoesKeyExist(object _, string configHeader, string configKey) {
			return Values.ContainsKey(configHeader) && Values[configHeader].ContainsKey(configKey);
		}

		public void WriteString(object _, string configHeader, string configKey, string configValue) {
			if (!Values.ContainsKey(configHeader)) {
				Values.Add(configHeader, new Dictionary<string, string>());
			}
			if (!Values[configHeader].ContainsKey(configKey)) {
				Values[configHeader].Add(configKey, configValue);
			} else {
				Values[configHeader][configKey] = configValue;
			}
			// For efficiency, this should be a separate call on a background task. For now, it simplifies the
			// implementation a bit.
			SaveToFile();
		}

		private void SaveToFile() {
			var parser = new ConfigParser();
			foreach (var section in Values) {
				foreach (var key in section.Value) {
					parser.SetValue(section.Key, key.Key, key.Value);
				}
			}
			parser.Save(ConfigPath);
		}

		public string Name { get { return ReadString(this, "BlendoBot", "Name"); } }
		public string Version { get { return ReadString(this, "BlendoBot", "Version"); } }
		public string Description { get { return ReadString(this, "BlendoBot", "Description"); } }
		public string Author { get { return ReadString(this, "BlendoBot", "Author"); } }
		public string ActivityName {
			get {
				try {
					return ReadString(this, "BlendoBot", "ActivityName");
				} catch (KeyNotFoundException) {
					//TODO: Double check whether this is necessary.
					return null;
				}
			}
		}
		public ActivityType? ActivityType {
			get {
				try {
					return (ActivityType)Enum.Parse(typeof(ActivityType), ReadString(this, "BlendoBot", "ActivityType"));
				} catch (ArgumentException) {
					return null;
				} catch (KeyNotFoundException) {
					//TODO: Double check whether this is necessary.
					return null;
				}
			}
		}

		/// <summary>
		/// Creates a JSON object from a file path. Returns null if the file doesn't exist or
		/// if the object doesn't contain every element.
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public static bool FromFile(string filePath, out Config config) {
			config = new Config {
				ConfigPath = filePath
			};
			if (!File.Exists(filePath)) {
				return false;
			}
			var parser = new ConfigParser(filePath);
			foreach (var section in parser.Sections) {
				if (!config.Values.ContainsKey(section.SectionName)) {
					config.Values.Add(section.SectionName, new Dictionary<string, string>());
				}
				foreach (var pair in section.Keys) {
					if (!config.Values[section.SectionName].ContainsKey(pair.Name)) {
						config.Values[section.SectionName].Add(pair.Name, pair.Content);
					} else {
						config.Values[section.SectionName][pair.Name] = pair.Content;
					}
				}
			}
			return true;
		}
	}
}
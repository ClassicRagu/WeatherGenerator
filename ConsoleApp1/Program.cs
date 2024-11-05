using ConsoleApp1;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using System.Collections;

class Program
{
	// This generates the weather data files bozja.net uses
	static void Main(string[] args)
	{
		// This should probably be made into an arg at some point
		var lumina = new Lumina.GameData("C:\\Program Files (x86)\\SquareEnix\\FINAL FANTASY XIV - A Realm Reborn\\game\\sqpack", new() { DefaultExcelLanguage = Lumina.Data.Language.English });

		var weatherRateObj = new List<CompiledWeather[]>();

		foreach (var column in lumina.GetExcelSheet<WeatherRate>())
		{
			CompiledWeather[] compiledWeathers = new CompiledWeather[8];
			for (int i = 0; i < 8; i++)
			{
				compiledWeathers[i] = new CompiledWeather() { Rate = column.Rate[i], Weather = lumina.GetExcelSheet<Weather>().GetRow(column.Weather[i].RowId).Name.ExtractText() };
			}
			weatherRateObj.Add(compiledWeathers);
		}

		var zonesObj = new Dictionary<string, int>();
		var zonesMapper = new List<string>();
		zonesMapper.Add("import { ZoneObject } from \"../types/ZoneObject\"");
		var allowedZones = "export type ValidZones = ";
		var allowedWeathers = "export type AllowedWeathers = \"\" | ";

		foreach (var column in lumina.GetExcelSheet<Map>())
		{
			try
			{
				var placeName = lumina.GetExcelSheet<PlaceName>().GetRow(column.PlaceName.RowId).Name.ExtractText();
				var rate = column.TerritoryType.Value.WeatherRate;
				// A bit of a nasty workaround, we only take the first rate we come across.
				// This may cause inaccuracies that need to be fixed in the future.
				var value = 0;
				if (placeName != "" && !zonesObj.TryGetValue(placeName, out value))
				{
					allowedZones = allowedZones + $"\"{placeName}\" | ";
					var processedName = placeName.ToUpperInvariant()
						// Remove common characters
						.Replace(" ", "_")
						.Replace("-", "")
						.Replace("'", "")
						// (Hard) dungeons
						.Replace("(", "")
						.Replace(")", "")
						// Literally just Frontlines.
						.Replace(":", "")
						// Omega Raids
						.Replace(".0","")
						// Unknown maps
						.Replace("???", "UNKOWN")
						// Mt. Gulg
						.Replace(".", "");
					processedName = $"export const ZONE_{processedName}: ZoneObject = {{Name: \"{placeName}\", Rate: {rate}}}";
					if (!zonesMapper.Contains(processedName))
					{
						zonesMapper.Add(processedName);
					}
					zonesObj.Add(placeName, column.TerritoryType.Value.WeatherRate);
				}
			}
			catch
			{
				// Tee hee, territory data is probably bad, just skip it and move on
			}
		}

		foreach (var column in lumina.GetExcelSheet<Weather>())
		{
			var weatherName = column.Name.ExtractText();
			if(!allowedWeathers.Contains(weatherName))
			{
				allowedWeathers = allowedWeathers + $"\"{weatherName}\" | ";
			}
		}

		// Remove final |
		allowedZones = allowedZones.Remove(allowedZones.Length - 2);
		allowedWeathers = allowedWeathers.Remove(allowedWeathers.Length - 2);

		string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), $"json/");
		if (!Directory.Exists(directoryPath))
		{
			Directory.CreateDirectory(directoryPath);
		}

		string weatherRateFilePath = Path.Combine(directoryPath, "WeatherRates.json");
		string zoneFilePath = Path.Combine(directoryPath, "Zones.json");
		string zoneMapperFilePath = Path.Combine(directoryPath, "ZoneMapper.ts");
		string allowedZonesFilePath = Path.Combine(directoryPath, "AllowedZones.ts");
		string allowedWeathersFilePath = Path.Combine(directoryPath, "AllowedWeathers.ts");

		File.WriteAllText(weatherRateFilePath, JsonConvert.SerializeObject(weatherRateObj, Formatting.Indented));
		File.WriteAllText(zoneFilePath, JsonConvert.SerializeObject(zonesObj, Formatting.Indented));
		File.WriteAllLines(zoneMapperFilePath, zonesMapper);
		File.WriteAllText(allowedZonesFilePath, allowedZones);
		File.WriteAllText(allowedWeathersFilePath, allowedWeathers);
	}
}

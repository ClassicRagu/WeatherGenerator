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

		var weatherRateObj = new ArrayList();

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

		foreach (var column in lumina.GetExcelSheet<Map>())
		{
			try
			{
				zonesObj.Add(lumina.GetExcelSheet<PlaceName>().GetRow(column.PlaceName.RowId).Name.ExtractText(), column.TerritoryType.Value.WeatherRate);
			}
			catch
			{
				// Tee hee, territory data is probably bad, just skip it and move on
			}
		}

		string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), $"json/");
		if (!Directory.Exists(directoryPath))
		{
			Directory.CreateDirectory(directoryPath);
		}

		string weatherRateFilePath = Path.Combine(directoryPath, "WeatherRates.json");
		string zoneFilePath = Path.Combine(directoryPath, "Zones.json");

		File.WriteAllText(weatherRateFilePath, JsonConvert.SerializeObject(weatherRateObj, Formatting.Indented));
		File.WriteAllText(zoneFilePath, JsonConvert.SerializeObject(zonesObj, Formatting.Indented));
	}
}

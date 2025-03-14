using Newtonsoft.Json;
using Spectre.Console;
using NodaTime;
using NodaTime.TimeZones;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;


string SettingsFile = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "WTApp",
    "settings.json"
);
// В начало метода Main
var settingsDir = Path.GetDirectoryName(SettingsFile);
if (!Directory.Exists(settingsDir))
{
    Directory.CreateDirectory(settingsDir!);
}

UserSettings Settings = LoadSettings();

if (args.Length == 0)
{
    ShowTime();
}
else if (args[0] == "add" && args.Length > 1)
{
    AddCity(args[1]);
}
else if (args[0] == "remove" && args.Length > 1)
{
    RemoveCity(args[1]);
}
else if (args[0] == "language" && args.Length > 1)
{
    SetLanguage(args[1]);
}
else
{
    Console.WriteLine("Invalid command.");
}

void ShowTime()
{
    var table = new Table().Border(TableBorder.Rounded);
    table.AddColumn(GetLocalizedText("City"));
    table.AddColumn(GetLocalizedText("Local Time"));
    table.AddColumn(GetLocalizedText("Time Zone"));

    table.AddRow(GetLocalizedText("Local"), DateTime.Now.ToString("hh:mm tt"), "-");
    table.AddRow("UTC", DateTime.UtcNow.ToString("hh:mm tt"), "-");

    foreach (var city in Settings.Cities)
    {
        try
        {
            string tz = GetTimeZoneByCity(city);
            if (tz != null)
            {
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
                var localTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZoneInfo);
                table.AddRow(city, localTime.ToString("hh:mm tt"), tz);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing {city}: {ex.Message}");
        }
    }

    AnsiConsole.Write(table);
}

void AddCity(string city)
{
    if (!Settings.Cities.Contains(city))
    {
        Settings.Cities.Add(city);
        SaveSettings();
        Console.WriteLine($"{GetLocalizedText("Added city")}: {city}.");
    }
    else
    {
        Console.WriteLine($"{city} {GetLocalizedText("is already added")}.");
    }
}

void RemoveCity(string city)
{
    if (Settings.Cities.Contains(city))
    {
        Settings.Cities.Remove(city);
        SaveSettings();
        Console.WriteLine($"{GetLocalizedText("Removed city")}: {city}.");
    }
    else
    {
        Console.WriteLine($"{city} {GetLocalizedText("not found")}.");
    }
}

void SetLanguage(string lang)
{
    Settings.Language = lang;
    SaveSettings();
    Console.WriteLine($"{GetLocalizedText("Language set to")}: {lang}.");
}

string GetTimeZoneByCity(string city)
{
    var tzdb = DateTimeZoneProviders.Tzdb;
    var location = TzdbDateTimeZoneSource.Default.ZoneLocations?
        .FirstOrDefault(z => z.ZoneId.Contains(city, StringComparison.OrdinalIgnoreCase));

    return location?.ZoneId!;
}

UserSettings LoadSettings()
{
    if (File.Exists(SettingsFile))
    {
        return JsonConvert.DeserializeObject<UserSettings>(File.ReadAllText(SettingsFile)) ?? new UserSettings();
    }
    return new UserSettings();
}

void SaveSettings()
{
    File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(Settings, Formatting.Indented));
}

string GetLocalizedText(string key)
{
    var translations = new Dictionary<string, Dictionary<string, string>>
        {
            { "en", new Dictionary<string, string> {
                { "City", "City" }, { "Local Time", "Local Time" }, { "Time Zone", "Time Zone" },
                { "Added city", "Added city" }, { "is already added", "is already added" },
                { "Removed city", "Removed city" }, { "not found", "not found" },
                { "Language set to", "Language set to" }
            }},
            { "ru", new Dictionary<string, string> {
                { "City", "Город" }, { "Local Time", "Местное время" }, { "Time Zone", "Часовой пояс" },
                { "Added city", "Город добавлен" }, { "is already added", "уже добавлен" },
                { "Removed city", "Город удален" }, { "not found", "не найден" },
                { "Language set to", "Язык установлен на" }
            }},
            { "uz", new Dictionary<string, string> {
                { "City", "Shahar" }, { "Local Time", "Mahalliy vaqt" }, { "Time Zone", "Vaqt zonasi" },
                { "Added city", "Shahar qoʻshildi" }, { "is already added", "allaqachon qoʻshilgan" },
                { "Removed city", "Shahar olib tashlandi" }, { "not found", "topilmadi" },
                { "Language set to", "Til oʻrnatildi" }
            }},
        };

    if (translations.ContainsKey(Settings.Language) && translations[Settings.Language].ContainsKey(key))
    {
        return translations[Settings.Language][key];
    }
    return key; // Default fallback
}

class UserSettings
{
    public List<string> Cities { get; set; } = new List<string>();
    public string Language { get; set; } = "en";
}

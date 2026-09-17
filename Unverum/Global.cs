using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Unverum;

public static class Global
{
    public static Config config = null!;
    public static Logger logger = null!;
    public static char s = Path.DirectorySeparatorChar;
    public static string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    public static List<string> games = null!;
    public static ObservableCollection<string> LoadoutItems = null!;
    public static ObservableCollection<Mod> ModList = null!;
    public static string CurrentGame => config.CurrentGame
        ?? throw new InvalidOperationException("No game is selected.");
    public static GameConfig CurrentGameConfig => config.Configs?.GetValueOrDefault(CurrentGame)
        ?? throw new InvalidOperationException($"No configuration exists for {CurrentGame}.");
    public static void UpdateConfig()
    {
        config.Configs![config.CurrentGame!].Loadouts![config.Configs![config.CurrentGame!].CurrentLoadout!] = ModList;
        string configString = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        try
        {
            File.WriteAllText($@"{assemblyLocation}{s}Config.json", configString);
        }
        catch (Exception e)
        {
            logger.WriteLine($"Couldn't write Config.json ({e.Message})", LoggerType.Error);
        }
    }
}

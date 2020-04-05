using System.Reflection;
using System.IO;
using Harmony;
using Newtonsoft.Json;
using System;

namespace EvenTheOdds
{
    public class EvenTheOdds
    {
        internal static string LogPath;
        internal static string ModDirectory;
        internal static Settings Settings;
        // BEN: DebugLevel (0: nothing, 1: error, 2: debug, 3: info)
        internal static int DebugLevel = 1;

        public static void Init(string directory, string settings)
        {
            ModDirectory = directory;
            LogPath = Path.Combine(ModDirectory, "EvenTheOdds.log");

            Logger.Initialize(LogPath, DebugLevel, ModDirectory, nameof(EvenTheOdds));

            try
            {
                Settings = JsonConvert.DeserializeObject<Settings>(settings);
            }
            catch (Exception e)
            {
                Settings = new Settings();
                Logger.Error(e);
            }

            Logger.Initialize(LogPath, DebugLevel, ModDirectory, nameof(EvenTheOdds));

            // Harmony calls need to go last here because their Prepare() methods directly check Settings...
            HarmonyInstance harmony = HarmonyInstance.Create("de.mad.EvenTheOdds");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}

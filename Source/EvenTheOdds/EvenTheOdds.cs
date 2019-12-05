using System.Reflection;
using System.IO;
using Harmony;

namespace EvenTheOdds
{
    public class EvenTheOdds
    {
        public static string LogPath;
        public static string ModDirectory;

        internal static bool EnableDynamicContractDifficultyVariance = true;

        // BEN: Debug (0: nothing, 1: errors, 2:all)
        internal static int DebugLevel = 2;

        public static void Init(string directory, string settingsJSON)
        {
            ModDirectory = directory;

            LogPath = Path.Combine(ModDirectory, "EvenTheOdds.log");
            File.CreateText(EvenTheOdds.LogPath);

            var harmony = HarmonyInstance.Create("de.mad.EvenTheOdds");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}

using UnityEditor;

namespace UniOpenCLI.Editor
{
    internal static class UniOpenCliToolsMenu
    {
        [MenuItem("Tools/UniOpen CLI/Settings")]
        private static void OpenSettings()
        {
            SettingsService.OpenUserPreferences(UniOpenCliSettingsProvider.SettingsPath);
        }

        [MenuItem("Tools/UniOpen CLI/Open Terminal at Project Root")]
        private static void OpenTerminalAtProjectRoot()
        {
            UniOpenCli.Open("");
        }

        // Recovery hatch for a deleted or corrupted Generated folder.
        [MenuItem("Tools/UniOpen CLI/Regenerate Menu Items")]
        private static void RegenerateMenuItems()
        {
            CliMenuGenerator.Generate(UniOpenCliSettings.Commands);
            AssetDatabase.Refresh();
        }
    }
}

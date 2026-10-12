using System.IO;
using UnityEditor;

namespace UniOpenCLI.Editor
{
    /// <summary>
    /// Target of the generated <c>[MenuItem]</c> methods: maps a menu slot index to its
    /// command and resolves the directory of the asset the user right-clicked.
    /// </summary>
    public static class CliContextMenu
    {
        public static bool IsEnabled(int index)
        {
            var commands = UniOpenCliSettings.Commands;

            if (index < 0 || index >= commands.Count)
                return false;

            var command = commands[index];
            return command.Enabled && !string.IsNullOrWhiteSpace(command.Command);
        }

        public static void Open(int index)
        {
            var commands = UniOpenCliSettings.Commands;

            if (index < 0 || index >= commands.Count)
                return;

            UniOpenCli.Open(commands[index].Command, GetSelectedDirectory());
        }

        private static string GetSelectedDirectory()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (string.IsNullOrEmpty(path))
                return Directory.GetCurrentDirectory();

            if (File.Exists(path))
                path = Path.GetDirectoryName(path);

            return Path.GetFullPath(path);
        }
    }
}

using System.IO;
using UnityEngine;

namespace UniOpenCLI.Editor
{
    /// <summary>
    /// Public entry point of UniOpenCLI for editor scripting.
    /// </summary>
    /// <example>
    /// <code>
    /// // Open a terminal at the project root and run a command:
    /// UniOpenCli.Open("git status");
    ///
    /// // Open it in a specific folder:
    /// UniOpenCli.Open("claude", "Assets/Scripts");
    ///
    /// // Plug in a custom terminal:
    /// UniOpenCli.Launcher = new MyITerm2Launcher();
    /// </code>
    /// </example>
    public static class UniOpenCli
    {
        private static ITerminalLauncher launcher;

        /// <summary>Terminal launcher for the current platform. Assign to override.</summary>
        public static ITerminalLauncher Launcher
        {
            get => launcher ??= CreateDefaultLauncher();
            set => launcher = value;
        }

        /// <summary>
        /// Opens a terminal in <paramref name="workingDirectory"/> (project root when null)
        /// and runs <paramref name="command"/> in it.
        /// </summary>
        public static void Open(string command, string workingDirectory = null)
        {
            // Launchers may start the terminal with an arbitrary CWD (e.g. Terminal.app
            // opens at $HOME), so relative paths must be resolved here.
            string directory = Path.GetFullPath(workingDirectory ?? Directory.GetCurrentDirectory());
            Launcher.Open(command, directory);
        }

        private static ITerminalLauncher CreateDefaultLauncher()
        {
            return Application.platform switch
            {
                RuntimePlatform.OSXEditor => new MacTerminalLauncher(),
                RuntimePlatform.LinuxEditor => new LinuxTerminalLauncher(),
                _ => new WindowsTerminalLauncher()
            };
        }
    }
}

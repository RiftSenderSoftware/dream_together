using System;
using System.Diagnostics;

namespace UniOpenCLI.Editor
{
    /// <summary>Opens the first available terminal emulator on Linux.</summary>
    internal sealed class LinuxTerminalLauncher : ITerminalLauncher
    {
        public void Open(string command, string workingDirectory)
        {
            // "exec bash" keeps the window open after the command exits.
            string shell = $"cd '{workingDirectory.Replace("'", "'\\''")}'; {command}; exec bash";
            string escaped = shell.Replace("\"", "\\\"");

            var candidates = new (string FileName, string Arguments)[]
            {
                ("gnome-terminal", $"-- bash -c \"{escaped}\""),
                ("konsole", $"-e bash -c \"{escaped}\""),
                ("xfce4-terminal", $"-x bash -c \"{escaped}\""),
                ("x-terminal-emulator", $"-e bash -c \"{escaped}\""),
                ("xterm", $"-e bash -c \"{escaped}\"")
            };

            foreach (var (fileName, arguments) in candidates)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        UseShellExecute = false
                    });
                    return;
                }
                catch (Exception)
                {
                    // Not installed — try the next emulator.
                }
            }

            UnityEngine.Debug.LogError(
                "[UniOpenCLI] No supported terminal emulator found. " +
                "Assign a custom UniOpenCli.Launcher for your environment.");
        }
    }
}

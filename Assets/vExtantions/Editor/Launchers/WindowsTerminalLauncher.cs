using System.Diagnostics;

namespace UniOpenCLI.Editor
{
    /// <summary>Opens PowerShell on Windows.</summary>
    internal sealed class WindowsTerminalLauncher : ITerminalLauncher
    {
        // Unity hands child processes the PATH captured at editor startup. Rebuilding it
        // from the registry makes CLIs installed while the editor is running resolvable
        // without an editor restart.
        private const string RefreshPath =
            "$env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')";

        public void Open(string command, string workingDirectory)
        {
            string directory = workingDirectory.Replace("'", "''");

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -Command \"{RefreshPath}; Set-Location -LiteralPath '{directory}'; {command}\"",
                UseShellExecute = true
            });
        }
    }
}

using System.Diagnostics;
using System.IO;

namespace UniOpenCLI.Editor
{
    /// <summary>Opens Terminal.app on macOS.</summary>
    internal sealed class MacTerminalLauncher : ITerminalLauncher
    {
        public void Open(string command, string workingDirectory)
        {
            // Terminal.app runs .command files in a new window. A temp script avoids the
            // three levels of quote escaping an osascript one-liner would require.
            string script = Path.Combine(Path.GetTempPath(), "UniOpenCLI.command");
            string directory = workingDirectory.Replace("'", "'\\''");

            File.WriteAllText(script, $"#!/bin/bash\ncd '{directory}'\nclear\n{command}\n");

            Process.Start("chmod", $"+x \"{script}\"")?.WaitForExit();
            Process.Start("open", $"\"{script}\"");
        }
    }
}

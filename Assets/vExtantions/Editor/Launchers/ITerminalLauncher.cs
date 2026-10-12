namespace UniOpenCLI.Editor
{
    /// <summary>
    /// Opens a terminal window in a directory and runs a shell command in it.
    /// Assign a custom implementation to <see cref="UniOpenCli.Launcher"/> to integrate
    /// a different terminal (Windows Terminal profile, iTerm2, tmux, ...).
    /// </summary>
    public interface ITerminalLauncher
    {
        /// <param name="command">Shell command to run, e.g. <c>claude</c>.</param>
        /// <param name="workingDirectory">Absolute path the terminal starts in.</param>
        void Open(string command, string workingDirectory);
    }
}

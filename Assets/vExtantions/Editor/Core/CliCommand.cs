using System;
using System.Collections.Generic;

namespace UniOpenCLI.Editor
{
    /// <summary>A single entry in the <c>Assets → Open CLI</c> context menu.</summary>
    [Serializable]
    public class CliCommand
    {
        /// <summary>Menu item label. <c>/</c> is replaced with <c>-</c> to avoid nested submenus.</summary>
        public string Name;

        /// <summary>Shell command executed in the opened terminal, e.g. <c>claude</c> or <c>git status</c>.</summary>
        public string Command;

        /// <summary>Disabled commands keep their menu item, but it is greyed out.</summary>
        public bool Enabled = true;
    }

    // Field name "Items" is part of the persisted JSON format — do not rename.
    [Serializable]
    internal class CliCommandList
    {
        public List<CliCommand> Items = new();
    }
}

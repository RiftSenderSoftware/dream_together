using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniOpenCLI.Editor
{
    /// <summary>
    /// Persists the command list in <see cref="EditorPrefs"/>, so every developer on a team
    /// keeps an individual setup and nothing leaks into version control.
    /// </summary>
    public static class UniOpenCliSettings
    {
        // Unchanged since 1.0 so settings survive package upgrades.
        private const string PrefsKey = "UniOpenCLI.Commands";

        private static List<CliCommand> cached;

        /// <summary>Current command list. Treat as read-only; persist changes via <see cref="Save"/>.</summary>
        public static IReadOnlyList<CliCommand> Commands => cached ??= Read();

        /// <summary>Returns a deep copy safe to mutate in UI code before calling <see cref="Save"/>.</summary>
        public static List<CliCommand> LoadEditableCopy()
        {
            cached ??= Read();
            return Clone(cached);
        }

        /// <summary>Persists the list and regenerates the context menu items.</summary>
        public static void Save(List<CliCommand> commands)
        {
            EditorPrefs.SetString(PrefsKey, ToJson(commands));
            cached = Clone(commands);
            CliMenuGenerator.Generate(cached);
        }

        public static List<CliCommand> CreateDefaults() => new()
        {
            new CliCommand { Name = "Claude", Command = "claude", Enabled = true },
            new CliCommand { Name = "Codex", Command = "codex", Enabled = true },
            new CliCommand { Name = "Gemini", Command = "gemini", Enabled = false }
        };

        internal static string ToJson(List<CliCommand> commands)
        {
            return JsonUtility.ToJson(new CliCommandList { Items = commands });
        }

        private static List<CliCommand> Read()
        {
            string json = EditorPrefs.GetString(PrefsKey, "");

            if (string.IsNullOrEmpty(json))
                return CreateDefaults();

            return JsonUtility.FromJson<CliCommandList>(json).Items;
        }

        private static List<CliCommand> Clone(List<CliCommand> commands)
        {
            return JsonUtility.FromJson<CliCommandList>(ToJson(commands)).Items;
        }
    }
}

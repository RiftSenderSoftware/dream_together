using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniOpenCLI.Editor
{
    /// <summary>Settings page under <c>Edit → Preferences → UniOpen CLI</c>.</summary>
    internal sealed class UniOpenCliSettingsProvider : SettingsProvider
    {
        public const string SettingsPath = "Preferences/UniOpen CLI";

        private List<CliCommand> commands;
        private string savedJson;
        private ReorderableList list;
        private Vector2 scroll;

        private UniOpenCliSettingsProvider()
            : base(SettingsPath, SettingsScope.User,
                new[] { "cli", "terminal", "shell", "console", "claude", "codex", "gemini" })
        {
        }

        [SettingsProvider]
        private static SettingsProvider Create() => new UniOpenCliSettingsProvider();

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            Revert();
        }

        public override void OnGUI(string searchContext)
        {
            if (list == null)
                Revert();

            bool isDirty = UniOpenCliSettings.ToJson(commands) != savedJson;

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "Each enabled command becomes an item under Assets → Open CLI. " +
                $"Applying rewrites {CliMenuGenerator.MenuFilePath} and triggers a short script reload.",
                MessageType.Info);
            EditorGUILayout.Space(4);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            list.DoLayoutList();
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!isDirty))
                {
                    if (GUILayout.Button("Apply"))
                        Apply();

                    if (GUILayout.Button("Revert"))
                    {
                        Revert();
                        GUI.FocusControl(null);
                    }
                }

                if (GUILayout.Button("Reset to Defaults"))
                {
                    commands = UniOpenCliSettings.CreateDefaults();
                    BuildList();
                    GUI.FocusControl(null);
                }
            }

            if (isDirty)
                EditorGUILayout.HelpBox("Unsaved changes — press Apply to update the menu.", MessageType.Warning);
        }

        private void Apply()
        {
            UniOpenCliSettings.Save(commands);
            savedJson = UniOpenCliSettings.ToJson(commands);
            AssetDatabase.Refresh();
        }

        private void Revert()
        {
            commands = UniOpenCliSettings.LoadEditableCopy();
            savedJson = UniOpenCliSettings.ToJson(commands);
            BuildList();
        }

        private void BuildList()
        {
            list = new ReorderableList(commands, typeof(CliCommand), true, true, true, true)
            {
                elementHeight = EditorGUIUtility.singleLineHeight + 6,
                drawHeaderCallback = rect =>
                {
                    // Mirror the element layout so the header doubles as column titles.
                    float x = rect.x + 14;
                    EditorGUI.LabelField(new Rect(x, rect.y, 24, rect.height), "On");
                    float nameWidth = (rect.width - 40) * 0.35f;
                    EditorGUI.LabelField(new Rect(x + 26, rect.y, nameWidth, rect.height), "Name");
                    EditorGUI.LabelField(new Rect(x + 30 + nameWidth, rect.y, rect.width - nameWidth - 30, rect.height), "Command");
                },
                drawElementCallback = DrawElement,
                onAddCallback = _ => commands.Add(new CliCommand { Name = "New CLI", Command = "", Enabled = true })
            };
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var command = commands[index];

            rect.y += 3;
            rect.height = EditorGUIUtility.singleLineHeight;

            var toggleRect = new Rect(rect.x, rect.y, 18, rect.height);
            float nameWidth = (rect.width - 26) * 0.35f;
            var nameRect = new Rect(toggleRect.xMax + 4, rect.y, nameWidth, rect.height);
            var commandRect = new Rect(nameRect.xMax + 4, rect.y, rect.xMax - nameRect.xMax - 4, rect.height);

            command.Enabled = EditorGUI.Toggle(toggleRect, command.Enabled);
            command.Name = EditorGUI.TextField(nameRect, command.Name);
            command.Command = EditorGUI.TextField(commandRect, command.Command);
        }
    }
}

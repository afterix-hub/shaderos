using UnityEditor;
using UnityEngine;
using ToolsStudio.ShaderOS.Audit;

namespace ToolsStudio.ShaderOS.Editor.Settings
{
    [FilePath("ProjectSettings/ShaderOSProjectSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class ShaderOSProjectSettings : ScriptableSingleton<ShaderOSProjectSettings>
    {
        public static ShaderOSProjectSettings Instance => instance;

        [SerializeField] private ScanScope _defaultScanScope              = ScanScope.FullProject;
        [SerializeField] private string    _defaultScanFolder             = "Assets";
        [SerializeField] private int       _healthScoreWarningThreshold   = 75;
        [SerializeField] private int       _healthScoreCriticalThreshold  = 50;
        [SerializeField] private bool      _warnOnSRPBatcherIncompat      = true;
        [SerializeField] private bool      _warnOnKeywordBudget           = true;
        [SerializeField, Range(50, 99)]
        private int _keywordBudgetWarningPercent = 75;

        public ScanScope DefaultScanScope
        {
            get => _defaultScanScope;
            set { _defaultScanScope = value; Save(true); }
        }

        public string DefaultScanFolder
        {
            get => _defaultScanFolder;
            set { _defaultScanFolder = value; Save(true); }
        }

        public int HealthScoreWarningThreshold
        {
            get => _healthScoreWarningThreshold;
            set { _healthScoreWarningThreshold = value; Save(true); }
        }

        public int HealthScoreCriticalThreshold
        {
            get => _healthScoreCriticalThreshold;
            set { _healthScoreCriticalThreshold = value; Save(true); }
        }

        public bool WarnOnSRPBatcherIncompatibility
        {
            get => _warnOnSRPBatcherIncompat;
            set { _warnOnSRPBatcherIncompat = value; Save(true); }
        }

        public bool WarnOnKeywordBudgetExceedingPercent
        {
            get => _warnOnKeywordBudget;
            set { _warnOnKeywordBudget = value; Save(true); }
        }

        public int KeywordBudgetWarningPercent
        {
            get => _keywordBudgetWarningPercent;
            set { _keywordBudgetWarningPercent = value; Save(true); }
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Tools Studio/ShaderOS", SettingsScope.Project)
            {
                label      = "ShaderOS",
                guiHandler = DrawSettings,
                keywords   = new[] { "Tools Studio", "ShaderOS", "Shader", "Material", "Audit" }
            };
        }

        private static void DrawSettings(string searchContext)
        {
            var s = instance;
            // Min width prevents label truncation — the settings panel is wide enough for these labels.
            EditorGUIUtility.labelWidth = 240f;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Scan", EditorStyles.boldLabel);

            var newScope = (ScanScope)EditorGUILayout.EnumPopup("Default scope", s.DefaultScanScope);
            if (newScope != s.DefaultScanScope) s.DefaultScanScope = newScope;

            if (s.DefaultScanScope == ScanScope.Folder)
            {
                EditorGUILayout.BeginHorizontal();
                var newFolder = EditorGUILayout.TextField("Scan folder", s.DefaultScanFolder);
                if (newFolder != s.DefaultScanFolder) s.DefaultScanFolder = newFolder;
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string picked = EditorUtility.OpenFolderPanel("Select Scan Folder", s.DefaultScanFolder, string.Empty);
                    if (!string.IsNullOrEmpty(picked))
                    {
                        if (TryGetProjectRelativePath(picked, out string relative))
                        {
                            s.DefaultScanFolder = relative;
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Folder Outside Project",
                                "ShaderOS can only scan folders inside the current Unity project " +
                                "(Assets, Packages, ProjectSettings, or another project-root folder). " +
                                "The folder you selected is outside the project and was not applied.",
                                "OK");
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Health Score Thresholds", EditorStyles.boldLabel);

            var newWarn = EditorGUILayout.IntSlider("Warning below", s.HealthScoreWarningThreshold, 1, 99);
            if (newWarn != s.HealthScoreWarningThreshold) s.HealthScoreWarningThreshold = newWarn;

            var newCrit = EditorGUILayout.IntSlider("Critical below", s.HealthScoreCriticalThreshold, 1, newWarn - 1);
            if (newCrit != s.HealthScoreCriticalThreshold) s.HealthScoreCriticalThreshold = newCrit;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Analysis Options", EditorStyles.boldLabel);

            var newSRP = EditorGUILayout.Toggle("Warn on SRP Batcher incompatibility", s.WarnOnSRPBatcherIncompatibility);
            if (newSRP != s.WarnOnSRPBatcherIncompatibility) s.WarnOnSRPBatcherIncompatibility = newSRP;

            var newKw = EditorGUILayout.Toggle("Warn when keyword budget exceeds threshold", s.WarnOnKeywordBudgetExceedingPercent);
            if (newKw != s.WarnOnKeywordBudgetExceedingPercent) s.WarnOnKeywordBudgetExceedingPercent = newKw;

            if (s.WarnOnKeywordBudgetExceedingPercent)
            {
                var newPct = EditorGUILayout.IntSlider("Keyword warning threshold (%)", s.KeywordBudgetWarningPercent, 50, 99);
                if (newPct != s.KeywordBudgetWarningPercent) s.KeywordBudgetWarningPercent = newPct;
            }

            EditorGUILayout.Space(8);
            EditorGUIUtility.labelWidth = 0f; // reset
            EditorGUILayout.LabelField("Tools Studio  ·  ShaderOS", EditorStyles.miniLabel);
        }

        // Accepts only paths inside the current project root (Assets, Packages, ProjectSettings,
        // or any other project-root folder) — Application.dataPath always ends in "/Assets".
        private static bool TryGetProjectRelativePath(string absolutePath, out string relativePath)
        {
            string picked = absolutePath.Replace('\\', '/').TrimEnd('/');
            string dataPath = Application.dataPath.Replace('\\', '/').TrimEnd('/');
            string projectRoot = dataPath.Substring(0, dataPath.Length - "Assets".Length).TrimEnd('/');

            if (string.Equals(picked, dataPath, System.StringComparison.OrdinalIgnoreCase))
            {
                relativePath = "Assets";
                return true;
            }
            if (picked.StartsWith(dataPath + "/", System.StringComparison.OrdinalIgnoreCase))
            {
                relativePath = "Assets/" + picked.Substring(dataPath.Length + 1);
                return true;
            }
            if (string.Equals(picked, projectRoot, System.StringComparison.OrdinalIgnoreCase))
            {
                relativePath = string.Empty;
                return true;
            }
            if (picked.StartsWith(projectRoot + "/", System.StringComparison.OrdinalIgnoreCase))
            {
                relativePath = picked.Substring(projectRoot.Length + 1);
                return true;
            }

            relativePath = null;
            return false;
        }
    }
}

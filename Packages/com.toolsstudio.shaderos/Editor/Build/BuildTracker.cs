using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ToolsStudio.ShaderOS.Editor.Build
{
    [Serializable]
    internal sealed class BuildShaderRecord
    {
        public string BuildId;
        public string Platform;
        public long   ShaderCompileMilliseconds;
        public int    ShaderCount;
        public long   UnixTimestampSeconds;

        public DateTime Timestamp => DateTimeOffset.FromUnixTimeSeconds(UnixTimestampSeconds).UtcDateTime;

        // NaN when there is no prior build to compare against.
        [NonSerialized]
        public float ChangeVsPreviousPercent;
    }

    [Serializable]
    internal sealed class BuildHistory
    {
        private const int    MAX_FREE_BUILDS = 10;
        private const string PREFS_KEY       = "ToolsStudio.ShaderOS.BuildHistory";

        [SerializeField]
        private List<BuildShaderRecord> _records = new List<BuildShaderRecord>(MAX_FREE_BUILDS + 1);

        private static BuildHistory _instance;
        public static BuildHistory Instance => _instance ??= Load();

        public IReadOnlyList<BuildShaderRecord> Records => _records;

        public void Add(BuildShaderRecord record, bool isProTier)
        {
            _records.Add(record);
            int limit = isProTier ? int.MaxValue : MAX_FREE_BUILDS;
            while (_records.Count > limit) _records.RemoveAt(0);
            AnnotateChangePercents();
            Save();
        }

        public void Clear()
        {
            _records.Clear();
            EditorPrefs.DeleteKey(PREFS_KEY);
        }

        private void AnnotateChangePercents()
        {
            for (int i = 0; i < _records.Count; i++)
            {
                if (i == 0) { _records[i].ChangeVsPreviousPercent = float.NaN; continue; }
                long prev = _records[i - 1].ShaderCompileMilliseconds;
                long curr = _records[i].ShaderCompileMilliseconds;
                _records[i].ChangeVsPreviousPercent = prev == 0 ? 0f : (float)(curr - prev) / prev * 100f;
            }
        }

        private void Save()
        {
            EditorPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(this));
        }

        private static BuildHistory Load()
        {
            if (!EditorPrefs.HasKey(PREFS_KEY)) return new BuildHistory();
            try
            {
                var h = new BuildHistory();
                JsonUtility.FromJsonOverwrite(EditorPrefs.GetString(PREFS_KEY), h);
                h.AnnotateChangePercents();
                return h;
            }
            catch (Exception ex)
            {
                ShaderOSLogger.Error(nameof(BuildHistory), "Failed to load build history from EditorPrefs", ex);
                return new BuildHistory();
            }
        }
    }

    internal sealed class ShaderOSBuildTracker : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 999;

        public void OnPreprocessBuild(BuildReport report) { }

        public void OnPostprocessBuild(BuildReport report)
        {
            try
            {
                long shaderMs    = ExtractShaderCompileTime(report);
                int  shaderCount = CountShaders(report);

                var record = new BuildShaderRecord
                {
                    BuildId                   = System.IO.Path.GetFileNameWithoutExtension(report.summary.outputPath)
                                                + "_" + DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Platform                  = report.summary.platform.ToString(),
                    ShaderCompileMilliseconds = shaderMs,
                    ShaderCount               = shaderCount,
                    UnixTimestampSeconds      = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                BuildHistory.Instance.Add(record, isProTier: false);
            }
            catch (Exception ex)
            {
                ShaderOSLogger.Error(nameof(ShaderOSBuildTracker), "Failed to capture build shader timing", ex);
            }
        }

        private static long ExtractShaderCompileTime(BuildReport report)
        {
#if UNITY_2020_1_OR_NEWER
            foreach (var step in report.steps)
            {
                if (step.name.Contains("Compiling shader",  StringComparison.OrdinalIgnoreCase) ||
                    step.name.Contains("shader programs",   StringComparison.OrdinalIgnoreCase))
                    return (long)step.duration.TotalMilliseconds;
            }
#endif
            return (long)report.summary.totalTime.TotalMilliseconds;
        }

        private static int CountShaders(BuildReport report)
        {
#if UNITY_2020_1_OR_NEWER
            int count = 0;
            foreach (var file in report.GetFiles())
                if (file.path.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
                    count++;
            return count;
#else
            return 0;
#endif
        }
    }
}

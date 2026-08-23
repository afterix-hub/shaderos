using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ToolsStudio.ShaderOS.Audit;
using ToolsStudio.ShaderOS.Keywords;

namespace ToolsStudio.ShaderOS.Editor.Analysis
{
    internal sealed class ShaderKeywordAnalyzer
    {
        public (List<string> globalKeywords, List<string> localKeywords) ExtractKeywords(Shader shader)
        {
            var global = new List<string>();
            var local  = new List<string>();

            if (shader == null) return (global, local);

            string assetPath = AssetDatabase.GetAssetPath(shader);
            if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
            {
                ParsePragmas(assetPath, global, local);
                return (global, local);
            }

            // Built-in / Shader Graph — enumerate keyword space, all treated as local.
            // LocalKeyword.isGlobal was removed in Unity 6000.
#if UNITY_2021_1_OR_NEWER
            if (shader.keywordSpace != null)
                foreach (var kw in shader.keywordSpace.keywords)
                    if (!string.IsNullOrEmpty(kw.name)) local.Add(kw.name);
#endif
            return (global, local);
        }

        public KeywordBudgetReport BuildBudgetReport(IReadOnlyList<ShaderRecord> shaders)
        {
            if (shaders == null || shaders.Count == 0) return KeywordBudgetReport.Empty();

            var allGlobal       = new HashSet<string>(StringComparer.Ordinal);
            var globalFrequency = new Dictionary<string, int>(StringComparer.Ordinal);
            var overBudget      = new List<string>();
            var contributions   = new List<ShaderKeywordContribution>(shaders.Count);

            foreach (var sr in shaders)
            {
                foreach (var kw in sr.GlobalKeywords)
                {
                    allGlobal.Add(kw);
                    globalFrequency.TryGetValue(kw, out int freq);
                    globalFrequency[kw] = freq + 1;
                }
                if (sr.LocalKeywordCount > KeywordBudgetReport.LOCAL_KEYWORD_PER_SHADER)
                    overBudget.Add(sr.Guid);
            }

            foreach (var sr in shaders)
            {
                var unique    = new List<string>();
                var duplicate = new List<string>();
                foreach (var kw in sr.GlobalKeywords)
                {
                    if (globalFrequency.TryGetValue(kw, out int freq) && freq > 1) duplicate.Add(kw);
                    else unique.Add(kw);
                }
                contributions.Add(new ShaderKeywordContribution(sr.Guid, sr.Name,
                    unique.AsReadOnly(), duplicate.AsReadOnly(), sr.LocalKeywordCount));
            }

            var duplicates = new List<string>();
            foreach (var kvp in globalFrequency)
                if (kvp.Value > 1) duplicates.Add(kvp.Key);

            var allGlobalList = new List<string>(allGlobal);
            allGlobalList.Sort(StringComparer.Ordinal);

            return new KeywordBudgetReport(
                allGlobal.Count,
                allGlobalList.AsReadOnly(),
                duplicates.AsReadOnly(),
                contributions.AsReadOnly(),
                overBudget.AsReadOnly());
        }

        // multi_compile variants contribute global keywords; shader_feature_local/_local variants are local.
        private static void ParsePragmas(string assetPath, List<string> global, List<string> local)
        {
            string fullPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), assetPath);
            if (!System.IO.File.Exists(fullPath)) return;

            foreach (string rawLine in System.IO.File.ReadLines(fullPath))
            {
                string line = rawLine.TrimStart();
                if (!line.StartsWith("#pragma ", StringComparison.Ordinal)) continue;

                int commentIdx = line.IndexOf("//", StringComparison.Ordinal);
                if (commentIdx >= 0) line = line.Substring(0, commentIdx);

                string lower = line.ToLowerInvariant();
                if (!lower.Contains("multi_compile") && !lower.Contains("shader_feature")) continue;

                bool isLocal = lower.Contains("shader_feature_local") || lower.Contains("multi_compile_local");

                string[] tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                int startIdx = 1;
                while (startIdx < tokens.Length &&
                       (tokens[startIdx].StartsWith("multi_compile", StringComparison.OrdinalIgnoreCase) ||
                        tokens[startIdx].StartsWith("shader_feature", StringComparison.OrdinalIgnoreCase)))
                    startIdx++;

                for (int i = startIdx; i < tokens.Length; i++)
                {
                    string token = tokens[i].Trim();
                    if (!string.IsNullOrEmpty(token) && token != "_")
                        (isLocal ? local : global).Add(token);
                }
            }
        }
    }
}

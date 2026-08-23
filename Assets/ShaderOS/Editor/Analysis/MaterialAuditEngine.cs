using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using ToolsStudio.ShaderOS.Audit;
using ToolsStudio.ShaderOS.Compatibility;
using ToolsStudio.ShaderOS.Dependency;
using ToolsStudio.ShaderOS.Interfaces;

namespace ToolsStudio.ShaderOS.Editor.Analysis
{
    internal sealed class MaterialAuditEngine
    {
        private const int FREE_TIER_LIMIT = 100;

        private readonly BrokenReferenceDetector _referenceDetector  = new BrokenReferenceDetector();
        private readonly SRPBatcherAnalyzer       _srpBatcherAnalyzer = new SRPBatcherAnalyzer();
        private readonly ShaderKeywordAnalyzer    _keywordAnalyzer    = new ShaderKeywordAnalyzer();
        private readonly ShaderVariantAnalyzer    _variantAnalyzer    = new ShaderVariantAnalyzer();
        private readonly DependencyGraphBuilder   _graphBuilder       = new DependencyGraphBuilder();
        private readonly bool                     _isProTier;

        public MaterialAuditEngine(bool isProTier = false) => _isProTier = isProTier;

        public AuditReport RunAudit(ScanScope scope, string scopeFolderPath = null, IScanProgressReporter reporter = null)
        {
            var timer = Stopwatch.StartNew();
            ShaderOSLogger.Info(this, "Starting scan");

            string[] allGuids     = DiscoverGuids(scope, scopeFolderPath);
            int      totalInProj  = allGuids.Length;
            bool     isPartial    = !_isProTier && totalInProj > FREE_TIER_LIMIT;
            int      analyzeCount = isPartial ? FREE_TIER_LIMIT : totalInProj;

            var materialRecords = new List<MaterialRecord>(analyzeCount);
            var shaderRecordMap = new Dictionary<string, ShaderRecord>();

            try
            {
                EditorUtility.DisplayProgressBar("ShaderOS", "Collecting materials…", 0f);
                ShaderOSLogger.Info(this, "Collecting materials", new { found = totalInProj, analyzing = analyzeCount });

                for (int i = 0; i < analyzeCount; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(allGuids[i]);
                    string fileName  = System.IO.Path.GetFileName(assetPath);

                    EditorUtility.DisplayProgressBar("ShaderOS", $"Analyzing shaders: {fileName}", (float)(i + 1) / analyzeCount);

                    var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                    if (material == null) continue;

                    var record = BuildMaterialRecord(allGuids[i], assetPath, material);
                    materialRecords.Add(record);

                    if (!string.IsNullOrEmpty(record.ShaderGuid) && !shaderRecordMap.ContainsKey(record.ShaderGuid))
                        shaderRecordMap[record.ShaderGuid] = BuildShaderRecord(record.ShaderGuid, material.shader);
                }

                var shaderList = new List<ShaderRecord>(shaderRecordMap.Values);

                EditorUtility.DisplayProgressBar("ShaderOS", "Building dependency graph…", 0.92f);
                var graph = _graphBuilder.Build(materialRecords.AsReadOnly(), shaderList.AsReadOnly());

                EditorUtility.DisplayProgressBar("ShaderOS", "Calculating keyword budget…", 0.96f);
                var keywordBudget  = _keywordAnalyzer.BuildBudgetReport(shaderList.AsReadOnly());
                var variantSummary = _variantAnalyzer.BuildSummary(shaderList.AsReadOnly());

                EditorUtility.DisplayProgressBar("ShaderOS", "Generating report…", 0.99f);
                timer.Stop();

                var report = new AuditReport(
                    DateTime.UtcNow, timer.Elapsed, scope, scopeFolderPath ?? string.Empty,
                    materialRecords.AsReadOnly(), shaderList.AsReadOnly(),
                    graph, keywordBudget, variantSummary,
                    isPartial, totalInProj);

                ShaderOSLogger.Info(this, "Scan completed", new {
                    materials = materialRecords.Count, shaders = shaderList.Count,
                    seconds   = Math.Round(timer.Elapsed.TotalSeconds, 1)
                });

                reporter?.OnComplete(report);
                return report;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private MaterialRecord BuildMaterialRecord(string guid, string assetPath, Material material)
        {
            var issues  = new List<MaterialIssue>();
            int penalty = 0;

            bool brokenShader = _referenceDetector.IsShaderBroken(material);
            string shaderGuid = string.Empty;

            if (brokenShader)
            {
                var issue = MaterialIssueLibrary.BrokenShaderReference();
                issues.Add(issue);
                penalty += issue.HealthPenalty;
            }
            else if (material.shader != null)
            {
                shaderGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(material.shader));
            }

            _referenceDetector.HasMissingTextures(material, out var missingSlots);
            foreach (string slot in missingSlots)
            {
                var issue = MaterialIssueLibrary.MissingTexture(slot);
                issues.Add(issue);
                penalty += issue.HealthPenalty;
            }

            var srpResult = brokenShader
                ? new SRPBatcherAnalysisResult(guid, SRPBatcherState.Unknown, null, 0)
                : _srpBatcherAnalyzer.Analyze(guid, material);

            if (srpResult.State == SRPBatcherState.Incompatible)
            {
                var issue = MaterialIssueLibrary.SRPBatcherIncompatible(srpResult.Breakers);
                issues.Add(issue);
                penalty += issue.HealthPenalty;
            }

            issues.Sort((a, b) => b.Severity.CompareTo(a.Severity));

            return new MaterialRecord(
                guid, material.name, assetPath,
                HealthScore.FromPenalties(penalty),
                brokenShader, missingSlots.Count > 0,
                shaderGuid, material.shader?.name ?? "—",
                srpResult.State, issues.AsReadOnly(),
                DetectPipeline(material.shader), isOrphaned: false);
        }

        private ShaderRecord BuildShaderRecord(string shaderGuid, Shader shader)
        {
            if (shader == null) return MakeUnknownShaderRecord(shaderGuid);

            string assetPath = AssetDatabase.GetAssetPath(shader);
            var kw = _keywordAnalyzer.ExtractKeywords(shader);
            int variants = _variantAnalyzer.EstimateVariants(shader, kw.globalKeywords.AsReadOnly(), kw.localKeywords.AsReadOnly());

            return new ShaderRecord(
                shaderGuid, shader.name, assetPath,
                isBuiltIn: string.IsNullOrEmpty(assetPath),
                materialReferenceCount: 0,
                globalKeywordCount: kw.globalKeywords.Count,
                localKeywordCount:  kw.localKeywords.Count,
                estimatedVariantCount: variants,
                kw.globalKeywords.AsReadOnly(),
                kw.localKeywords.AsReadOnly(),
                DetectPipeline(shader));
        }

        private static ShaderRecord MakeUnknownShaderRecord(string guid) => new ShaderRecord(
            guid, "<unknown>", string.Empty, false, 0, 0, 0, 0,
            Array.AsReadOnly(Array.Empty<string>()),
            Array.AsReadOnly(Array.Empty<string>()),
            "Unresolved");

        private static string[] DiscoverGuids(ScanScope scope, string folderPath)
        {
            string[] searchIn = scope == ScanScope.Folder && !string.IsNullOrEmpty(folderPath)
                ? new[] { folderPath } : null;
            return searchIn != null
                ? AssetDatabase.FindAssets("t:Material", searchIn)
                : AssetDatabase.FindAssets("t:Material");
        }

        // Pipeline detection: covers URP, HDRP, BiRP standard shaders, and common shader patterns.
        // "Unresolved" (not "Unknown") for custom shaders we can't categorize — honest about limitation.
        private static string DetectPipeline(Shader shader)
        {
            if (shader == null) return "Unresolved";
            string n = shader.name;

            if (n.StartsWith("Universal Render Pipeline/", StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("URP/",                        StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("Sprites/",                    StringComparison.OrdinalIgnoreCase) ||
                n.Contains("/URP/",                         StringComparison.OrdinalIgnoreCase))
                return "URP";

            if (n.StartsWith("HDRP/",                  StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("High Definition RP/",    StringComparison.OrdinalIgnoreCase) ||
                n.Contains("/HDRP/",                   StringComparison.OrdinalIgnoreCase))
                return "HDRP";

            if (n.StartsWith("Standard",           StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("Mobile/",             StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("Legacy Shaders/",     StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("Particles/",          StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("Nature/",             StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("Autodesk Interactive", StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("GUI/",                StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("Skybox/",             StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("Diffuse",             StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("Transparent",         StringComparison.OrdinalIgnoreCase))
                return "BiRP";

            // Hidden/Shader Graphs and VFX Graph shaders — they run on whatever pipeline is active.
            if (n.StartsWith("Hidden/", StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("Shader Graphs/",   StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("VFX Graph/",        StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("2D/",               StringComparison.OrdinalIgnoreCase))
                return "Graph";

            return "Unresolved";
        }
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ToolsStudio.ShaderOS.Audit;
using ToolsStudio.ShaderOS.Keywords;
using ToolsStudio.ShaderOS.Variants;
using ToolsStudio.ShaderOS.Compatibility;
using ToolsStudio.ShaderOS.Editor.Styles;

namespace ToolsStudio.ShaderOS.Editor.Windows.Panels
{
    internal sealed class OverviewPanel
    {
        private Vector2 _issuesScroll;

        public void Draw(AuditReport report)
        {
            EditorGUILayout.Space(6);

            if (report.IsPartial)
            {
                EditorGUILayout.HelpBox(
                    $"Free edition: analyzed {report.AnalyzedMaterialCount} of {report.TotalMaterialsInProject} materials.",
                    MessageType.Warning);
                EditorGUILayout.Space(4);
            }

            // Compute counters once — not inside tile drawing to avoid O(N) per repaint.
            int brokenCount = 0, srpIssues = 0, warnCount = 0;
            foreach (var m in report.Materials)
            {
                if (m.HasBrokenShaderReference) brokenCount++;
                if (m.SRPBatcherCompatibility == SRPBatcherState.Incompatible) srpIssues++;
                foreach (var issue in m.Issues)
                    if (issue.Severity == IssueSeverity.Warning) warnCount++;
            }

            DrawKPICards(report, brokenCount, srpIssues);
            EditorGUILayout.Space(6);
            DrawCriticalIssues(report, warnCount);
            EditorGUILayout.Space(8);
            DrawPipelineDistribution(report);
            EditorGUILayout.Space(8);
            DrawDependencyGraphSummary(report);
        }

        private static void DrawKPICards(AuditReport report, int brokenCount, int srpIssues)
        {
            EditorGUILayout.BeginHorizontal();

            // Health — weighted tile: larger score number, health bar
            DrawTile("Project Health", () =>
            {
                int score = report.ProjectHealthScore.Value;
                var col   = ShaderOSEditorStyles.GetHealthColor(score);
                DrawColorLabel(score.ToString(), col, ShaderOSEditorStyles.ScoreLabel, GUILayout.Height(32));
                DrawBar(score / 100f, col);
                EditorGUILayout.LabelField(report.ProjectHealthScore.Severity.ToString(),
                    ShaderOSEditorStyles.TinyLabel);
            });

            GUILayout.Space(4);

            // Keywords
            var budget  = report.KeywordBudget;
            var kwColor = budget.GlobalBudgetLevel == KeywordBudgetLevel.Critical ? ShaderOSEditorStyles.ColorCritical
                        : budget.GlobalBudgetLevel >= KeywordBudgetLevel.Caution  ? ShaderOSEditorStyles.ColorWarning
                                                                                  : ShaderOSEditorStyles.ColorHealthy;
            DrawTile("Global Keywords", () =>
            {
                DrawColorLabel($"{budget.GlobalKeywordCount} / {KeywordBudgetReport.GLOBAL_KEYWORD_LIMIT}",
                    kwColor, ShaderOSEditorStyles.ScoreLabel, GUILayout.Height(32));
                DrawBar(budget.GlobalUsagePercent / 100f, kwColor);
                EditorGUILayout.LabelField(
                    budget.DuplicateKeywords.Count > 0 ? $"{budget.DuplicateKeywords.Count} duplicate(s)" : "No duplicates",
                    ShaderOSEditorStyles.TinyLabel);
            });

            GUILayout.Space(4);

            // Variants — show pre-strip estimate with honest context
            var summary  = report.VariantSummary;
            var varColor = summary.ProjectRiskLevel == VariantRiskLevel.Critical ? ShaderOSEditorStyles.ColorCritical
                         : summary.ProjectRiskLevel >= VariantRiskLevel.Moderate  ? ShaderOSEditorStyles.ColorWarning
                                                                                  : ShaderOSEditorStyles.ColorHealthy;
            DrawTile("Est. Variants (pre-strip)", () =>
            {
                DrawColorLabel(FormatCount(summary.TotalEstimatedVariants),
                    varColor, ShaderOSEditorStyles.ScoreLabel, GUILayout.Height(32));
                EditorGUILayout.LabelField(summary.ProjectRiskLevel.ToString(), ShaderOSEditorStyles.TinyLabel);
                if (summary.CriticalShaderCount > 0)
                    EditorGUILayout.LabelField($"{summary.CriticalShaderCount} shader(s) at critical risk",
                        ShaderOSEditorStyles.TinyLabel);
            });

            GUILayout.Space(4);

            // Broken materials
            DrawTile("Broken Materials", () =>
            {
                var col = brokenCount > 0 ? ShaderOSEditorStyles.ColorCritical : ShaderOSEditorStyles.ColorHealthy;
                DrawColorLabel(brokenCount.ToString(), col, ShaderOSEditorStyles.ScoreLabel, GUILayout.Height(32));
                EditorGUILayout.LabelField(brokenCount == 0 ? "All clear" : "Fix required",
                    ShaderOSEditorStyles.TinyLabel);
            });

            GUILayout.Space(4);

            // SRP Batcher
            DrawTile("SRP Batcher Issues", () =>
            {
                var col = srpIssues > 0 ? ShaderOSEditorStyles.ColorWarning : ShaderOSEditorStyles.ColorHealthy;
                DrawColorLabel(srpIssues.ToString(), col, ShaderOSEditorStyles.ScoreLabel, GUILayout.Height(32));
                EditorGUILayout.LabelField(srpIssues == 0 ? "All clear" : "Performance impact",
                    ShaderOSEditorStyles.TinyLabel);
            });

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCriticalIssues(AuditReport report, int warnCount)
        {
            // Count first — collapse section entirely when clean.
            int errorCount = report.CriticalIssueCount;

            if (errorCount == 0)
            {
                // Clean project — single line, no wasted space.
                EditorGUILayout.BeginHorizontal();
                var prev = GUI.contentColor;
                GUI.contentColor = ShaderOSEditorStyles.ColorHealthy;
                EditorGUILayout.LabelField("✓  No critical issues.", EditorStyles.miniLabel);
                GUI.contentColor = prev;
                if (warnCount > 0)
                    EditorGUILayout.LabelField($"{warnCount} warning(s) — see Materials tab.",
                        ShaderOSEditorStyles.TinyLabel);
                EditorGUILayout.EndHorizontal();
                return;
            }

            EditorGUILayout.LabelField("Critical Issues", ShaderOSEditorStyles.SectionHeader);

            int shown = 0;
            _issuesScroll = EditorGUILayout.BeginScrollView(_issuesScroll, GUILayout.MaxHeight(160));

            foreach (var material in report.Materials)
            {
                foreach (var issue in material.Issues)
                {
                    if (issue.Severity != IssueSeverity.Error) continue;
                    if (shown >= 20) break;

                    EditorGUILayout.BeginHorizontal(ShaderOSEditorStyles.IssueRow);
                    DrawColorLabel("●", ShaderOSEditorStyles.ColorCritical, EditorStyles.miniLabel, GUILayout.Width(14));
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField($"{material.Name}  —  {issue.Description}", EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.LabelField($"→ {issue.Resolution}", ShaderOSEditorStyles.TinyLabel);
                    EditorGUILayout.EndVertical();
                    if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(36)))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<Object>(material.AssetPath);
                        if (asset != null) EditorGUIUtility.PingObject(asset);
                    }
                    EditorGUILayout.EndHorizontal();
                    shown++;
                }
            }

            EditorGUILayout.EndScrollView();

            if (warnCount > 0)
                EditorGUILayout.LabelField($"+ {warnCount} warning(s). See Materials tab.",
                    ShaderOSEditorStyles.TinyLabel);
        }

        private static void DrawPipelineDistribution(AuditReport report)
        {
            int total = report.AnalyzedMaterialCount;
            if (total == 0) return;

            // Count pipeline tags
            var counts = new Dictionary<string, int>();
            foreach (var m in report.Materials)
            {
                counts.TryGetValue(m.PipelineTag, out int c);
                counts[m.PipelineTag] = c + 1;
            }

            EditorGUILayout.LabelField("Pipeline Distribution", ShaderOSEditorStyles.SectionHeader);
            EditorGUILayout.BeginVertical(ShaderOSEditorStyles.IssueRow);
            foreach (var kvp in counts)
            {
                float frac = kvp.Value / (float)total;
                EditorGUILayout.BeginHorizontal();
                // Fixed label width so bars all start at the same x position
                EditorGUILayout.LabelField(kvp.Key, EditorStyles.miniLabel, GUILayout.Width(72));
                var rect = EditorGUILayout.GetControlRect(GUILayout.Height(12));
                DrawBarRect(rect, frac, ShaderOSEditorStyles.ColorAccent);
                EditorGUILayout.LabelField($"{kvp.Value} ({frac * 100:F0}%)", ShaderOSEditorStyles.TinyLabel, GUILayout.Width(72));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private static void DrawDependencyGraphSummary(AuditReport report)
        {
            var graph = report.DependencyGraph;
            int orphanCount = 0;
            foreach (var _ in graph.OrphanNodes) orphanCount++;

            EditorGUILayout.LabelField("Dependency Graph", ShaderOSEditorStyles.SectionHeader);

            // Single explicit-height rect for the whole row, then every element is positioned
            // against that same rect's y/height — guarantees a shared vertical baseline instead
            // of relying on IMGUI's default top-alignment of mismatched-height controls (a mini
            // label and a mini button render at different natural heights, so laying them out
            // via plain GUILayout.BeginHorizontal left the button visibly off-center).
            const float rowHeight = 22f;
            var full = EditorGUILayout.GetControlRect(GUILayout.Height(rowHeight));
            GUI.Box(full, GUIContent.none, ShaderOSEditorStyles.IssueRow);

            var row = new Rect(full.x + 6, full.y, full.width - 12, full.height);

            var nodesRect = new Rect(row.x, row.y, 84, row.height);
            EditorGUI.LabelField(nodesRect, $"Nodes: {graph.NodeCount}", EditorStyles.miniLabel);

            var edgesRect = new Rect(nodesRect.xMax, row.y, 84, row.height);
            EditorGUI.LabelField(edgesRect, $"Edges: {graph.EdgeCount}", EditorStyles.miniLabel);

            var orphanRect = new Rect(edgesRect.xMax, row.y, 140, row.height);
            if (orphanCount > 0)
            {
                var prev = GUI.contentColor;
                GUI.contentColor = ShaderOSEditorStyles.ColorWarning;
                EditorGUI.LabelField(orphanRect, $"Unreferenced: {orphanCount}", EditorStyles.miniLabel);
                GUI.contentColor = prev;
            }
            else
            {
                EditorGUI.LabelField(orphanRect, "No unreferenced assets", ShaderOSEditorStyles.TinyLabel);
            }

            const float buttonWidth = 96f;
            var buttonRect = new Rect(row.xMax - buttonWidth, row.y, buttonWidth, row.height);
            if (GUI.Button(buttonRect, "Learn More", EditorStyles.miniButton))
            {
                Application.OpenURL("https://discord.gg/dXny3f4dcG");
            }

            var messageRect = new Rect(orphanRect.xMax, row.y, buttonRect.x - orphanRect.xMax - 6, row.height);
            GUI.Label(messageRect, "Interactive dependency visualization is not available in the free edition.",
                ShaderOSEditorStyles.ProLockLabel);
        }

        internal static string FormatCount(int n)
            => n >= 1_000_000 ? $"~{n / 1_000_000}M" : n >= 1_000 ? $"~{n / 1_000}K" : n.ToString();

        private static void DrawTile(string title, System.Action content)
        {
            EditorGUILayout.BeginVertical(ShaderOSEditorStyles.WindowChrome, GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField(title, ShaderOSEditorStyles.TileHeader);
            content?.Invoke();
            EditorGUILayout.EndVertical();
        }

        internal static void DrawColorLabel(string text, Color color, GUIStyle style, params GUILayoutOption[] options)
        {
            var prev = GUI.color;
            GUI.color = color;
            GUILayout.Label(text, style, options);
            GUI.color = prev;
        }

        private static void DrawBar(float t, Color fill)
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(ShaderOSEditorStyles.SCORE_BAR_HEIGHT));
            DrawBarRect(rect, t, fill);
        }

        internal static void DrawBarRect(Rect rect, float t, Color fill)
        {
            EditorGUI.DrawRect(rect, new Color(fill.r * 0.25f, fill.g * 0.25f, fill.b * 0.25f, 1f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(t), rect.height), fill);
        }
    }
}

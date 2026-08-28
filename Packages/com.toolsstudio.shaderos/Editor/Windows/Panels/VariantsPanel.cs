using UnityEditor;
using UnityEngine;
using ToolsStudio.ShaderOS.Audit;
using ToolsStudio.ShaderOS.Variants;
using ToolsStudio.ShaderOS.Editor.Styles;

namespace ToolsStudio.ShaderOS.Editor.Windows.Panels
{
    internal sealed class VariantsPanel
    {
        private Vector2 _scroll;

        public void Draw(AuditReport report)
        {
            var summary = report.VariantSummary;
            EditorGUILayout.Space(6);

            DrawProjectSummary(summary);
            EditorGUILayout.Space(8);
            DrawLegend();
            EditorGUILayout.Space(4);
            DrawShaderTable(summary);
            EditorGUILayout.Space(8);
            DrawProUpsell();
        }

        private static void DrawProjectSummary(VariantSummary summary)
        {
            EditorGUILayout.LabelField("Project Variant Estimate", ShaderOSEditorStyles.SectionHeader);
            EditorGUILayout.BeginHorizontal();

            var riskColor = RiskColor(summary.ProjectRiskLevel);
            var prev      = GUI.color;
            GUI.color     = riskColor;
            // Large pre-strip number — font size communicates it's a headline figure, not precise data
            EditorGUILayout.LabelField(OverviewPanel.FormatCount(summary.TotalEstimatedVariants),
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 26 }, GUILayout.Width(110));
            GUI.color = prev;

            EditorGUILayout.BeginVertical();
            var prevC = GUI.contentColor;
            GUI.contentColor = riskColor;
            EditorGUILayout.LabelField(RiskLabel(summary.ProjectRiskLevel), EditorStyles.boldLabel);
            GUI.contentColor = prevC;

            // Honest estimation context inline with the data, not in fine print
            EditorGUILayout.LabelField(
                summary.CriticalShaderCount > 0
                    ? $"{summary.CriticalShaderCount} shader(s) with 10,000+ theoretical variants"
                    : "No shaders at critical variant count.",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Upper bound — actual compiled count reduced by Unity's keyword stripping.",
                ShaderOSEditorStyles.TinyLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawLegend()
        {
            EditorGUILayout.BeginHorizontal();
            DrawLegendDot(ShaderOSEditorStyles.ColorHealthy, "Low (< 1K)");
            GUILayout.Space(8);
            DrawLegendDot(ShaderOSEditorStyles.ColorAccent,   "Moderate (1K–5K)");
            GUILayout.Space(8);
            DrawLegendDot(ShaderOSEditorStyles.ColorWarning,  "High (5K–10K)");
            GUILayout.Space(8);
            DrawLegendDot(ShaderOSEditorStyles.ColorCritical, "Critical (> 10K)");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawLegendDot(Color color, string label)
        {
            OverviewPanel.DrawColorLabel("●", color, EditorStyles.miniLabel, GUILayout.Width(14));
            GUILayout.Label(label, EditorStyles.miniLabel);
        }

        private void DrawShaderTable(VariantSummary summary)
        {
            if (summary.ShaderEstimates.Count == 0)
            {
                EditorGUILayout.LabelField("No shaders with multiple variants found.", ShaderOSEditorStyles.TinyLabel);
                return;
            }

            EditorGUILayout.LabelField($"Shaders by variant count  ({summary.ShaderEstimates.Count})",
                ShaderOSEditorStyles.SectionHeader);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Shader",            EditorStyles.miniLabel, GUILayout.Width(220));
            GUILayout.Label("Est. (pre-strip)",  EditorStyles.miniLabel, GUILayout.Width(110));
            GUILayout.Label("Risk",              EditorStyles.miniLabel, GUILayout.Width(70));
            GUILayout.Label("Strip candidates",  EditorStyles.miniLabel, GUILayout.Width(110));
            GUILayout.Label("Relative scale",    EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // Normalize bars against the highest estimate in the list — not a fixed cap.
            // This makes relative scale meaningful instead of all bars being max width.
            int maxEstimate = 1;
            foreach (var e in summary.ShaderEstimates)
                if (e.EstimatedCount > maxEstimate) maxEstimate = e.EstimatedCount;

            foreach (var estimate in summary.ShaderEstimates)
            {
                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(Truncate(estimate.ShaderName, 32), EditorStyles.miniLabel, GUILayout.Width(220));

                var riskColor = RiskColor(estimate.RiskLevel);
                var prevC     = GUI.contentColor;
                GUI.contentColor = riskColor;
                // Append "(pre-strip)" suffix at row level too, reinforcing the estimation nature
                GUILayout.Label(OverviewPanel.FormatCount(estimate.EstimatedCount), EditorStyles.miniLabel, GUILayout.Width(110));
                GUILayout.Label(estimate.RiskLevel.ToString(), EditorStyles.miniLabel, GUILayout.Width(70));
                GUI.contentColor = prevC;

                GUILayout.Label(
                    estimate.StripCandidateKeywordCount > 0
                        ? $"~{estimate.StripCandidateKeywordCount} keywords"
                        : "—",
                    EditorStyles.miniLabel, GUILayout.Width(110));

                var barRect = EditorGUILayout.GetControlRect(GUILayout.Height(10));
                float frac  = (float)estimate.EstimatedCount / maxEstimate;
                OverviewPanel.DrawBarRect(barRect, frac, riskColor);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawProUpsell()
        {
            EditorGUILayout.BeginVertical(ShaderOSEditorStyles.IssueRow);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("🔒", GUILayout.Width(16));
            EditorGUILayout.LabelField("Variant Intelligence — not available in the free edition", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Variant Explosion Predictor — model keyword additions before committing.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Automated stripping recommendations — reduce thousands of variants safely.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("ShaderVariantCollection generator — warm up only the variants you use.", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Per-build regression detection — catch variant explosions before they ship.", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Learn More →", GUILayout.Width(110)))
                Application.OpenURL("https://discord.gg/dXny3f4dcG");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static Color RiskColor(VariantRiskLevel level)
        {
            switch (level)
            {
                case VariantRiskLevel.Critical: return ShaderOSEditorStyles.ColorCritical;
                case VariantRiskLevel.High:     return ShaderOSEditorStyles.ColorWarning;
                case VariantRiskLevel.Moderate: return ShaderOSEditorStyles.ColorAccent;
                default:                        return ShaderOSEditorStyles.ColorHealthy;
            }
        }

        private static string RiskLabel(VariantRiskLevel level)
        {
            switch (level)
            {
                case VariantRiskLevel.Critical: return "Critical risk";
                case VariantRiskLevel.High:     return "High risk";
                case VariantRiskLevel.Moderate: return "Moderate";
                default:                        return "Low risk";
            }
        }

        private static string Truncate(string s, int maxLen)
            => s != null && s.Length > maxLen ? s.Substring(0, maxLen - 1) + "…" : s ?? string.Empty;
    }
}

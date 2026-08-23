using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ToolsStudio.ShaderOS.Audit;
using ToolsStudio.ShaderOS.Keywords;
using ToolsStudio.ShaderOS.Editor.Styles;

namespace ToolsStudio.ShaderOS.Editor.Windows.Panels
{
    internal sealed class KeywordsPanel
    {
        // Column widths — single source of truth. Shader column uses ExpandWidth to absorb window resize.
        private const float COL_GLOBAL = 50f;
        private const float COL_LOCAL  = 50f;
        private const float COL_DUPS   = 46f;

        private static GUIStyle _headerLabelRight;
        private static GUIStyle _cellRight;
        private static GUIStyle _cellRightBold;
        private static GUIStyle _headerRow;

        private static void EnsureStyles()
        {
            if (_headerLabelRight != null) return;
            _headerLabelRight = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
            _cellRight         = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
            _cellRightBold     = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleRight };

            // Explicit padding, matching IssueRow's own explicit override exactly (both start from
            // 6,6 regardless of what either base style's own inherited padding happens to be) —
            // this is what actually guarantees the header lines up with the row values beneath it.
            _headerRow = new GUIStyle(EditorStyles.toolbar) { padding = new RectOffset(6, 6, 3, 3) };
        }

        private string _search = string.Empty;

        private KeywordBudgetReport                     _lastBudget;
        private List<ShaderKeywordContribution>          _filteredShaders;
        private Vector2                                  _scroll;

        public void Draw(AuditReport report)
        {
            EnsureStyles();
            var budget = report.KeywordBudget;
            if (_lastBudget != budget) { _lastBudget = budget; RebuildFilteredList(); }

            EditorGUILayout.Space(4);

            DrawGlobalBudgetBar(budget);
            EditorGUILayout.Space(5);
            DrawPerShaderBreakdown(budget);
        }

        // ------------------------------------------------------------------
        // Global budget — the one project-wide indicator against Unity's
        // hard 256-keyword limit. Kept as a bar: it's the single glanceable
        // "how close to the edge am I" signal and nothing else on this tab
        // duplicates it.
        // ------------------------------------------------------------------
        private static void DrawGlobalBudgetBar(KeywordBudgetReport budget)
        {
            EditorGUILayout.LabelField("Global Keyword Budget", ShaderOSEditorStyles.SectionHeader);

            var col = budget.GlobalBudgetLevel == KeywordBudgetLevel.Critical ? ShaderOSEditorStyles.ColorCritical
                    : budget.GlobalBudgetLevel >= KeywordBudgetLevel.Caution  ? ShaderOSEditorStyles.ColorWarning
                                                                              : ShaderOSEditorStyles.ColorHealthy;

            var barRect = EditorGUILayout.GetControlRect(GUILayout.Height(14));
            OverviewPanel.DrawBarRect(barRect, budget.GlobalUsagePercent / 100f, col);

            DrawThresholdMarker(barRect, 0.75f);
            DrawThresholdMarker(barRect, 0.90f);

            EditorGUILayout.BeginHorizontal();
            var prevC = GUI.contentColor;
            GUI.contentColor = col;
            EditorGUILayout.LabelField(
                $"{budget.GlobalKeywordCount} / {KeywordBudgetReport.GLOBAL_KEYWORD_LIMIT} used  ({budget.GlobalUsagePercent:F0}%)",
                EditorStyles.boldLabel);
            GUI.contentColor = prevC;
            EditorGUILayout.LabelField(budget.GlobalBudgetLevel.ToString(),
                ShaderOSEditorStyles.TinyLabel, GUILayout.Width(55));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawThresholdMarker(Rect barRect, float t)
        {
            float x = barRect.x + barRect.width * t;
            EditorGUI.DrawRect(new Rect(x - 1, barRect.y, 2, barRect.height),
                new Color(1f, 1f, 1f, 0.35f));
        }

        // ------------------------------------------------------------------
        // Per-shader breakdown — the large-data region. Search filters the
        // list without touching analysis data; the scroll view expands to
        // fill whatever space the window actually has (see ShaderOSWindow,
        // which does not wrap this tab in its own outer scroll view for
        // exactly this reason — a scroll view with no height constraint
        // nested inside another scroll view never actually engages).
        // ------------------------------------------------------------------
        private void DrawPerShaderBreakdown(KeywordBudgetReport budget)
        {
            EditorGUILayout.LabelField("Per-Shader Breakdown", ShaderOSEditorStyles.SectionHeader);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.Width(160));
            if (EditorGUI.EndChangeCheck()) RebuildFilteredList();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(_headerRow);
            GUILayout.Label("Shader", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            GUILayout.Label("Global", _headerLabelRight, GUILayout.Width(COL_GLOBAL));
            GUILayout.Label("Local",  _headerLabelRight, GUILayout.Width(COL_LOCAL));
            GUILayout.Label("Dups",   _headerLabelRight, GUILayout.Width(COL_DUPS));
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            foreach (var contrib in _filteredShaders) DrawShaderRow(contrib);
            EditorGUILayout.EndScrollView();

            int total = budget.ShaderContributions.Count;
            string countLabel = string.IsNullOrEmpty(_search)
                ? $"{total} shaders"
                : $"{_filteredShaders.Count} of {total} shaders";
            EditorGUILayout.LabelField(countLabel, ShaderOSEditorStyles.TinyLabel);
        }

        private static void DrawShaderRow(ShaderKeywordContribution contrib)
        {
            EditorGUILayout.BeginHorizontal(ShaderOSEditorStyles.IssueRow);

            GUILayout.Label(Truncate(contrib.ShaderName, 60), EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            GUILayout.Label(contrib.UniqueGlobalKeywords.Count.ToString(), _cellRight, GUILayout.Width(COL_GLOBAL));

            // Local count — highlight red if over the per-shader budget. This color coding is the
            // decision-relevant signal; a relative bar next to numbers that are already exact was
            // redundant and cost an extra row of vertical space per shader for no added clarity.
            bool overBudget = contrib.LocalKeywordCount > KeywordBudgetReport.LOCAL_KEYWORD_PER_SHADER;
            if (overBudget)
            {
                var prevC = GUI.contentColor;
                GUI.contentColor = ShaderOSEditorStyles.ColorCritical;
                GUILayout.Label(contrib.LocalKeywordCount.ToString(), _cellRightBold, GUILayout.Width(COL_LOCAL));
                GUI.contentColor = prevC;
            }
            else
            {
                GUILayout.Label(contrib.LocalKeywordCount.ToString(), _cellRight, GUILayout.Width(COL_LOCAL));
            }

            if (contrib.DuplicateGlobalKeywords.Count > 0)
            {
                var prevC = GUI.contentColor;
                GUI.contentColor = ShaderOSEditorStyles.ColorWarning;
                GUILayout.Label(contrib.DuplicateGlobalKeywords.Count.ToString(), _cellRight, GUILayout.Width(COL_DUPS));
                GUI.contentColor = prevC;
            }
            else
            {
                GUILayout.Label("0", _cellRight, GUILayout.Width(COL_DUPS));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RebuildFilteredList()
        {
            if (_lastBudget == null) { _filteredShaders = new List<ShaderKeywordContribution>(); return; }

            _filteredShaders = new List<ShaderKeywordContribution>(_lastBudget.ShaderContributions.Count);
            string searchLower = _search?.ToLowerInvariant() ?? string.Empty;

            foreach (var c in _lastBudget.ShaderContributions)
            {
                if (!string.IsNullOrEmpty(searchLower) && !c.ShaderName.ToLowerInvariant().Contains(searchLower)) continue;
                _filteredShaders.Add(c);
            }

            // Heaviest local-keyword contributors first — surfaces the shaders most worth
            // investigating without requiring an interactive sort control.
            _filteredShaders.Sort((a, b) => b.LocalKeywordCount.CompareTo(a.LocalKeywordCount));
        }

        private static string Truncate(string s, int maxLen)
            => s != null && s.Length > maxLen ? s.Substring(0, maxLen - 1) + "…" : s ?? string.Empty;
    }
}

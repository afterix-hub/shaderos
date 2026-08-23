using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ToolsStudio.ShaderOS.Audit;
using ToolsStudio.ShaderOS.Compatibility;
using ToolsStudio.ShaderOS.Editor.Styles;

namespace ToolsStudio.ShaderOS.Editor.Windows.Panels
{
    internal sealed class MaterialsPanel
    {
        // Column widths — single source of truth. Name col uses ExpandWidth to absorb window resize.
        private const float COL_HEALTH   = 40f;
        private const float COL_SHADER   = 150f;
        private const float COL_PIPELINE = 60f;
        private const float COL_SRP      = 70f;
        private const float COL_ISSUES   = 42f;
        private const float COL_PING     = 24f;

        private enum FilterMode { All, Broken, Warnings, SRPIssues }
        private enum SortColumn { Health, Name, Pipeline, SRPBatcher }
        private enum SortDir    { Asc, Desc }

        private FilterMode _filter  = FilterMode.All;
        private SortColumn _sortCol = SortColumn.Health;
        private SortDir    _sortDir = SortDir.Desc;
        private string     _search  = string.Empty;

        private readonly HashSet<string>      _expandedGuids = new HashSet<string>();
        private List<MaterialRecord>          _filteredList;
        private AuditReport                   _lastReport;
        private Vector2                       _scroll;

        public void Draw(AuditReport report)
        {
            if (_lastReport != report) { _lastReport = report; RebuildList(); }

            DrawFilters();
            DrawColumnHeaders();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var mat in _filteredList) DrawMaterialRow(mat);
            EditorGUILayout.EndScrollView();

            // Footer count with denominator — clarifies what the number means.
            int total = _lastReport?.AnalyzedMaterialCount ?? 0;
            string countLabel = _filter == FilterMode.All && string.IsNullOrEmpty(_search)
                ? $"{_filteredList.Count} materials"
                : $"{_filteredList.Count} of {total} materials";
            EditorGUILayout.LabelField(countLabel, ShaderOSEditorStyles.TinyLabel);
        }

        private void DrawFilters()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.Width(130));
            if (EditorGUI.EndChangeCheck()) RebuildList();

            DrawFilterButton(FilterMode.All,       "All");
            DrawFilterButton(FilterMode.Broken,    "Broken");
            DrawFilterButton(FilterMode.Warnings,  "Warnings");
            DrawFilterButton(FilterMode.SRPIssues, "SRP Issues");

            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            bool asc = GUILayout.Toggle(_sortDir == SortDir.Asc, "↑", EditorStyles.toolbarButton, GUILayout.Width(22));
            if (EditorGUI.EndChangeCheck()) { _sortDir = asc ? SortDir.Asc : SortDir.Desc; RebuildList(); }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilterButton(FilterMode mode, string label)
        {
            bool active  = _filter == mode;
            bool clicked = GUILayout.Toggle(active, label, EditorStyles.toolbarButton);
            if (clicked && !active) { _filter = mode; RebuildList(); }
        }

        private static void DrawColumnHeaders()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Health",        EditorStyles.miniLabel, GUILayout.Width(COL_HEALTH));
            GUILayout.Label("Material Name", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            GUILayout.Label("Shader",        EditorStyles.miniLabel, GUILayout.Width(COL_SHADER));
            GUILayout.Label("Pipeline",      EditorStyles.miniLabel, GUILayout.Width(COL_PIPELINE));
            GUILayout.Label("SRP Batcher",   EditorStyles.miniLabel, GUILayout.Width(COL_SRP));
            GUILayout.Label("Issues",        EditorStyles.miniLabel, GUILayout.Width(COL_ISSUES));
            GUILayout.Label(string.Empty,                            GUILayout.Width(COL_PING));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMaterialRow(MaterialRecord mat)
        {
            bool expanded = _expandedGuids.Contains(mat.Guid);

            EditorGUILayout.BeginVertical(ShaderOSEditorStyles.IssueRow);
            EditorGUILayout.BeginHorizontal();

            // Health badge
            var col = ShaderOSEditorStyles.GetHealthColor(mat.HealthScore.Value);
            var prev = GUI.color;
            GUI.color = col;
            EditorGUILayout.LabelField(mat.HealthScore.Value.ToString("D3"), EditorStyles.boldLabel,
                GUILayout.Width(COL_HEALTH));
            GUI.color = prev;

            // Name — ExpandWidth so the row absorbs window resize instead of clipping shader column.
            // Foldout does NOT get a Width constraint here; the column itself is ExpandWidth.
            bool expand = EditorGUILayout.Foldout(expanded, mat.Name, true, EditorStyles.foldout);
            if (expand != expanded)
            {
                if (expand) _expandedGuids.Add(mat.Guid);
                else        _expandedGuids.Remove(mat.Guid);
            }

            // Fixed-width right side — these never change width regardless of window size
            string shaderDisplay = mat.HasBrokenShaderReference ? "⚠  BROKEN" : Truncate(mat.ShaderName, 22);
            var shaderStyle = mat.HasBrokenShaderReference ? ShaderOSEditorStyles.ProLockLabel : EditorStyles.miniLabel;
            GUILayout.Label(shaderDisplay, shaderStyle, GUILayout.Width(COL_SHADER));

            GUILayout.Label(mat.PipelineTag, EditorStyles.miniLabel, GUILayout.Width(COL_PIPELINE));

            DrawSRPBadge(mat.SRPBatcherCompatibility);

            GUILayout.Label(mat.Issues.Count > 0 ? mat.Issues.Count.ToString() : "—",
                EditorStyles.miniLabel, GUILayout.Width(COL_ISSUES));

            if (GUILayout.Button("◎", EditorStyles.miniLabel, GUILayout.Width(COL_PING)))
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(mat.AssetPath);
                if (asset != null) EditorGUIUtility.PingObject(asset);
            }

            EditorGUILayout.EndHorizontal();

            if (expanded) DrawMaterialDetail(mat);

            EditorGUILayout.EndVertical();
        }

        private static void DrawSRPBadge(SRPBatcherState state)
        {
            switch (state)
            {
                case SRPBatcherState.Compatible:
                    var prevC = GUI.contentColor;
                    GUI.contentColor = ShaderOSEditorStyles.ColorHealthy;
                    GUILayout.Label("✓ OK", EditorStyles.miniLabel, GUILayout.Width(COL_SRP));
                    GUI.contentColor = prevC;
                    break;
                case SRPBatcherState.Incompatible:
                    var prevI = GUI.contentColor;
                    GUI.contentColor = ShaderOSEditorStyles.ColorWarning;
                    GUILayout.Label("✗ Fail", EditorStyles.miniLabel, GUILayout.Width(COL_SRP));
                    GUI.contentColor = prevI;
                    break;
                default:
                    GUILayout.Label("—", EditorStyles.miniLabel, GUILayout.Width(COL_SRP));
                    break;
            }
        }

        private static void DrawMaterialDetail(MaterialRecord mat)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(mat.AssetPath, ShaderOSEditorStyles.TinyLabel);
            foreach (var issue in mat.Issues)
            {
                string icon = issue.Severity == IssueSeverity.Error ? "⚠" : "ℹ";
                EditorGUILayout.LabelField($"  {icon}  {issue.Description}", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField($"      → {issue.Resolution}", ShaderOSEditorStyles.TinyLabel);
            }
            EditorGUILayout.EndVertical();
        }

        private void RebuildList()
        {
            if (_lastReport == null) { _filteredList = new List<MaterialRecord>(); return; }

            string searchLower = _search?.ToLowerInvariant() ?? string.Empty;
            _filteredList = new List<MaterialRecord>(_lastReport.Materials.Count);

            foreach (var m in _lastReport.Materials)
            {
                if (!PassesFilter(m)) continue;
                if (!string.IsNullOrEmpty(searchLower) &&
                    !m.Name.ToLowerInvariant().Contains(searchLower) &&
                    !m.ShaderName.ToLowerInvariant().Contains(searchLower)) continue;
                _filteredList.Add(m);
            }

            _filteredList.Sort(GetComparer());
        }

        private bool PassesFilter(MaterialRecord m)
        {
            switch (_filter)
            {
                case FilterMode.Broken:    return m.HasBrokenShaderReference;
                case FilterMode.Warnings:  return m.Issues.Count > 0;
                case FilterMode.SRPIssues: return m.SRPBatcherCompatibility == SRPBatcherState.Incompatible;
                default:                   return true;
            }
        }

        private Comparison<MaterialRecord> GetComparer()
        {
            bool desc = _sortDir == SortDir.Desc;
            switch (_sortCol)
            {
                case SortColumn.Name:       return (a, b) => desc
                    ? string.Compare(b.Name, a.Name, StringComparison.OrdinalIgnoreCase)
                    : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                case SortColumn.Pipeline:   return (a, b) => desc
                    ? string.Compare(b.PipelineTag, a.PipelineTag, StringComparison.Ordinal)
                    : string.Compare(a.PipelineTag, b.PipelineTag, StringComparison.Ordinal);
                case SortColumn.SRPBatcher: return (a, b) => desc
                    ? b.SRPBatcherCompatibility.CompareTo(a.SRPBatcherCompatibility)
                    : a.SRPBatcherCompatibility.CompareTo(b.SRPBatcherCompatibility);
                default:                    return (a, b) => desc
                    ? b.HealthScore.CompareTo(a.HealthScore)
                    : a.HealthScore.CompareTo(b.HealthScore);
            }
        }

        private static string Truncate(string s, int maxLen)
            => s != null && s.Length > maxLen ? s.Substring(0, maxLen - 1) + "…" : s ?? string.Empty;
    }
}

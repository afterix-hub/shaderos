using UnityEditor;
using UnityEngine;
using ToolsStudio.ShaderOS.Audit;
using ToolsStudio.ShaderOS.Interfaces;
using ToolsStudio.ShaderOS.Editor.Analysis;
using ToolsStudio.ShaderOS.Editor.Session;
using ToolsStudio.ShaderOS.Editor.Settings;
using ToolsStudio.ShaderOS.Editor.Styles;
using ToolsStudio.ShaderOS.Editor.Windows.Panels;

namespace ToolsStudio.ShaderOS.Editor.Windows
{
    internal sealed class ShaderOSWindow : EditorWindow
    {
        private const string WINDOW_TITLE = "ShaderOS";
        private const string DOCS_URL     = "https://github.com/tools-studio/shaderos-docs";
        private const string VERSION      = "1.0.0";

        private enum Tab { Overview = 0, Materials = 1, Keywords = 2, Variants = 3, About = 4 }

        [SerializeField] private Tab     _activeTab;
        [SerializeField] private Vector2 _scrollPosition;

        private OverviewPanel  _overviewPanel;
        private MaterialsPanel _materialsPanel;
        private KeywordsPanel  _keywordsPanel;
        private VariantsPanel  _variantsPanel;
        private AboutPanel     _aboutPanel;

        [MenuItem("Tools/ShaderOS", priority = 1000)]
        public static void Open()
        {
            var w = GetWindow<ShaderOSWindow>(WINDOW_TITLE);
            w.minSize = new Vector2(520f, 400f);
            w.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WINDOW_TITLE);
            minSize      = new Vector2(520f, 400f);

            _overviewPanel  = new OverviewPanel();
            _materialsPanel = new MaterialsPanel();
            _keywordsPanel  = new KeywordsPanel();
            _variantsPanel  = new VariantsPanel();
            _aboutPanel     = new AboutPanel();

            ShaderOSEditorEvents.ReportUpdated += OnReportUpdated;
            ShaderOSEditorEvents.ScanCancelled += OnScanCancelled;
        }

        private void OnDisable()
        {
            ShaderOSEditorEvents.ReportUpdated -= OnReportUpdated;
            ShaderOSEditorEvents.ScanCancelled -= OnScanCancelled;
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawTabBar();

            if (_activeTab == Tab.Keywords)
            {
                // A scroll view with no height constraint never scrolls inside another one,
                // so Keywords (which manages its own scroll region) is left unwrapped here.
                DrawActivePanel();
            }
            else
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                DrawActivePanel();
                EditorGUILayout.EndScrollView();
            }

            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label(WINDOW_TITLE, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("?", EditorStyles.toolbarButton, GUILayout.Width(24)))
                Application.OpenURL(DOCS_URL);
            if (GUILayout.Button("⚙", EditorStyles.toolbarButton, GUILayout.Width(24)))
                SettingsService.OpenProjectSettings("Project/Tools Studio/ShaderOS");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawTabButton(Tab.Overview,  "Overview");
            DrawTabButton(Tab.Materials, "Materials");
            DrawTabButton(Tab.Keywords,  "Keywords");
            DrawTabButton(Tab.Variants,  "Variants");
            DrawTabButton(Tab.About,     "About");
            GUILayout.FlexibleSpace();

            var session = ShaderOSSession.Instance;

            EditorGUI.BeginDisabledGroup(session.IsScanRunning);

            var demoContent = new GUIContent("Load Demo Data",
                "Shows example ShaderOS output across every tab. Not from your project.");
            if (GUILayout.Button(demoContent, EditorStyles.toolbarButton, GUILayout.Width(104)))
            {
                session.EnablePreview();
                Repaint();
            }

            if (GUILayout.Button(session.IsScanRunning ? "Scanning…" : "▶  Scan Project",
                EditorStyles.toolbarButton, GUILayout.Width(104)))
                RunScan();

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabButton(Tab tab, string label)
        {
            bool active  = _activeTab == tab;
            bool clicked = GUILayout.Toggle(active, label, EditorStyles.toolbarButton, GUILayout.Width(74));
            if (clicked && !active)
            {
                _activeTab      = tab;
                _scrollPosition = Vector2.zero;
            }
        }

        private void DrawActivePanel()
        {
            // About is product identity, not scan output — it must be readable before a scan
            // has ever run, so it's checked before the "no report yet" gate applies to everything else.
            if (_activeTab == Tab.About) { _aboutPanel.Draw(VERSION); return; }

            var session = ShaderOSSession.Instance;
            var report  = session.DisplayedReport;
            if (report == null) { DrawEmptyState(); return; }

            if (session.IsShowingPreview) DrawDemoIndicator();

            switch (_activeTab)
            {
                case Tab.Overview:  _overviewPanel .Draw(report, this); break;
                case Tab.Materials: _materialsPanel.Draw(report, this); break;
                case Tab.Keywords:  _keywordsPanel .Draw(report); break;
                case Tab.Variants:  _variantsPanel .Draw(report); break;
            }
        }

        private static void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("No scan has been run yet.", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
        }

        private static void DrawDemoIndicator()
        {
            EditorGUILayout.LabelField("Demo Data", ShaderOSEditorStyles.DemoDataLabel);
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();

            var session = ShaderOSSession.Instance;
            if (session.IsShowingPreview)
            {
                GUILayout.Label("Demo Data — not from your project", ShaderOSEditorStyles.TinyLabel);
                GUILayout.Space(12);
            }
            else if (session.HasReport)
            {
                var r = session.LastReport;
                string partial = r.IsPartial ? $" of {r.TotalMaterialsInProject} (free limit)" : string.Empty;
                GUILayout.Label(
                    $"Last scan: {r.Timestamp.ToLocalTime():HH:mm:ss}  ·  " +
                    $"{r.AnalyzedMaterialCount} materials{partial}  ·  " +
                    $"{r.ScanDuration.TotalSeconds:F1}s",
                    ShaderOSEditorStyles.TinyLabel);
                GUILayout.Space(12);
            }

            GUILayout.Label($"Tools Studio  ·  ShaderOS  ·  {VERSION}", ShaderOSEditorStyles.FooterLabel, GUILayout.Width(220));
            EditorGUILayout.EndHorizontal();
        }

        private void RunScan()
        {
            var session  = ShaderOSSession.Instance;
            var settings = ShaderOSProjectSettings.Instance;

            session.OnScanStarted();
            Repaint();

            try
            {
                var engine = new MaterialAuditEngine(isProTier: false);
                var report = engine.RunAudit(settings.DefaultScanScope, settings.DefaultScanFolder);
                session.OnScanCompleted(report);
            }
            catch (System.Exception ex)
            {
                ShaderOSLogger.Error(this, "Scan failed", ex);
                session.OnScanCancelled();
            }

            Repaint();
        }

        private void OnReportUpdated(AuditReport _) => Repaint();
        private void OnScanCancelled()              => Repaint();
    }
}

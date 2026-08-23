using UnityEditor;
using UnityEngine;

namespace ToolsStudio.ShaderOS.Editor.Styles
{
    internal static class ShaderOSEditorStyles
    {
        public static readonly Color ColorHealthy  = new Color(0.20f, 0.75f, 0.40f, 1f);
        public static readonly Color ColorWarning  = new Color(0.95f, 0.72f, 0.15f, 1f);
        public static readonly Color ColorCritical = new Color(0.92f, 0.25f, 0.30f, 1f);
        public static readonly Color ColorMuted    = new Color(0.55f, 0.55f, 0.55f, 1f);
        public static readonly Color ColorAccent   = new Color(0.05f, 0.75f, 0.90f, 1f);

        public const float SCORE_BAR_HEIGHT = 8f;

        private static GUIStyle _windowChrome;
        public static GUIStyle WindowChrome => _windowChrome ??= new GUIStyle(EditorStyles.helpBox)
        {
            margin  = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(8, 8, 6, 6)
        };

        // Mixed-case, same weight as body — not all-caps dashboard style.
        private static GUIStyle _sectionHeader;
        public static GUIStyle SectionHeader => _sectionHeader ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            margin   = new RectOffset(0, 0, 8, 3)
        };

        private static GUIStyle _scoreLabel;
        public static GUIStyle ScoreLabel => _scoreLabel ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 22,
            alignment = TextAnchor.MiddleCenter
        };

        private static GUIStyle _tinyLabel;
        public static GUIStyle TinyLabel => _tinyLabel ??= new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 9,
            normal   = { textColor = ColorMuted }
        };

        private static GUIStyle _issueRow;
        public static GUIStyle IssueRow => _issueRow ??= new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(6, 6, 3, 3),
            margin  = new RectOffset(0, 0, 1, 2)
        };

        private static GUIStyle _footerLabel;
        public static GUIStyle FooterLabel => _footerLabel ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight,
            normal    = { textColor = ColorMuted }
        };

        private static GUIStyle _proLockLabel;
        public static GUIStyle ProLockLabel => _proLockLabel ??= new GUIStyle(EditorStyles.miniLabel)
        {
            normal    = { textColor = ColorWarning },
            fontStyle = FontStyle.Italic
        };

        // Subtle, unmistakable marker shown wherever demo data is on screen instead of a real scan.
        private static GUIStyle _demoDataLabel;
        public static GUIStyle DemoDataLabel => _demoDataLabel ??= new GUIStyle(EditorStyles.miniLabel)
        {
            normal    = { textColor = ColorWarning },
            fontStyle = FontStyle.Italic
        };

        // Tile label — small caps-weight label for KPI card headings, lighter than SectionHeader.
        private static GUIStyle _tileHeader;
        public static GUIStyle TileHeader => _tileHeader ??= new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize  = 9,
            fontStyle = FontStyle.Normal,
            normal    = { textColor = ColorMuted }
        };

        public static Color GetHealthColor(int score)
        {
            if (score >= 80) return ColorHealthy;
            if (score >= 50) return ColorWarning;
            return ColorCritical;
        }

        public static void Reset()
        {
            _windowChrome  = null;
            _sectionHeader = null;
            _scoreLabel    = null;
            _tinyLabel     = null;
            _issueRow      = null;
            _footerLabel   = null;
            _proLockLabel  = null;
            _tileHeader    = null;
            _demoDataLabel = null;
        }
    }
}

using UnityEditor;
using UnityEngine;
using ToolsStudio.ShaderOS.Editor.Styles;

namespace ToolsStudio.ShaderOS.Editor.Windows.Panels
{
    // Takes no AuditReport by design — this panel must stay readable before a scan has ever run.
    internal sealed class AboutPanel
    {
        private const float MAX_TEXT_WIDTH = 620f;
        private const float SIDE_MARGIN    = 40f;

        private const float BUTTON_WIDTH  = 170f;
        private const float BUTTON_HEIGHT = 22f;
        private const float BUTTON_GAP    = 10f;

        private const float CARD_WIDTH  = 190f;
        private const float CARD_HEIGHT = 34f;

        private static GUIStyle _title;
        private static GUIStyle _tagline;
        private static GUIStyle _description;
        private static GUIStyle _centeredMuted;
        private static GUIStyle _sectionHeaderCentered;
        private static GUIStyle _button;
        private static GUIStyle _card;
        private static GUIStyle _cardTitle;
        private static GUIStyle _cardTagline;

        private static void EnsureStyles()
        {
            if (_title != null) return;

            _title = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 26,
                alignment = TextAnchor.MiddleCenter
            };
            _tagline = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            _description = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            _centeredMuted = new GUIStyle(ShaderOSEditorStyles.TinyLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            _sectionHeaderCentered = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            _button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                padding = new RectOffset(8, 8, 3, 3)
            };
            _card = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter
            };
            _cardTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            _cardTagline = new GUIStyle(ShaderOSEditorStyles.TinyLabel)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
        }

        public void Draw(string version)
        {
            EnsureStyles();
            float textWidth = Mathf.Min(EditorGUIUtility.currentViewWidth - SIDE_MARGIN, MAX_TEXT_WIDTH);

            EditorGUILayout.Space(24);

            EditorGUILayout.LabelField("ShaderOS", _title);
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Shader diagnostics and health monitoring for Unity.", _tagline);

            EditorGUILayout.Space(12);
            DrawCenteredWidth(textWidth, () => GUILayout.Label(
                "Analyze shaders, materials, keywords, variants, and dependencies to identify " +
                "rendering risks before production.",
                _description, GUILayout.Width(textWidth)));

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"Version {version}", _centeredMuted);

            EditorGUILayout.Space(20);
            DrawSupportButtons();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Free Edition  ·  Unity Asset Store EULA applies.", _centeredMuted);

            EditorGUILayout.Space(24);
            DrawEcosystemSection();
        }

        private static void DrawCenteredWidth(float width, System.Action content)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(GUILayout.Width(width));
            content();
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSupportButtons()
        {
            DrawButtonPair(
                ("Documentation", "https://github.com/tools-studio/shaderos-docs"),
                ("Discord", "https://discord.gg/dXny3f4dcG"));

            EditorGUILayout.Space(BUTTON_GAP);

            DrawButtonPair(
                ("Report Issue", "https://discord.gg/XPMGcdnpmn"),
                ("Feature Request", "https://discord.gg/Ge99xqt5qr"));

            EditorGUILayout.Space(BUTTON_GAP);

            DrawSingleButton("Email Support", BUTTON_WIDTH * 2 + BUTTON_GAP, () =>
                Application.OpenURL("mailto:support.toolsstudio@gmail.com"));
        }

        private static void DrawEcosystemSection()
        {
            EditorGUILayout.LabelField("Explore More Tools Studio Assets", _sectionHeaderCentered);
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Other Tools Studio tools for Unity production workflows.", _centeredMuted);

            EditorGUILayout.Space(12);

            DrawEcosystemCards(
                ("Runtime Atlas", "Server-authoritative runtime economy.", "https://assetstore.unity.com/packages/tools/utilities/runtime-atlas-367424"),
                ("Audio Atlas",   "Zero-GC audio architecture for Unity.", "https://assetstore.unity.com/packages/tools/utilities/audio-atlas-378112"),
                ("Build Lens",    "Build size and dependency analysis.",   "https://assetstore.unity.com/packages/tools/utilities/buildlens-378102"));
        }

        private static void DrawButtonPair((string label, string url) a, (string label, string url) b)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(a.label, _button, GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
                Application.OpenURL(a.url);

            GUILayout.Space(BUTTON_GAP);

            if (GUILayout.Button(b.label, _button, GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
                Application.OpenURL(b.url);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSingleButton(string label, float width, System.Action onClick)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(label, _button, GUILayout.Width(width), GUILayout.Height(BUTTON_HEIGHT)))
                onClick();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawEcosystemCards(
            (string name, string tagline, string url) a,
            (string name, string tagline, string url) b,
            (string name, string tagline, string url) c)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            DrawEcosystemCard(a);
            GUILayout.Space(BUTTON_GAP);
            DrawEcosystemCard(b);
            GUILayout.Space(BUTTON_GAP);
            DrawEcosystemCard(c);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawEcosystemCard((string name, string tagline, string url) product)
        {
            var rect = GUILayoutUtility.GetRect(CARD_WIDTH, CARD_HEIGHT, GUILayout.Width(CARD_WIDTH));

            if (GUI.Button(rect, GUIContent.none, _card))
                Application.OpenURL(product.url);

            var titleRect   = new Rect(rect.x + 4, rect.y + 2, rect.width - 8, 14);
            var taglineRect = new Rect(rect.x + 4, rect.y + 16, rect.width - 8, rect.height - 17);

            GUI.Label(titleRect, product.name, _cardTitle);
            GUI.Label(taglineRect, product.tagline, _cardTagline);
        }
    }
}

using UnityEditor;
using UnityEngine;
using ToolsStudio.ShaderOS.Audit;
using ToolsStudio.ShaderOS.Editor.Session;

namespace ToolsStudio.ShaderOS.Editor.Windows
{
    // Single source of truth for pinging a MaterialRecord's asset — every panel that renders
    // a material row goes through here, so demo-data guarding never drifts between panels.
    internal static class PingUtility
    {
        private const string DEMO_MESSAGE    = "Ping unavailable for demo data.";
        private const string NO_PATH_MESSAGE = "No asset path recorded for this material.";
        private const string MISSING_MESSAGE = "Asset not found — it may have moved or been deleted since the scan.";

        // Content shown on the button itself; callers apply this whether or not the button
        // ends up disabled, so the tooltip is correct in both states.
        public static GUIContent ButtonContent(string label) => new GUIContent(
            label,
            IsPingable ? "Select this asset in the Project window." : DEMO_MESSAGE);

        // Demo data is never pingable — nothing behind it exists on disk, real or otherwise.
        public static bool IsPingable => !ShaderOSSession.Instance.IsShowingPreview;

        public static void PingMaterial(MaterialRecord material, EditorWindow window)
        {
            if (!IsPingable)
            {
                window.ShowNotification(new GUIContent(DEMO_MESSAGE));
                return;
            }

            if (string.IsNullOrEmpty(material.AssetPath))
            {
                window.ShowNotification(new GUIContent(NO_PATH_MESSAGE));
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<Object>(material.AssetPath);
            if (asset == null)
            {
                window.ShowNotification(new GUIContent(MISSING_MESSAGE));
                return;
            }

            EditorGUIUtility.PingObject(asset);
        }
    }
}

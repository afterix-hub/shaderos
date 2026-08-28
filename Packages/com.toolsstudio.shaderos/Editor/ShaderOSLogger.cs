using System;
using UnityEngine;

namespace ToolsStudio.ShaderOS.Editor
{
    internal static class ShaderOSLogger
    {
        private const string PREFIX = "[TS] [ShaderOS]";

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Info(object context, string message, object payload = null)
            => Debug.Log(Format("INFO", context, message, payload));

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Warn(object context, string message, object payload = null)
            => Debug.LogWarning(Format("WARN", context, message, payload));

        public static void Error(object context, string message, object payload = null)
            => Debug.LogError(Format("ERROR", context, message, payload));

        public static void Error(object context, string message, Exception ex)
            => Debug.LogError(Format("ERROR", context, $"{message} — {ex.GetType().Name}: {ex.Message}", null));

        private static string Format(string level, object context, string message, object payload)
        {
            string tag        = context is string s ? s : context?.GetType().Name ?? "null";
            string payloadStr = payload != null ? $"  {FormatPayload(payload)}" : string.Empty;
            return $"{PREFIX}  [{level}]  [{tag}]  {message}.{payloadStr}";
        }

        private static string FormatPayload(object payload)
        {
            var sb    = new System.Text.StringBuilder("{");
            var props = payload.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(props[i].Name);
                sb.Append(": \"");
                sb.Append(props[i].GetValue(payload));
                sb.Append('"');
            }
            sb.Append('}');
            return sb.ToString();
        }
    }
}

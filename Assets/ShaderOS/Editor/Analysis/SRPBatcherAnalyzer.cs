using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using ToolsStudio.ShaderOS.Compatibility;

namespace ToolsStudio.ShaderOS.Editor.Analysis
{
    internal sealed class SRPBatcherAnalyzer
    {
        // ShaderUtil.IsPassSRPBatchCompatible was removed in Unity 6000.
        // We probe once at runtime and fall through to source-text analysis if absent.
        private static bool       _reflectionProbed;
        private static MethodInfo _passCompatMethod;

        public SRPBatcherAnalysisResult Analyze(string materialGuid, Material material)
        {
            if (material == null || material.shader == null)
                return new SRPBatcherAnalysisResult(materialGuid, SRPBatcherState.Unknown, null, 0);

            if (!IsSRPActive())
                return new SRPBatcherAnalysisResult(materialGuid, SRPBatcherState.NotApplicable, null, 0);

            var breakers = new List<SRPBatcherBreaker>();
            RunAssessment(material, breakers);

            var state = breakers.Count == 0 ? SRPBatcherState.Compatible : SRPBatcherState.Incompatible;
            return new SRPBatcherAnalysisResult(materialGuid, state, breakers.AsReadOnly(), breakers.Count > 0 ? 1 : 0);
        }

        private static void RunAssessment(Material material, List<SRPBatcherBreaker> breakers)
        {
            if (TryReflectionCheck(material, out bool compatible))
            {
                if (!compatible) breakers.Add(MakeCBufferBreaker());
                return;
            }

            if (TrySourceTextCheck(material, out bool sourceCompatible))
            {
                if (!sourceCompatible) breakers.Add(MakeSourceBreaker());
                return;
            }
            // Neither layer could assess — emit no breakers (Unknown, no false positive)
        }

        private static bool TryReflectionCheck(Material material, out bool isCompatible)
        {
            isCompatible = false;
            if (!_reflectionProbed)
            {
                var t = typeof(ShaderUtil);
                _passCompatMethod =
                    t.GetMethod("IsPassSRPBatchCompatible", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Shader), typeof(int) }, null)
                    ?? t.GetMethod("IsPassSRPBatchCompatible", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Shader) }, null);
                _reflectionProbed = true;
            }

            if (_passCompatMethod == null) return false;

            try
            {
                object result = _passCompatMethod.GetParameters().Length == 2
                    ? _passCompatMethod.Invoke(null, new object[] { material.shader, 0 })
                    : _passCompatMethod.Invoke(null, new object[] { material.shader });
                isCompatible = result is bool b && b;
                return true;
            }
            catch { _passCompatMethod = null; return false; }
        }

        private static bool TrySourceTextCheck(Material material, out bool isCompatible)
        {
            isCompatible = false;
            string assetPath = AssetDatabase.GetAssetPath(material.shader);
            if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
                return false;

            string fullPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), assetPath);
            if (!System.IO.File.Exists(fullPath)) return false;

            string source = System.IO.File.ReadAllText(fullPath);
            isCompatible  = source.Contains("CBUFFER_START(UnityPerMaterial)") || source.Contains("UnityPerMaterial");
            return true;
        }

        private static bool IsSRPActive() => GraphicsSettings.currentRenderPipeline != null;

        private static SRPBatcherBreaker MakeCBufferBreaker() => new SRPBatcherBreaker(
            "Shader is not SRP Batcher compatible. Declare all per-material properties inside CBUFFER_START(UnityPerMaterial).",
            drawCallsAffected: 1, hasAutoFix: false);

        private static SRPBatcherBreaker MakeSourceBreaker() => new SRPBatcherBreaker(
            "Shader source has no 'UnityPerMaterial' CBUFFER. Add CBUFFER_START(UnityPerMaterial) ... CBUFFER_END around per-material properties.",
            drawCallsAffected: 1, hasAutoFix: false);
    }
}

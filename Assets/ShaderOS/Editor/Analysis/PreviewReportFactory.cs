using System;
using System.Collections.Generic;
using ToolsStudio.ShaderOS.Audit;
using ToolsStudio.ShaderOS.Compatibility;
using ToolsStudio.ShaderOS.Dependency;

namespace ToolsStudio.ShaderOS.Editor.Analysis
{
    // Builds an AuditReport from curated data instead of a real scan, reusing the same models
    // and keyword/variant analyzers MaterialAuditEngine uses — no separate demo architecture.
    internal static class PreviewReportFactory
    {
        private const int VARIANT_CAP = 2_000_000;

        public static AuditReport Build()
        {
            var shaders   = BuildShaders();
            var materials = BuildMaterials(shaders);
            var graph     = BuildDependencyGraph(materials, shaders);

            var keywordAnalyzer = new ShaderKeywordAnalyzer();
            var variantAnalyzer = new ShaderVariantAnalyzer();
            var keywordBudget   = keywordAnalyzer.BuildBudgetReport(shaders.AsReadOnly());
            var variantSummary  = variantAnalyzer.BuildSummary(shaders.AsReadOnly());

            return new AuditReport(
                DateTime.UtcNow, TimeSpan.FromSeconds(1.8), ScanScope.FullProject, string.Empty,
                materials.AsReadOnly(), shaders.AsReadOnly(),
                graph, keywordBudget, variantSummary,
                isPartial: false, totalMaterialsInProject: materials.Count);
        }

        private static List<ShaderRecord> BuildShaders()
        {
            var urpLit = BuildShader(
                "demo-shd-urp-lit", "Universal Render Pipeline/Lit", string.Empty, isBuiltIn: true,
                global: new[] { "_NORMALMAP", "_METALLICSPECGLOSSMAP", "_RECEIVE_SHADOWS_OFF" },
                local: new[] { "_SURFACE_TYPE_TRANSPARENT", "_ALPHATEST_ON", "_EMISSION", "_SPECULAR_SETUP",
                    "_OCCLUSIONMAP", "_PARALLAXMAP", "_DETAIL_MULX2", "_CLEARCOAT" },
                pipeline: "URP");

            var urpSimpleLit = BuildShader(
                "demo-shd-urp-simplelit", "Universal Render Pipeline/Simple Lit", string.Empty, isBuiltIn: true,
                global: new[] { "_NORMALMAP" },
                local: new[] { "_ALPHATEST_ON", "_EMISSION", "_SPECULAR_HIGHLIGHTS_OFF" },
                pipeline: "URP");

            var characterSkin = BuildShader(
                "demo-shd-char-skin", "Shader Graphs/Character_Skin_SG",
                "Assets/Art/Shaders/Character_Skin_SG.shadergraph", isBuiltIn: false,
                global: new[] { "_NORMALMAP", "_SUBSURFACE_SCATTERING" },
                local: new[] { "_USE_TRIPLANAR", "_DETAIL_BLEND" },
                pipeline: "Graph");

            var waterSurface = BuildShader(
                "demo-shd-water", "Shader Graphs/Water_Surface_SG",
                "Assets/Art/Shaders/Water_Surface_SG.shadergraph", isBuiltIn: false,
                global: new[] { "_NORMALMAP", "_RECEIVE_SHADOWS_OFF" },
                local: new[] { "_FOAM_ON", "_CAUSTICS_ON", "_REFLECTION_PROBE_BLENDING" },
                pipeline: "Graph");

            var foliageWind = BuildShader(
                "demo-shd-foliage-wind", "Shader Graphs/Foliage_Wind_SG",
                "Assets/Art/Shaders/Foliage_Wind_SG.shadergraph", isBuiltIn: false,
                global: new[] { "_NORMALMAP", "_WIND_ENABLED" },
                local: BuildFoliageLocalKeywords(),
                pipeline: "Graph");

            var standard = BuildShader(
                "demo-shd-standard", "Standard", string.Empty, isBuiltIn: true,
                global: Array.Empty<string>(),
                local: new[] { "_NORMALMAP", "_METALLICGLOSSMAP", "_EMISSION", "_DETAIL_MULX2", "_PARALLAXMAP" },
                pipeline: "BiRP");

            return new List<ShaderRecord>
            {
                urpLit, urpSimpleLit, characterSkin, waterSurface, foliageWind, standard
            };
        }

        // A foliage/terrain-blend shader is the realistic case for blowing past the local budget —
        // one toggle per terrain layer adds up fast. Fifteen named toggles plus one per layer.
        private static IReadOnlyList<string> BuildFoliageLocalKeywords()
        {
            var keywords = new List<string>(71)
            {
                "_WIND_QUALITY_LOW", "_WIND_QUALITY_MEDIUM", "_WIND_QUALITY_HIGH",
                "_SNOW_LAYER_ON", "_WETNESS_ON", "_COLOR_VARIATION_ON",
                "_LOD_FADE_CROSSFADE", "_BILLBOARD_FACE_CAMERA_POS", "_INSTANCING_ON",
                "_DOUBLE_SIDED_ON", "_TRANSLUCENCY_ON", "_VERTEX_COLOR_BLEND",
                "_AO_FROM_TEXTURE", "_SEASONAL_TINT_ON", "_SUBSURFACE_APPROX"
            };
            for (int i = 0; i < 56; i++)
                keywords.Add($"_TERRAIN_LAYER_{i:D2}");
            return keywords;
        }

        private static ShaderRecord BuildShader(
            string guid, string name, string assetPath, bool isBuiltIn,
            IReadOnlyList<string> global, IReadOnlyList<string> local, string pipeline)
        {
            return new ShaderRecord(
                guid, name, assetPath, isBuiltIn,
                0, global.Count, local.Count, EstimateVariantCount(global.Count, local.Count),
                global, local, pipeline);
        }

        // Mirrors ShaderVariantAnalyzer.EstimateVariants' formula directly — that method requires
        // a live Shader reference only to null-check, which no demo shader has an equivalent for.
        private static int EstimateVariantCount(int globalCount, int localCount)
        {
            int dims = globalCount + localCount;
            if (dims <= 0) return 1;
            if (dims >= 21) return VARIANT_CAP;
            return Math.Min(1 << dims, VARIANT_CAP);
        }

        private static List<MaterialRecord> BuildMaterials(List<ShaderRecord> shaders)
        {
            var urpLit        = shaders[0];
            var urpSimpleLit  = shaders[1];
            var characterSkin = shaders[2];
            var waterSurface  = shaders[3];
            var foliageWind   = shaders[4];
            var standard      = shaders[5];

            var srpBreaker = new SRPBatcherBreaker(
                "Material overrides per-instance properties outside CBUFFER_START(UnityPerMaterial).",
                drawCallsAffected: 1, hasAutoFix: false);

            return new List<MaterialRecord>
            {
                BuildMaterial("demo-mat-char-skin", "Character_Skin_Mat",
                    "Assets/Materials/Character_Skin_Mat.mat", characterSkin, "Graph"),
                BuildMaterial("demo-mat-char-hair", "Character_Hair_Mat",
                    "Assets/Materials/Character_Hair_Mat.mat", urpLit, "URP"),
                BuildMaterial("demo-mat-char-eyes", "Character_Eyes_Mat",
                    "Assets/Materials/Character_Eyes_Mat.mat", urpSimpleLit, "URP"),
                BuildMaterial("demo-mat-rock-01", "Environment_Rock_01_Mat",
                    "Assets/Materials/Environment_Rock_01_Mat.mat", urpLit, "URP",
                    MaterialIssueLibrary.MissingTexture("_BumpMap")),
                BuildMaterial("demo-mat-cliff-blend", "Environment_Cliff_Blend_Mat",
                    "Assets/Materials/Environment_Cliff_Blend_Mat.mat", standard, "BiRP"),
                BuildMaterial("demo-mat-foliage-bush", "Foliage_Bush_Common_Mat",
                    "Assets/Materials/Foliage_Bush_Common_Mat.mat", foliageWind, "Graph"),
                BuildMaterial("demo-mat-foliage-tree", "Foliage_Tree_Canopy_Mat",
                    "Assets/Materials/Foliage_Tree_Canopy_Mat.mat", foliageWind, "Graph"),
                BuildMaterial("demo-mat-water-lake", "Water_Lake_Surface_Mat",
                    "Assets/Materials/Water_Lake_Surface_Mat.mat", waterSurface, "Graph",
                    MaterialIssueLibrary.SRPBatcherIncompatible(new[] { srpBreaker })),
                BuildMaterial("demo-mat-prop-barrel", "Prop_Barrel_Wood_Mat",
                    "Assets/Materials/Prop_Barrel_Wood_Mat.mat", urpLit, "URP"),
                BuildMaterial("demo-mat-prop-crate", "Prop_Crate_Metal_Mat",
                    "Assets/Materials/Prop_Crate_Metal_Mat.mat", urpLit, "URP",
                    MaterialIssueLibrary.MissingTexture("_MetallicGlossMap")),
                BuildMaterial("demo-mat-vfx-fire", "VFX_Fire_Additive_Mat",
                    "Assets/Materials/VFX_Fire_Additive_Mat.mat", urpSimpleLit, "URP"),
                BuildMaterial("demo-mat-ui-icon", "UI_Icon_Overlay_Mat",
                    "Assets/Materials/UI_Icon_Overlay_Mat.mat", standard, "BiRP"),
                BuildBrokenMaterial("demo-mat-debris", "Debris_Rubble_Broken_Mat",
                    "Assets/Materials/Debris_Rubble_Broken_Mat.mat")
            };
        }

        private static MaterialRecord BuildMaterial(
            string guid, string name, string assetPath, ShaderRecord shader, string pipeline,
            params MaterialIssue[] issues)
        {
            int penalty = 0;
            bool hasMissingTexture = false;
            var srpState = SRPBatcherState.Compatible;

            foreach (var issue in issues)
            {
                penalty += issue.HealthPenalty;
                if (issue.Category == IssueCategory.MissingTexture) hasMissingTexture = true;
                if (issue.Category == IssueCategory.SRPBatcherIncompatible) srpState = SRPBatcherState.Incompatible;
            }

            return new MaterialRecord(
                guid, name, assetPath, HealthScore.FromPenalties(penalty),
                false, hasMissingTexture,
                shader.Guid, shader.Name, srpState,
                issues, pipeline, isOrphaned: false);
        }

        private static MaterialRecord BuildBrokenMaterial(string guid, string name, string assetPath)
        {
            var issue = MaterialIssueLibrary.BrokenShaderReference();
            return new MaterialRecord(
                guid, name, assetPath, HealthScore.FromPenalties(issue.HealthPenalty),
                true, false,
                string.Empty, "—", SRPBatcherState.Unknown,
                new[] { issue }, "Unresolved", isOrphaned: false);
        }

        private static DependencyGraph BuildDependencyGraph(
            List<MaterialRecord> materials, List<ShaderRecord> shaders)
        {
            var builder = new DependencyGraph.Builder();

            foreach (var shader in shaders)
                builder.AddNode(new DependencyNode(shader.Guid, shader.Name, DependencyNodeKind.Shader, shader.AssetPath));

            foreach (var material in materials)
            {
                builder.AddNode(new DependencyNode(material.Guid, material.Name, DependencyNodeKind.Material, material.AssetPath));
                if (!string.IsNullOrEmpty(material.ShaderGuid))
                    builder.AddEdge(material.Guid, material.ShaderGuid);
            }

            AddTexture(builder, materials, "demo-mat-char-skin", "T_Character_Skin_Albedo", "T_Character_Skin_Normal", "T_Character_Skin_AO");
            AddTexture(builder, materials, "demo-mat-char-hair", "T_Character_Hair_Albedo", "T_Character_Hair_Alpha");
            AddTexture(builder, materials, "demo-mat-rock-01", "T_Rock_01_Albedo", "T_Rock_01_Normal");
            AddTexture(builder, materials, "demo-mat-foliage-bush", "T_Foliage_Atlas_Albedo", "T_Foliage_Atlas_Normal");
            AddTexture(builder, materials, "demo-mat-foliage-tree", "T_Foliage_Atlas_Albedo", "T_Foliage_Atlas_Normal");
            AddTexture(builder, materials, "demo-mat-water-lake", "T_Water_Normal_Flow", "T_Water_Foam_Mask");
            AddTexture(builder, materials, "demo-mat-prop-barrel", "T_Prop_Wood_Albedo");
            AddTexture(builder, materials, "demo-mat-prop-crate", "T_Prop_Metal_Albedo", "T_Prop_Metal_Normal");
            AddTexture(builder, materials, "demo-mat-ui-icon", "T_UI_IconAtlas");

            return builder.Build();
        }

        // Shared textures (e.g. the foliage atlas above) are added once and referenced by every
        // material that uses them — the builder itself de-duplicates nodes by guid.
        private static void AddTexture(
            DependencyGraph.Builder builder, List<MaterialRecord> materials, string materialGuid, params string[] textureNames)
        {
            MaterialRecord material = null;
            foreach (var m in materials)
                if (m.Guid == materialGuid) { material = m; break; }
            if (material == null) return;

            foreach (string textureName in textureNames)
            {
                string textureGuid = "demo-tex-" + textureName;
                string texturePath = "Assets/Textures/" + textureName + ".png";
                builder.AddNode(new DependencyNode(textureGuid, textureName, DependencyNodeKind.Texture, texturePath));
                builder.AddEdge(material.Guid, textureGuid);
            }
        }
    }
}

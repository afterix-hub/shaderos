using System.Collections.Generic;
using ToolsStudio.ShaderOS.Audit;
using ToolsStudio.ShaderOS.Compatibility;

namespace ToolsStudio.ShaderOS.Editor.Analysis
{
    internal static class MaterialIssueLibrary
    {
        public static MaterialIssue BrokenShaderReference() => new MaterialIssue(
            "TS.SO.V001",
            "Shader reference is missing or broken (pink material).",
            "Assign a valid shader in the Material Inspector, or reimport the shader asset.",
            IssueSeverity.Error, IssueCategory.BrokenShaderReference, healthPenalty: 40);

        public static MaterialIssue MissingTexture(string propertyName) => new MaterialIssue(
            "TS.SO.V002",
            $"Texture property '{propertyName}' references a missing or deleted asset.",
            $"Reassign a texture to '{propertyName}' or clear the slot.",
            IssueSeverity.Error, IssueCategory.MissingTexture, healthPenalty: 10, propertyName: propertyName);

        public static MaterialIssue SRPBatcherIncompatible(IReadOnlyList<SRPBatcherBreaker> breakers)
        {
            string detail = breakers?.Count > 0 ? breakers[0].Description : "Shader is not SRP Batcher compatible.";
            return new MaterialIssue(
                "TS.SO.V003",
                detail,
                "Declare all per-material properties inside CBUFFER_START(UnityPerMaterial).",
                IssueSeverity.Warning, IssueCategory.SRPBatcherIncompatible, healthPenalty: 5);
        }

        public static MaterialIssue PipelineMismatch(string materialPipeline, string projectPipeline) => new MaterialIssue(
            "TS.SO.V004",
            $"Material targets '{materialPipeline}' but the project uses '{projectPipeline}'.",
            $"Migrate to a '{projectPipeline}'-compatible shader.",
            IssueSeverity.Warning, IssueCategory.PipelineMismatch, healthPenalty: 20);

        public static MaterialIssue OrphanedMaterial() => new MaterialIssue(
            "TS.SO.V005",
            "Material is not referenced by any scene, prefab, or script.",
            "Remove if unused, or add a reference to suppress this warning.",
            IssueSeverity.Advisory, IssueCategory.OrphanedMaterial, healthPenalty: 0);
    }
}

using System;
using System.Collections.Generic;
using ToolsStudio.ShaderOS.Compatibility;

namespace ToolsStudio.ShaderOS.Audit
{
    internal sealed class MaterialRecord
    {
        public string                    Guid                     { get; }
        public string                    Name                     { get; }
        public string                    AssetPath                { get; }
        public HealthScore               HealthScore              { get; }
        public bool                      HasBrokenShaderReference { get; }
        public bool                      HasMissingTextures       { get; }
        public string                    ShaderGuid               { get; }
        public string                    ShaderName               { get; }
        public SRPBatcherState           SRPBatcherCompatibility  { get; }
        public IReadOnlyList<MaterialIssue> Issues                { get; }
        public string                    PipelineTag              { get; }
        public bool                      IsOrphaned               { get; }

        public MaterialRecord(
            string guid, string name, string assetPath,
            HealthScore healthScore,
            bool hasBrokenShaderReference, bool hasMissingTextures,
            string shaderGuid, string shaderName,
            SRPBatcherState srpBatcherCompatibility,
            IReadOnlyList<MaterialIssue> issues,
            string pipelineTag, bool isOrphaned)
        {
            Guid                     = guid      ?? throw new ArgumentNullException(nameof(guid));
            Name                     = name      ?? throw new ArgumentNullException(nameof(name));
            AssetPath                = assetPath ?? throw new ArgumentNullException(nameof(assetPath));
            HealthScore              = healthScore;
            HasBrokenShaderReference = hasBrokenShaderReference;
            HasMissingTextures       = hasMissingTextures;
            ShaderGuid               = shaderGuid ?? string.Empty;
            ShaderName               = shaderName ?? string.Empty;
            SRPBatcherCompatibility  = srpBatcherCompatibility;
            Issues                   = issues ?? Array.AsReadOnly(Array.Empty<MaterialIssue>());
            PipelineTag              = pipelineTag ?? "Unknown";
            IsOrphaned               = isOrphaned;
        }

        public override string ToString() => $"{Name} ({HealthScore})";
    }
}

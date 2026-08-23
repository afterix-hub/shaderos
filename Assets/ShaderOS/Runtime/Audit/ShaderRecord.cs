using System;
using System.Collections.Generic;

namespace ToolsStudio.ShaderOS.Audit
{
    internal sealed class ShaderRecord
    {
        public string                Guid                   { get; }
        public string                Name                   { get; }
        public string                AssetPath              { get; }
        public bool                  IsBuiltIn              { get; }
        public int                   MaterialReferenceCount { get; }
        public int                   GlobalKeywordCount     { get; }
        public int                   LocalKeywordCount      { get; }
        public int                   EstimatedVariantCount  { get; }
        public IReadOnlyList<string> GlobalKeywords         { get; }
        public IReadOnlyList<string> LocalKeywords          { get; }
        public string                PipelineTarget         { get; }

        public ShaderRecord(
            string guid, string name, string assetPath, bool isBuiltIn,
            int materialReferenceCount, int globalKeywordCount, int localKeywordCount,
            int estimatedVariantCount,
            IReadOnlyList<string> globalKeywords, IReadOnlyList<string> localKeywords,
            string pipelineTarget)
        {
            Guid                   = guid ?? throw new ArgumentNullException(nameof(guid));
            Name                   = name ?? throw new ArgumentNullException(nameof(name));
            AssetPath              = assetPath ?? string.Empty;
            IsBuiltIn              = isBuiltIn;
            MaterialReferenceCount = materialReferenceCount;
            GlobalKeywordCount     = globalKeywordCount;
            LocalKeywordCount      = localKeywordCount;
            EstimatedVariantCount  = estimatedVariantCount;
            GlobalKeywords         = globalKeywords ?? Array.AsReadOnly(Array.Empty<string>());
            LocalKeywords          = localKeywords  ?? Array.AsReadOnly(Array.Empty<string>());
            PipelineTarget         = pipelineTarget ?? "Unknown";
        }

        public override string ToString() => $"{Name} ({MaterialReferenceCount} materials, ~{EstimatedVariantCount} variants)";
    }
}

using System;
using System.Collections.Generic;

namespace ToolsStudio.ShaderOS.Variants
{
    internal enum VariantRiskLevel { Low = 0, Moderate = 1, High = 2, Critical = 3 }

    internal sealed class ShaderVariantEstimate
    {
        public string           ShaderGuid                 { get; }
        public string           ShaderName                 { get; }
        public int              EstimatedCount             { get; }
        public VariantRiskLevel RiskLevel                  { get; }
        public int              StripCandidateKeywordCount { get; }

        public ShaderVariantEstimate(string shaderGuid, string shaderName, int estimatedCount, int stripCandidateKeywordCount)
        {
            ShaderGuid                 = shaderGuid ?? throw new ArgumentNullException(nameof(shaderGuid));
            ShaderName                 = shaderName ?? throw new ArgumentNullException(nameof(shaderName));
            EstimatedCount             = estimatedCount;
            StripCandidateKeywordCount = stripCandidateKeywordCount;
            RiskLevel = estimatedCount >= 10_000 ? VariantRiskLevel.Critical
                      : estimatedCount >=  5_000 ? VariantRiskLevel.High
                      : estimatedCount >=  1_000 ? VariantRiskLevel.Moderate
                                                 : VariantRiskLevel.Low;
        }
    }

    internal sealed class VariantSummary
    {
        public int              TotalEstimatedVariants { get; }
        public VariantRiskLevel ProjectRiskLevel       { get; }
        public IReadOnlyList<ShaderVariantEstimate> ShaderEstimates { get; }
        public int              CriticalShaderCount    { get; }

        public VariantSummary(int totalEstimatedVariants, IReadOnlyList<ShaderVariantEstimate> shaderEstimates)
        {
            TotalEstimatedVariants = totalEstimatedVariants;
            ShaderEstimates        = shaderEstimates ?? throw new ArgumentNullException(nameof(shaderEstimates));

            int criticalCount = 0;
            foreach (var e in shaderEstimates)
                if (e.RiskLevel == VariantRiskLevel.Critical) criticalCount++;
            CriticalShaderCount = criticalCount;

            ProjectRiskLevel = totalEstimatedVariants >= 10_000 ? VariantRiskLevel.Critical
                             : totalEstimatedVariants >=  5_000 ? VariantRiskLevel.High
                             : totalEstimatedVariants >=  1_000 ? VariantRiskLevel.Moderate
                                                                : VariantRiskLevel.Low;
        }

        public static VariantSummary Empty() =>
            new VariantSummary(0, Array.AsReadOnly(Array.Empty<ShaderVariantEstimate>()));
    }
}

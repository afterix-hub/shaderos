using System;
using System.Collections.Generic;
using UnityEngine;
using ToolsStudio.ShaderOS.Audit;
using ToolsStudio.ShaderOS.Variants;

namespace ToolsStudio.ShaderOS.Editor.Analysis
{
    internal sealed class ShaderVariantAnalyzer
    {
        private const int VARIANT_CAP = 2_000_000;

        public int EstimateVariants(Shader shader, IReadOnlyList<string> globalKeywords, IReadOnlyList<string> localKeywords)
        {
            if (shader == null) return 0;
            int dims = CountDimensions(globalKeywords) + CountDimensions(localKeywords);
            if (dims <= 0) return 1;
            if (dims >= 21) return VARIANT_CAP;
            return Math.Min(1 << dims, VARIANT_CAP);
        }

        public VariantSummary BuildSummary(IReadOnlyList<ShaderRecord> shaders)
        {
            if (shaders == null || shaders.Count == 0) return VariantSummary.Empty();

            int total     = 0;
            var estimates = new List<ShaderVariantEstimate>(shaders.Count);

            foreach (var sr in shaders)
            {
                if (sr.EstimatedVariantCount <= 1) continue;
                total += sr.EstimatedVariantCount;
                estimates.Add(new ShaderVariantEstimate(sr.Guid, sr.Name, sr.EstimatedVariantCount, sr.LocalKeywordCount));
            }

            estimates.Sort((a, b) => b.EstimatedCount.CompareTo(a.EstimatedCount));
            return new VariantSummary(total, estimates.AsReadOnly());
        }

        private static int CountDimensions(IReadOnlyList<string> keywords)
        {
            int count = 0;
            foreach (string kw in keywords)
                if (!string.IsNullOrWhiteSpace(kw) && kw != "_") count++;
            return count;
        }
    }
}

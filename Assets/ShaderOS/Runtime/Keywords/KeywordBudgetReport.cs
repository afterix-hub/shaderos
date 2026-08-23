using System;
using System.Collections.Generic;

namespace ToolsStudio.ShaderOS.Keywords
{
    internal enum KeywordBudgetLevel { Safe = 0, Caution = 1, Warning = 2, Critical = 3 }

    internal sealed class ShaderKeywordContribution
    {
        public string                ShaderGuid              { get; }
        public string                ShaderName              { get; }
        public IReadOnlyList<string> UniqueGlobalKeywords    { get; }
        public IReadOnlyList<string> DuplicateGlobalKeywords { get; }
        public int                   LocalKeywordCount       { get; }

        public ShaderKeywordContribution(
            string shaderGuid, string shaderName,
            IReadOnlyList<string> uniqueGlobalKeywords,
            IReadOnlyList<string> duplicateGlobalKeywords,
            int localKeywordCount)
        {
            ShaderGuid              = shaderGuid ?? throw new ArgumentNullException(nameof(shaderGuid));
            ShaderName              = shaderName ?? throw new ArgumentNullException(nameof(shaderName));
            UniqueGlobalKeywords    = uniqueGlobalKeywords    ?? Array.AsReadOnly(Array.Empty<string>());
            DuplicateGlobalKeywords = duplicateGlobalKeywords ?? Array.AsReadOnly(Array.Empty<string>());
            LocalKeywordCount       = localKeywordCount;
        }
    }

    internal sealed class KeywordBudgetReport
    {
        public const int GLOBAL_KEYWORD_LIMIT     = 256;
        public const int LOCAL_KEYWORD_PER_SHADER = 64;

        public int                GlobalKeywordCount    { get; }
        public KeywordBudgetLevel GlobalBudgetLevel     { get; }
        public IReadOnlyList<string> AllGlobalKeywords  { get; }
        public IReadOnlyList<string> DuplicateKeywords  { get; }
        public IReadOnlyList<ShaderKeywordContribution> ShaderContributions { get; }
        public IReadOnlyList<string> OverBudgetShaderGuids { get; }
        public float GlobalUsagePercent => GlobalKeywordCount / (float)GLOBAL_KEYWORD_LIMIT * 100f;

        public KeywordBudgetReport(
            int globalKeywordCount,
            IReadOnlyList<string> allGlobalKeywords,
            IReadOnlyList<string> duplicateKeywords,
            IReadOnlyList<ShaderKeywordContribution> shaderContributions,
            IReadOnlyList<string> overBudgetShaderGuids)
        {
            GlobalKeywordCount    = globalKeywordCount;
            AllGlobalKeywords     = allGlobalKeywords    ?? throw new ArgumentNullException(nameof(allGlobalKeywords));
            DuplicateKeywords     = duplicateKeywords    ?? Array.AsReadOnly(Array.Empty<string>());
            ShaderContributions   = shaderContributions  ?? throw new ArgumentNullException(nameof(shaderContributions));
            OverBudgetShaderGuids = overBudgetShaderGuids ?? Array.AsReadOnly(Array.Empty<string>());

            GlobalBudgetLevel = globalKeywordCount >= GLOBAL_KEYWORD_LIMIT              ? KeywordBudgetLevel.Critical
                              : globalKeywordCount >= (int)(GLOBAL_KEYWORD_LIMIT * 0.90f) ? KeywordBudgetLevel.Warning
                              : globalKeywordCount >= (int)(GLOBAL_KEYWORD_LIMIT * 0.75f) ? KeywordBudgetLevel.Caution
                                                                                           : KeywordBudgetLevel.Safe;
        }

        public static KeywordBudgetReport Empty() => new KeywordBudgetReport(
            0,
            Array.AsReadOnly(Array.Empty<string>()),
            Array.AsReadOnly(Array.Empty<string>()),
            Array.AsReadOnly(Array.Empty<ShaderKeywordContribution>()),
            Array.AsReadOnly(Array.Empty<string>()));
    }
}

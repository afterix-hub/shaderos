using System;
using System.Collections.Generic;

namespace ToolsStudio.ShaderOS.Compatibility
{
    internal enum SRPBatcherState { NotApplicable = 0, Compatible = 1, Incompatible = 2, Unknown = 3 }

    internal sealed class SRPBatcherBreaker
    {
        public string Description       { get; }
        public int    DrawCallsAffected { get; }
        public bool   HasAutoFix        { get; }

        public SRPBatcherBreaker(string description, int drawCallsAffected, bool hasAutoFix)
        {
            Description       = description ?? throw new ArgumentNullException(nameof(description));
            DrawCallsAffected = drawCallsAffected;
            HasAutoFix        = hasAutoFix;
        }
    }

    internal sealed class SRPBatcherAnalysisResult
    {
        public string                        MaterialGuid      { get; }
        public SRPBatcherState               State             { get; }
        public IReadOnlyList<SRPBatcherBreaker> Breakers       { get; }
        public int                           BatchingPotential { get; }

        public SRPBatcherAnalysisResult(
            string materialGuid, SRPBatcherState state,
            IReadOnlyList<SRPBatcherBreaker> breakers, int batchingPotential)
        {
            MaterialGuid      = materialGuid ?? throw new ArgumentNullException(nameof(materialGuid));
            State             = state;
            Breakers          = breakers ?? Array.AsReadOnly(Array.Empty<SRPBatcherBreaker>());
            BatchingPotential = batchingPotential;
        }
    }
}

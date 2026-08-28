using System;
using System.Collections.Generic;

namespace ToolsStudio.ShaderOS.Audit
{
    internal enum HealthSeverity { Healthy = 0, Warning = 1, Critical = 2 }

    internal readonly struct HealthScore : IEquatable<HealthScore>, IComparable<HealthScore>
    {
        public int Value { get; }
        public HealthSeverity Severity { get; }

        public static readonly HealthScore Perfect = new HealthScore(100);
        public static readonly HealthScore Zero    = new HealthScore(0);

        private HealthScore(int value)
        {
            Value    = Math.Max(0, Math.Min(100, value));
            Severity = Value >= 80 ? HealthSeverity.Healthy
                     : Value >= 50 ? HealthSeverity.Warning
                                   : HealthSeverity.Critical;
        }

        public static HealthScore FromPenalties(int totalPenalty) => new HealthScore(100 - totalPenalty);

        public static HealthScore Aggregate(IReadOnlyList<HealthScore> scores)
        {
            if (scores == null || scores.Count == 0) return Perfect;
            int sum = 0;
            for (int i = 0; i < scores.Count; i++) sum += scores[i].Value;
            return new HealthScore(sum / scores.Count);
        }

        public bool Equals(HealthScore other)              => Value == other.Value;
        public override bool Equals(object obj)            => obj is HealthScore h && Equals(h);
        public override int GetHashCode()                  => Value;
        public int CompareTo(HealthScore other)            => Value.CompareTo(other.Value);
        public override string ToString()                  => $"{Value}/100 ({Severity})";

        public static bool operator ==(HealthScore a, HealthScore b) => a.Value == b.Value;
        public static bool operator !=(HealthScore a, HealthScore b) => a.Value != b.Value;
        public static bool operator < (HealthScore a, HealthScore b) => a.Value <  b.Value;
        public static bool operator > (HealthScore a, HealthScore b) => a.Value >  b.Value;
    }
}

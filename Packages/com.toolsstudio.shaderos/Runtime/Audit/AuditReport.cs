using System;
using System.Collections.Generic;
using ToolsStudio.ShaderOS.Dependency;
using ToolsStudio.ShaderOS.Keywords;
using ToolsStudio.ShaderOS.Variants;

namespace ToolsStudio.ShaderOS.Audit
{
    internal enum ScanScope { FullProject = 0, Folder = 1, Scenes = 2 }

    internal sealed class AuditReport
    {
        public DateTime                      Timestamp               { get; }
        public TimeSpan                      ScanDuration            { get; }
        public ScanScope                     Scope                   { get; }
        public string                        ScopeFolderPath         { get; }
        public IReadOnlyList<MaterialRecord> Materials               { get; }
        public IReadOnlyList<ShaderRecord>   Shaders                 { get; }
        public DependencyGraph               DependencyGraph         { get; }
        public KeywordBudgetReport           KeywordBudget           { get; }
        public VariantSummary                VariantSummary          { get; }
        public HealthScore                   ProjectHealthScore      { get; }
        public bool                          IsPartial               { get; }
        public int                           TotalMaterialsInProject { get; }
        public int                           AnalyzedMaterialCount   => Materials.Count;
        public int                           CriticalIssueCount      { get; }

        public AuditReport(
            DateTime timestamp, TimeSpan scanDuration, ScanScope scope,
            string scopeFolderPath,
            IReadOnlyList<MaterialRecord> materials,
            IReadOnlyList<ShaderRecord>   shaders,
            DependencyGraph dependencyGraph,
            KeywordBudgetReport keywordBudget,
            VariantSummary variantSummary,
            bool isPartial, int totalMaterialsInProject)
        {
            Timestamp               = timestamp;
            ScanDuration            = scanDuration;
            Scope                   = scope;
            ScopeFolderPath         = scopeFolderPath ?? string.Empty;
            Materials               = materials       ?? throw new ArgumentNullException(nameof(materials));
            Shaders                 = shaders         ?? throw new ArgumentNullException(nameof(shaders));
            DependencyGraph         = dependencyGraph ?? throw new ArgumentNullException(nameof(dependencyGraph));
            KeywordBudget           = keywordBudget   ?? throw new ArgumentNullException(nameof(keywordBudget));
            VariantSummary          = variantSummary  ?? throw new ArgumentNullException(nameof(variantSummary));
            IsPartial               = isPartial;
            TotalMaterialsInProject = totalMaterialsInProject;

            var scores = new HealthScore[materials.Count];
            int criticalCount = 0;
            for (int i = 0; i < materials.Count; i++)
            {
                scores[i] = materials[i].HealthScore;
                foreach (var issue in materials[i].Issues)
                    if (issue.Severity == IssueSeverity.Error)
                        criticalCount++;
            }
            ProjectHealthScore = HealthScore.Aggregate(scores);
            CriticalIssueCount = criticalCount;
        }

        public override string ToString()
            => $"AuditReport [{Timestamp:u}] — {AnalyzedMaterialCount} materials, " +
               $"{ProjectHealthScore}, {CriticalIssueCount} critical issues" +
               (IsPartial ? " [PARTIAL]" : string.Empty);
    }
}

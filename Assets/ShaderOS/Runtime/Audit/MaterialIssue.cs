namespace ToolsStudio.ShaderOS.Audit
{
    internal enum IssueSeverity { Advisory = 0, Warning = 1, Error = 2 }

    internal enum IssueCategory
    {
        BrokenShaderReference  = 0,
        MissingTexture         = 1,
        SRPBatcherIncompatible = 2,
        PipelineMismatch       = 3,
        OrphanedMaterial       = 4,
        KeywordBudgetExceeded  = 5,
        NullPropertyValue      = 6
    }

    internal sealed class MaterialIssue
    {
        public string        ErrorCode    { get; }
        public string        Description  { get; }
        public string        Resolution   { get; }
        public IssueSeverity Severity     { get; }
        public IssueCategory Category     { get; }
        public int           HealthPenalty { get; }
        public string        PropertyName { get; }

        public MaterialIssue(
            string errorCode, string description, string resolution,
            IssueSeverity severity, IssueCategory category, int healthPenalty,
            string propertyName = null)
        {
            ErrorCode     = errorCode    ?? throw new System.ArgumentNullException(nameof(errorCode));
            Description   = description  ?? throw new System.ArgumentNullException(nameof(description));
            Resolution    = resolution   ?? string.Empty;
            Severity      = severity;
            Category      = category;
            HealthPenalty = healthPenalty;
            PropertyName  = propertyName ?? string.Empty;
        }

        public override string ToString() => $"[{ErrorCode}] {Description}";
    }
}

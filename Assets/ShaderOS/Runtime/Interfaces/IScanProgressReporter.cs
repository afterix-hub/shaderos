using ToolsStudio.ShaderOS.Audit;

namespace ToolsStudio.ShaderOS.Interfaces
{
    // Implementations must not throw — exceptions are swallowed by the engine.
    internal interface IScanProgressReporter
    {
        void OnProgress(string currentItem, int processedCount, int totalCount);
        void OnComplete(AuditReport report);
        void OnCancelled();
    }
}

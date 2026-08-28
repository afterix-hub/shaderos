using UnityEditor;
using ToolsStudio.ShaderOS.Audit;
using ToolsStudio.ShaderOS.Editor.Analysis;

namespace ToolsStudio.ShaderOS.Editor.Session
{
    [FilePath("ProjectSettings/ShaderOSSession.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class ShaderOSSession : ScriptableSingleton<ShaderOSSession>
    {
        public static ShaderOSSession Instance => instance;

        // AuditReport is not Unity-serializable; state resets on domain reload.
        [System.NonSerialized] private AuditReport _lastReport;
        [System.NonSerialized] private AuditReport _previewReport;
        [System.NonSerialized] private bool        _isScanRunning;
        [System.NonSerialized] private bool        _isPreviewActive;

        public AuditReport LastReport      => _lastReport;
        public bool        IsScanRunning   => _isScanRunning;
        public bool        HasReport       => _lastReport != null;
        public bool        IsPreviewActive => _isPreviewActive;

        // Explicit choice, not derived from _lastReport's mere presence — only OnScanStarted
        // forces this back to real, so a user can return to demo data after a real scan.
        public AuditReport DisplayedReport  => _isPreviewActive ? _previewReport : _lastReport;
        public bool        IsShowingPreview => _isPreviewActive;

        public void EnablePreview()
        {
            _previewReport ??= PreviewReportFactory.Build();
            _isPreviewActive = true;
        }

        public void DisablePreview()
        {
            _isPreviewActive = false;
        }

        public void OnScanStarted()
        {
            _isScanRunning   = true;
            _isPreviewActive = false;
        }

        public void OnScanCompleted(AuditReport report)
        {
            _lastReport    = report;
            _isScanRunning = false;
            ShaderOSEditorEvents.RaiseReportUpdated(report);
        }

        public void OnScanCancelled()
        {
            _isScanRunning = false;
            ShaderOSEditorEvents.RaiseScanCancelled();
        }

        public void Clear()
        {
            _lastReport      = null;
            _previewReport   = null;
            _isScanRunning   = false;
            _isPreviewActive = false;
        }
    }

    internal static class ShaderOSEditorEvents
    {
        public static event System.Action<AuditReport> ReportUpdated;
        public static event System.Action              ScanCancelled;

        internal static void RaiseReportUpdated(AuditReport report) => ReportUpdated?.Invoke(report);
        internal static void RaiseScanCancelled()                   => ScanCancelled?.Invoke();
    }
}

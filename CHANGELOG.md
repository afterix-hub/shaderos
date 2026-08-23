# Changelog

All notable changes to the ShaderOS source are documented here. This is the complete technical record; the customer-facing changelog shipped inside the Asset Store package is a subset of this content, scoped to what a customer experiences.

Format based on [Keep a Changelog](https://keepachangelog.com/). Versions follow MAJOR.MINOR.PATCH.

## [1.0.0] — 2026

### Added

- Initial ShaderOS release: Editor window (`Tools -> ShaderOS`) with Overview, Materials, Keywords, Variants, and About tabs
- `MaterialAuditEngine` — full-project or folder-scoped scan over materials, resolving each material's shader, health score, and issue set
- Diagnostics: broken shader reference (TS.SO.V001), missing texture property (TS.SO.V002), SRP Batcher incompatibility with reasons (TS.SO.V003)
- Per-material and project-wide health scoring (`HealthScore`, 0–100, Healthy/Warning/Critical bands)
- Global and local shader keyword budget tracking against Unity's 256-keyword and 64-local-keyword limits, with duplicate-keyword detection across shaders (`ShaderKeywordAnalyzer`, `KeywordBudgetReport`)
- Estimated shader variant counts with risk banding (`ShaderVariantAnalyzer`, `VariantSummary`)
- Material -> Shader -> Texture dependency graph with unreferenced-node counting (`DependencyGraphBuilder`)
- Build-time shader compilation tracking with per-build regression detection (`ShaderOSBuildTracker`, `IPreprocessBuildWithReport`/`IPostprocessBuildWithReport`), 10-build history retained in the free edition
- Project Settings page (`Edit -> Project Settings -> Tools Studio -> ShaderOS`): default scan scope/folder, health score thresholds, SRP Batcher and keyword budget warning toggles
- `Load Demo Data` toolbar action and `PreviewReportFactory` — builds a fully populated example `AuditReport` from curated data, reusing the same models and analyzers `MaterialAuditEngine` uses for a real scan; always available, never blocks or is blocked by a real scan, never persisted to disk
- Stage-by-stage scan logging via `ShaderOSLogger` (Starting scan, Collecting materials, Building dependency graph, Calculating keyword budget, Generating report, Scan completed)
- Free tier: 100-material scan limit per pass

### Fixed

- `ShaderOSProjectSettings` did not expose a public accessor for its `ScriptableSingleton<T>` instance; `ShaderOSWindow` called the inherited `protected` member directly, which does not compile (`CS0122`). Added `public static ShaderOSProjectSettings Instance => instance;`, matching the pattern already used by `ShaderOSSession`
- Project Settings scan-folder picker validated the selected path against `Application.dataPath` only, silently rejecting valid project-internal folders outside `Assets/` (`Packages/`, `ProjectSettings/`) with no feedback either way. Folder validation now checks the full project root and shows an explicit dialog when a genuinely external path is selected
- Corrected every customer-facing link (documentation URL, support email, Discord channels, cross-product Asset Store links) to the values recorded in the Tools Studio Links Registry; several had drifted to unregistered or incorrect values
- Corrected the customer-facing changelog and Features documentation to list only diagnostic codes the scan engine actually produces

### Documentation

- Documented the `Load Demo Data` action and toolbar layout across all customer-facing documentation surfaces

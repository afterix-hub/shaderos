# ShaderOS

Editor-only shader and material diagnostics for Unity. Scans a project's materials and shaders, surfaces broken references, missing textures, SRP Batcher incompatibilities, keyword budget pressure, and estimated shader variant counts — all read-only, nothing in this tool ever modifies a project asset.

This is the private source repository. It is not the package a customer receives — see [Repository Types](#repository-layout) below for how this relates to the Asset Store Package and the public Documentation Repository.

## Requirements

- Unity 2021.3 LTS or newer (developed and last verified against 6000.3.10f1)
- No third-party package dependencies — both assemblies compile against Unity's own core modules only

## Opening the Project
Open this folder directly in Unity via Unity Hub. No package installation step is required beyond what's already in `Packages/manifest.json`.

## Architecture

Two assemblies, Editor-only:

| Assembly | Path | Scope |
|---|---|---|
| `ToolsStudio.ShaderOS` | `Assets/ShaderOS/Runtime/` | Pure data models and analyzers — no `UnityEngine`/`UnityEditor` reference |
| `ToolsStudio.ShaderOS.Editor` | `Assets/ShaderOS/Editor/` | Scan engine, Editor window, Project Settings page — references the Runtime assembly only |

Entry point: `Tools -> ShaderOS`. Settings: `Edit -> Project Settings -> Tools Studio -> ShaderOS`.

## Development Workflow

Edit any `.cs` file under `Assets/ShaderOS/`; Unity recompiles both assemblies automatically. Open the ShaderOS window (`Tools -> ShaderOS`) to test changes directly — `Load Demo Data` gives you a populated report to work against without needing a real project's materials on hand. There is no separate build step during development; compiling for release only matters at export time, covered in `DLL-PUBLISHING-GUIDE.md`.

## Development Notes

- The `Editor` assembly is the only consumer of `UnityEditor`/`UnityEngine` APIs; the `Runtime` assembly stays engine-free by design so its analyzers are trivially unit-testable and so it can never end up referenced from anything player-facing.
- `PreviewReportFactory` (`Editor/Analysis/`) builds the built-in demo report shown by the in-window Load Demo Data action. It reuses the same models and analyzers a real scan uses — there is no separate demo data path.
- See `DLL-PUBLISHING-GUIDE.md` at this repository's root for the binary-only Asset Store export process.

## Repository Layout

This repository is one of two Tools Studio maintains for ShaderOS. The Asset Store Package customers actually install is a release artifact built from this repository — never a direct copy of it — and is never itself a repository. The public [Documentation Repository](https://github.com/tools-studio/shaderos-docs) is maintained independently of this one.

## License

Proprietary. This repository is private; ShaderOS is distributed to customers exclusively through the Unity Asset Store under Unity's Standard EULA.

---

Copyright (c) 2026 Arish. All rights reserved.
Tools Studio is a brand operated by Arish.

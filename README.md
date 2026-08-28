# ShaderOS

Shader diagnostics and health monitoring for Unity.

This is ShaderOS's public source repository. ShaderOS gives you immediate visibility into the shader health of your Unity project. Scan every material in one click, find broken references instantly, track keyword budget consumption before it becomes a compile failure, and understand where variant counts are growing before they become a build problem — all read-only, nothing here ever modifies a project asset.

## What it does

- **Material Health Scan** — broken shader reference detection, missing texture slot detection, a per-material health score, and SRP Batcher compatibility flags with specific incompatibility reasons
- **Keyword Budget Tracker** — global keyword usage against Unity's 256-keyword hard limit, per-shader local keyword tracking against the 64-keyword limit, and duplicate keyword detection across shaders
- **Variant Estimator** — theoretical variant counts per shader with risk classification, always labeled as pre-strip upper bounds
- **Dependency Graph** — a Material → Shader → Texture relationship model built from Unity's own dependency data
- **Build Tracker** — shader compile time recorded after every build, stored per machine for later comparison
- **Load Demo Data** — a fully populated example report to explore before running your first scan

## Source Included

ShaderOS's `.cs` source is included and editable, both here and in the Unity Asset Store package. Nothing ships as a compiled DLL. See [LICENSE.md](LICENSE.md) for exactly what the license grants and withholds, and [CONTRIBUTING.md](CONTRIBUTING.md) for how to open the project and submit a change.

## Documentation

Full documentation — Getting Started, Features, Configuration, Troubleshooting, FAQ — lives in the [documentation repository](https://github.com/tools-studio/shaderos-docs).

[Documentation](https://github.com/tools-studio/shaderos-docs) · [Support](SUPPORT.md) · [Contributing](CONTRIBUTING.md)

## Other Tools Studio Products

- [Runtime Atlas](https://assetstore.unity.com/packages/tools/utilities/runtime-atlas-367424) — server-authoritative runtime economy
- [Audio Atlas](https://assetstore.unity.com/packages/tools/utilities/audio-atlas-378112) — zero-GC audio architecture for Unity
- [BuildLens](https://assetstore.unity.com/packages/tools/utilities/buildlens-378102) — build size and dependency analysis

## License

Source-Available under the [Tools Studio Source-Available License](LICENSE.md) — you can read, run, and modify this source for personal and commercial projects. Redistribution, resale, rebranding, and use of this source to build a competing product are not permitted. The compiled product is additionally distributed through the Unity Asset Store under Unity's Standard EULA.

---

Copyright (c) 2026 Arish. All rights reserved.
Tools Studio is a brand operated by Arish.

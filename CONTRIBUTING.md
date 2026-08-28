# Contributing to ShaderOS

Thanks for taking the time to contribute. This document covers how to open the project, how the source is organized, and how to submit a change.

## Opening the project

Clone this repository and open its root folder directly in Unity Hub — it's a complete, directly-openable Unity project. Requires Unity 2021.3 LTS or newer.

ShaderOS's own source lives under `Packages/com.toolsstudio.shaderos/` as an embedded UPM package; Unity resolves and compiles it automatically once the project is open. The `Assets/` folder is intentionally empty — it holds no product code.

## Source layout

| Path | Assembly | Contents |
|---|---|---|
| `Packages/com.toolsstudio.shaderos/Runtime/` | `ToolsStudio.ShaderOS` | Data model and analysis result types. No `UnityEngine`/`UnityEditor` dependency. |
| `Packages/com.toolsstudio.shaderos/Editor/` | `ToolsStudio.ShaderOS.Editor` | Scan engine, editor window, panels, project settings, build tracking. |
| `Packages/com.toolsstudio.shaderos/Tests/` | *(reserved)* | No test assembly ships yet. See the folder's own README. |

Both assemblies are Editor-only (`includePlatforms: ["Editor"]`) — ShaderOS never ships code into a player build.

## Making a change

1. Create a branch off `main` named `fix/<short-description>` or `feature/<short-description>`.
2. Keep the change focused — one fix or one feature per pull request.
3. Match the existing code style: explicit access modifiers, no banner or section-divider comments, and comments reserved for a genuine Unity-engine limitation, a misuse-prevention warning, or a safety note — one or two lines, never more.
4. Open a pull request against `main` using the pull request template. Every contribution requires a sign-off (`git commit -s`) attesting that you have the right to submit the change under this repository's license — see [LICENSE.md](LICENSE.md).
5. Pull requests are squash-merged, so keep your branch's own commit history clean but don't worry about rewriting it before review.

There is no separate contributor license agreement to sign — the commit sign-off is the only attestation required.

## Reporting bugs or requesting features

Use [GitHub Issues](https://github.com/afterix-hub/shaderos/issues) on this repository for bugs found in the source itself. For product-level bug reports, feature requests, or usage questions, see [SUPPORT.md](SUPPORT.md).

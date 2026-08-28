This folder is reserved for the package's test assembly.

`AssemblyInfo.cs` in both `Runtime/` and `Editor/` already grants
`InternalsVisibleTo("ToolsStudio.ShaderOS.Tests.Editor")`, anticipating a
single combined edit-mode/play-mode test assembly under this folder named
`ToolsStudio.ShaderOS.Tests.Editor`. No test assembly ships yet.

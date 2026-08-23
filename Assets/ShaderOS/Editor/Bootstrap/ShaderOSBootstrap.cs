using UnityEditor;
using ToolsStudio.ShaderOS.Editor.Styles;

namespace ToolsStudio.ShaderOS.Editor.Bootstrap
{
    [InitializeOnLoad]
    internal static class ShaderOSBootstrap
    {
        static ShaderOSBootstrap()
        {
            ShaderOSEditorStyles.Reset();
            AssemblyReloadEvents.beforeAssemblyReload += ShaderOSEditorStyles.Reset;
        }
    }
}

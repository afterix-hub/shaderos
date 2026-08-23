using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ToolsStudio.ShaderOS.Audit;
using ToolsStudio.ShaderOS.Dependency;

namespace ToolsStudio.ShaderOS.Editor.Analysis
{
    internal sealed class DependencyGraphBuilder
    {
        public DependencyGraph Build(IReadOnlyList<MaterialRecord> materials, IReadOnlyList<ShaderRecord> shaders)
        {
            var builder      = new DependencyGraph.Builder();
            var texturesSeen = new HashSet<string>();

            foreach (var sr in shaders)
                builder.AddNode(new DependencyNode(sr.Guid, sr.Name, DependencyNodeKind.Shader, sr.AssetPath));

            foreach (var mr in materials)
            {
                builder.AddNode(new DependencyNode(mr.Guid, mr.Name, DependencyNodeKind.Material, mr.AssetPath));

                if (!string.IsNullOrEmpty(mr.ShaderGuid))
                    builder.AddEdge(mr.Guid, mr.ShaderGuid);

                AddTextureDependencies(mr, builder, texturesSeen);
            }

            return builder.Build();
        }

        private static void AddTextureDependencies(
            MaterialRecord mr, DependencyGraph.Builder builder, HashSet<string> seen)
        {
            foreach (string depPath in AssetDatabase.GetDependencies(mr.AssetPath, recursive: false))
            {
                var type = AssetDatabase.GetMainAssetTypeAtPath(depPath);
                if (type == null || !IsTextureType(type)) continue;

                string texGuid = AssetDatabase.AssetPathToGUID(depPath);
                if (string.IsNullOrEmpty(texGuid)) continue;

                if (seen.Add(texGuid))
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(depPath);
                    builder.AddNode(new DependencyNode(texGuid, name, DependencyNodeKind.Texture, depPath));
                }

                builder.AddEdge(mr.Guid, texGuid);
            }
        }

        private static bool IsTextureType(Type type)
            => type == typeof(Texture2D)   ||
               type == typeof(Texture3D)   ||
               type == typeof(Cubemap)     ||
               type == typeof(Texture2DArray);
    }
}

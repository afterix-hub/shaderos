using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToolsStudio.ShaderOS.Editor.Analysis
{
    internal sealed class BrokenReferenceDetector
    {
        private const string HIDDEN_ERROR_SHADER = "Hidden/InternalErrorShader";

        public bool IsShaderBroken(Material material)
        {
            if (material == null) return false;
            if (material.shader == null) return true;
            return material.shader.name == HIDDEN_ERROR_SHADER;
        }

        public bool HasMissingTextures(Material material, out IReadOnlyList<string> missingSlotNames)
        {
            var missing = new List<string>();

            if (material == null || material.shader == null)
            {
                missingSlotNames = missing.AsReadOnly();
                return false;
            }

            int propCount = material.shader.GetPropertyCount();
            for (int i = 0; i < propCount; i++)
            {
                if (material.shader.GetPropertyType(i) != ShaderPropertyType.Texture) continue;
                string propName = material.shader.GetPropertyName(i);
                if (IsMissingTextureReference(material, propName))
                    missing.Add(propName);
            }

            missingSlotNames = missing.AsReadOnly();
            return missing.Count > 0;
        }

        // SerializedObject distinguishes "slot empty" from "asset deleted":
        // deleted assets serialize as null objectReferenceValue with non-zero instanceID.
        private static bool IsMissingTextureReference(Material material, string propertyName)
        {
            using (var so = new SerializedObject(material))
            {
                var textures = so.FindProperty("m_SavedProperties.m_TexEnvs");
                if (textures == null || !textures.isArray) return false;

                for (int i = 0; i < textures.arraySize; i++)
                {
                    var elem    = textures.GetArrayElementAtIndex(i);
                    var keyProp = elem.FindPropertyRelative("first");
                    if (keyProp == null || keyProp.stringValue != propertyName) continue;

                    var texProp = elem.FindPropertyRelative("second.m_Texture");
                    if (texProp == null) continue;

                    return texProp.objectReferenceValue == null &&
                           texProp.objectReferenceInstanceIDValue != 0;
                }
            }
            return false;
        }
    }
}

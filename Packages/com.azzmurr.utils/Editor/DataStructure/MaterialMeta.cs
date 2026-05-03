#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Thry.ThryEditor;
using UnityEngine;

namespace Azzmurr.Utils {
    [Serializable]
    internal class MaterialMeta {
        public MaterialMeta(Material material) {
            Material = material;
        }

        public List<TextureMeta> Textures => GetTextures();

        public string Name => Material.name;

        public Material Material { get; }

        public Shader Shader => Material.shader;

        public bool Poiyomi => Shader.name.Contains(".poiyomi");

        public bool Standard => Shader.name == "Standard";

        public bool StandardLite => Shader.name == "VRChat/Mobile/Standard Lite";

        public bool ToonStandard => Shader.name == "VRChat/Mobile/Toon Standard";

        public bool ToonLit => Shader.name == "VRChat/Mobile/Toon Lit";

        public bool FastFur => Shader.name.Contains("Fast Fur");

        public bool Goo => Shader.name.Contains(".ValueFactory");

        public bool NotLockable => ShaderVersion == "Unknown" || Standard || StandardLite || ToonStandard || ToonLit;

        public bool ShaderLocked => Shader.name.Contains("Hidden");

        public string ShaderLockedString => NotLockable ? "---" : Shader.name.Contains("Hidden").ToString();

        public string ShaderName => ShaderLocked ? Shader.name.Split("/")[^2] : Shader.name.Split("/")[^1];

        public bool? ShaderLockedError => !NotLockable ? !ShaderLocked : null;

        public string ShaderVersion => GetShaderVersion();

        public bool? ShaderVersionError => Poiyomi ? ShaderVersion != "Latest" : null;

        public Material GetQuestMaterial() {
            Material newQuestMaterial = new(Material) {
                shader = Shader.Find("VRChat/Mobile/Toon Standard")
            };
            return newQuestMaterial;
        }

        private List<TextureMeta> GetTextures() {
            HashSet<Texture> textures = new();
            var textureIds = Material.GetTexturePropertyNameIDs();
            foreach (var id in textureIds) {
                if (!Material.HasProperty(id)) continue;
                var texture = Material.GetTexture(id);
                if (texture == null) continue;

                textures.Add(texture);
            }

            return textures.ToList().ConvertAll(texture => new TextureMeta(texture));
        }

        private string GetShaderVersion() {
            if (Standard || StandardLite || ToonStandard || ToonLit) return "---";

            if (Poiyomi) {
                if (Shader.name.Contains("Hidden") && Shader.name.Contains("Old Versions"))
                    return $"{Shader.name.Split("/")[4]}";

                if (!Shader.name.Contains("Hidden") && Shader.name.Contains("Old Versions"))
                    return Shader.name.Split("/")[2];

                return "Latest";
            }

            if (FastFur)
                return Shader.name.Contains("Hidden") ? $"{Shader.name.Split("/")[2]}" : Shader.name.Split("/")[0];

            if (Goo)
                return Shader.name.Contains("Hidden") ? $"{Shader.name.Split("/")[2]}" : Shader.name.Split("/")[1];

            return "Unknown";
        }

        public void UnlockMaterial() {
            if (!Poiyomi) return;
            ShaderOptimizer.UnlockMaterials(new List<Material> { Material });
        }

        public void UpdateMaterial() {
            if (!Poiyomi || ShaderLocked) return;
            Material.shader = Shader.Find(".poiyomi/Poiyomi Pro");
        }

        public void LockMaterial() {
            if (!Poiyomi) return;
            ShaderOptimizer.LockMaterials(new List<Material> { Material });
        }

        public T? GetPropertyValue<T>(string propertyName) {
            if (!Material.HasProperty(propertyName))
                return default;

            var type = typeof(T);

            return type switch {
                not null when type == typeof(float) => (T)(object)Material.GetFloat(propertyName),
                not null when type == typeof(int) => (T)(object)Material.GetInt(propertyName),
                not null when type == typeof(Color) => (T)(object)Material.GetColor(propertyName),
                not null when type == typeof(Vector4) => (T)(object)Material.GetVector(propertyName),
                not null when type == typeof(Matrix4x4) => (T)(object)Material.GetMatrix(propertyName),
                not null when type == typeof(Texture) => (T)(object)Material.GetTexture(propertyName)!,
                not null when type == typeof(Texture2D) => (T)(object)Material.GetTexture(propertyName)!,
                _ => throw new ArgumentException($"Unsupported property type: {type}")
            };
        }

        public void SetPropertyValue(string propertyName, object value) {
            if (!Material.HasProperty(propertyName))
                return;

            switch (value) {
                case not null when value is float f:
                    Material.SetFloat(propertyName, f);
                    break;

                case not null when value is int i:
                    Material.SetInt(propertyName, i);
                    break;

                case not null when value is Color color:
                    Material.SetColor(propertyName, color);
                    break;

                case not null when value is Vector4 vector4:
                    Material.SetVector(propertyName, vector4);
                    break;

                case not null when value is Texture texture:
                    Material.SetTexture(propertyName, texture);
                    break;

                case not null when value is Matrix4x4 x4:
                    Material.SetMatrix(propertyName, x4);
                    break;

                case null:
                    Debug.LogWarning($"You trying to set {propertyName} to null, but it's not supported");
                    break;

                default:
                    throw new ArgumentException($"Unsupported property type");
            }
        }
    }
}
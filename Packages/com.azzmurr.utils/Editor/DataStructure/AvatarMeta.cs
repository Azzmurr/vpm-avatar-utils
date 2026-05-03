using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Thry.ThryEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;

namespace Azzmurr.Utils {
    [Serializable]
    internal class AvatarMeta {
        public List<MaterialMeta> materials;
        public List<TextureMeta> textures;
        public Dictionary<Texture, HashSet<Material>> MaterialsRelatedToTextures = new();

        public AvatarMeta(GameObject gameObject) {
            GameObject = gameObject;
            Recalculate();
        }

        public string Name => GameObject.name;
        public GameObject GameObject { get; }
        public int MaterialsCount => materials?.Count() ?? 0;
        public int TextureCount => textures?.Count() ?? 0;

        public void Recalculate() {
            EditorUtility.DisplayProgressBar("Getting Avatar Data", "Getting Materials", 0.3f);
            materials = GetMaterials();

            EditorUtility.DisplayProgressBar("Getting Avatar Data", "Getting Textures", 0.6f);
            textures = GetTextures();

            EditorUtility.ClearProgressBar();
        }

        public void ForeachMaterial(Action<MaterialMeta> action) {
            foreach (var material in materials) action.Invoke(material);
        }

        public void ForeachTexture(Action<TextureMeta> action) {
            foreach (var texture in textures) action.Invoke(texture);
        }

        public void ForeachTextureMaterial(TextureMeta texture, Action<Material> action) {
            if (MaterialsRelatedToTextures[texture.Texture] == null) return;
            foreach (var material in MaterialsRelatedToTextures[texture.Texture]) action.Invoke(material);
        }

        public void MakeAllTextures2K() {
            ForeachTexture(texture => {
                if (texture.PcResolution > 2048) texture.ChangeImportSize(2048);
            });

            Recalculate();
        }

        public void MakeTexturesReadyForAndroid() {
            ForeachTexture(texture => {
                if (texture.AndroidResolution > texture.PcResolution / 2 && texture.PcResolution > 512)
                    texture.ChangeImportSizeAndroid(texture.PcResolution / 2);
            });

            Recalculate();
        }

        public void CrunchTextures() {
            ForeachTexture(texture => {
                if (texture.BestTextureFormat != null && texture.Format != null &&
                    (TextureImporterFormat)texture.Format != texture.BestTextureFormat)
                    texture.ChangeImporterFormat((TextureImporterFormat)texture.BestTextureFormat);
            });

            Recalculate();
        }

        public void CreateQuestMaterialPresets() {
            var scene = SceneManager.GetActiveScene();
            var dialog = EditorUtility.DisplayDialog(
                "Create Quest Materials",
                $"You are going to create Quest materials with changed shader to VRChat/Mobile/Standard in Assets/Quest Materials/{scene.name}/{Name}.",
                "Yes let's do this!", "Na aah, I just hanging around"
            );

            if (!dialog) return;

            if (!Directory.Exists("Assets/Quest Materials")) Directory.CreateDirectory("Assets/Quest Materials");

            if (!Directory.Exists($"Assets/Quest Materials/{scene.name.Trim()}"))
                Directory.CreateDirectory($"Assets/Quest Materials/{scene.name.Trim()}");

            if (!Directory.Exists($"Assets/Quest Materials/{scene.name.Trim()}/{Name.Trim()}"))
                Directory.CreateDirectory($"Assets/Quest Materials/{scene.name.Trim()}/{Name.Trim()}");

            ForeachMaterial(material => {
                if (material == null) return;
                var newQuestMaterial = material.GetQuestMaterial();
                AssetDatabase.CreateAsset(newQuestMaterial,
                    $"Assets/Quest Materials/{scene.name.Trim()}/{Name.Trim()}/Quest {material.Name}.mat");
            });

            EditorGUIUtility.PingObject(
                AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                    $"Assets/Quest Materials/{scene.name.Trim()}/{Name.Trim()}"));
            AssetDatabase.Refresh();
        }

        public void UnlockMaterials() {
            var poi = materials.Where(meta => meta.Poiyomi).ToList().ConvertAll(meta => meta.Material);
            ShaderOptimizer.UnlockMaterials(poi);
            Recalculate();
        }

        public void UpdateMaterials() {
            var poi = materials.Where(meta => meta.Poiyomi && !meta.ShaderLocked).ToList()
                .ConvertAll(meta => meta.Material);
            poi.ForEach(mat => { mat.shader = Shader.Find(".poiyomi/Poiyomi Pro"); });

            Recalculate();
        }

        public void LockMaterials() {
            var poi = materials.Where(meta => meta.Poiyomi).ToList().ConvertAll(meta => meta.Material);
            ShaderOptimizer.LockMaterials(poi);
            Recalculate();
        }

        private List<MaterialMeta> GetMaterials() {
            var allBuiltRenderers = GameObject
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.gameObject.GetComponentsInParent<Transform>(true)
                    .All(transform => !transform.CompareTag("EditorOnly")));

            var materialsAll = allBuiltRenderers.SelectMany(r => r.sharedMaterials)
                .Where(material => material != null).ToList();
            var descriptor = GameObject.GetComponent<VRCAvatarDescriptor>();

            if (descriptor != null) {
                var clips = descriptor
                    .baseAnimationLayers
                    .Select(layer => layer.animatorController)
                    .Where(controller => controller != null)
                    .SelectMany(controller => controller.animationClips)
                    .Distinct();

                foreach (var clip in clips) {
                    var clipMaterials = AnimationUtility
                        .GetObjectReferenceCurveBindings(clip)
                        .Where(binding =>
                            binding.isPPtrCurve && binding.type.IsSubclassOf(typeof(Renderer)) &&
                            binding.propertyName.StartsWith("m_Materials"))
                        .SelectMany(binding => AnimationUtility.GetObjectReferenceCurve(clip, binding))
                        .Select(r => r.value as Material);

                    materialsAll.AddRange(clipMaterials);
                }
            }

            var materialMetas = materialsAll
                .ToHashSet()
                .ToList()
                .ConvertAll(material => new MaterialMeta(material));

            return materialMetas;
        }

        private List<TextureMeta> GetTextures() {
            var hashSet = new HashSet<TextureMeta>();
            MaterialsRelatedToTextures = new Dictionary<Texture, HashSet<Material>>();

            ForeachMaterial(material => {
                if (material == null) return;

                material.Textures.ForEach(texture => {
                    if (MaterialsRelatedToTextures.ContainsKey(texture.Texture)) {
                        var relatedMaterials = MaterialsRelatedToTextures.GetValueSafe(texture.Texture);
                        relatedMaterials.Add(material.Material);
                    }
                    else {
                        MaterialsRelatedToTextures.Add(texture.Texture, new HashSet<Material> { material.Material });
                        hashSet.Add(texture);
                    }
                });
            });

            var textureMetas = hashSet.ToList();
            textureMetas.Sort((t1, t2) => {
                var material1 = MaterialsRelatedToTextures[t1.Texture].ToList()[0].name;
                var material2 = MaterialsRelatedToTextures[t2.Texture].ToList()[0].name;

                return string.Compare(material1, material2, StringComparison.Ordinal);
            });

            return textureMetas;
        }
    }
}
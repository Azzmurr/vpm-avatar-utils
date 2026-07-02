using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;

namespace Azzmurr.Utils {
    [Serializable]
    internal class AvatarMeta {
        public List<MaterialMeta> materials = new();
        public List<TextureMeta> textures = new();
        public Dictionary<Texture, HashSet<Material>> MaterialsRelatedToTextures = new();

        public AvatarMeta(GameObject gameObject) {
            GameObject = gameObject;
            Recalculate();
        }

        public string Name => GameObject.name;
        public GameObject GameObject { get; }
        public int MaterialsCount => materials?.Count() ?? 0;
        public int TextureCount => textures?.Count() ?? 0;
        public string TexturesMemory => GetTexturesMemory();

        public void Recalculate() {
            EditorUtility.DisplayProgressBar("Getting Avatar Data", "Getting Materials", 0.3f);
            materials.Clear();
            materials.AddRange(GetMaterials());

            EditorUtility.DisplayProgressBar("Getting Avatar Data", "Getting Textures", 0.6f);
            textures.Clear();
            textures.AddRange(GetTextures());

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

        public string GetTexturesMemory() {
            var memory = textures.Sum(texture => texture.Size);
            return Common.ToMebiByteString(memory);
        }

        public void ChangeAllPCTexturesSize(int size = 2048) {
            ForeachTexture(texture => {
                if (texture.PcResolution != size) texture.ChangePCImportSize(size);
            });
        }

        public void ChangeAllAndroidTexturesSize(int size = 1024) {
            ForeachTexture(texture => {
                if (texture.AndroidResolution != size) texture.ChangeAndroidImportSize(size);
            });
        }

        public void MakeTexturesReadyForAndroid() {
            ForeachTexture(texture => {
                if (texture.AndroidResolution > texture.PcResolution / 2 && texture.PcResolution > 512)
                    texture.ChangeAndroidImportSize(texture.PcResolution / 2);
            });
        }

        public void CrunchThemAll() {
            ForeachTexture(texture => {
                if (texture.CrunchPCTextureFormat != null && texture.PCFormat != null && texture.PCFormat != texture.CrunchPCTextureFormat) {
                    texture.ChangePCImporterFormat(texture.CrunchPCTextureFormat);
                }
            });
        }

        public void SetBestPCTexturesFormat() {
            ForeachTexture(texture => {
                if (texture.BestPCTextureFormat != null && texture.PCFormat != null && texture.PCFormat != texture.BestPCTextureFormat) {
                    texture.ChangePCImporterFormat(texture.BestPCTextureFormat);
                }
            });
        }

        public void SetBestAndroidTexturesFormat() {
            ForeachTexture(texture => {
                if (texture.BestAndroidTextureFormat != null && texture.AndroidFormat != null && texture.AndroidFormat != texture.BestAndroidTextureFormat) {
                    texture.ChangeAndroidImporterFormat(texture.BestAndroidTextureFormat);
                }
            });
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
            Selection.objects = materials.ToList().ConvertAll(meta => meta.Material).ToArray();
            EditorApplication.ExecuteMenuItem("Assets/Thry/Materials/Unlock All");
        }

        public void UpdateMaterials() {
            var poi = materials.Where(meta => meta.Poiyomi && !meta.ShaderLocked).ToList().ConvertAll(meta => meta.Material);
            poi.ForEach(mat => { mat.shader = Shader.Find(".poiyomi/Poiyomi Pro"); });
        }

        public void LockMaterials() {
            Selection.objects = materials.ToList().ConvertAll(meta => meta.Material).ToArray();
            EditorApplication.ExecuteMenuItem("Assets/Thry/Materials/Lock All");
        }

        private List<Material> GetRenderersMaterials() {
            var renderers = GameObject
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer
                    .gameObject
                    .GetComponentsInParent<Transform>(true)
                    .All(transform => !transform.CompareTag("EditorOnly"))
                );

            return renderers
                .SelectMany(r => r.sharedMaterials)
                .Where(material => material != null)
                .ToList();

        }

        private List<Material> GetDescriptorMaterials() {
            var materialsToReturn = new List<Material>();
            var descriptor = GameObject.GetComponent<VRCAvatarDescriptor>();

            if (descriptor == null) return materialsToReturn;

            var controllers = descriptor
                .baseAnimationLayers
                .Select(layer => layer.animatorController)
                .Where(controller => controller != null)
                .Distinct();

            foreach (var controller in controllers) {
                materialsToReturn.AddRange(GetMaterialsFromAnimatorController(controller));
            }

            return materialsToReturn;
        }

        private List<Material> GetVrcFuryMaterials() {
            var materialsToReturn = new List<Material>();

            var components = GameObject.GetComponentsInChildren<Component>(true)
                .Where(component => component != null)
                .Where(component => component
                    .gameObject
                    .GetComponentsInParent<Transform>(true)
                    .All(transform => !transform.CompareTag("EditorOnly"))
                )
                .Where(component => component.GetType().FullName == "VF.Model.VRCFury");

            foreach (var componentObject in components) {
                var component = new ObjectMeta(componentObject);
                var content = component.Get("content");

                content.Get("state").Get("actions").ForEach((action) => {
                    if (action.IsMaterialAction) {
                        var objRef = action.Get("mat").Get("objRef");

                        if (objRef.Object is Material material) {
                            materialsToReturn.Add(material);
                        }
                    }

                    if (action.IsFlipBookBuilderAction) {
                        action.Get("pages").ForEach((page) => {
                            page.Get("state").Get("actions").ForEach((subAction) => {
                                var objRef = subAction.Get("mat").Get("objRef");

                                if (objRef.Object is Material material) {
                                    materialsToReturn.Add(material);
                                }
                            });
                        });
                    }
                });

                content.Get("controllers").ForEach((controller) => {
                    if (controller.Get("controller").Get("objRef").Object is RuntimeAnimatorController animatorController) {
                        materialsToReturn.AddRange(GetMaterialsFromAnimatorController(animatorController));
                    }
                });
            }

            return materialsToReturn
                .Where(material => material != null)
                .Distinct()
                .ToList();
        }

        private static IEnumerable<Material> GetMaterialsFromAnimatorController(RuntimeAnimatorController controller) {
            if (controller == null) return Enumerable.Empty<Material>();

            return controller.animationClips
                .Where(clip => clip != null)
                .Distinct()
                .SelectMany(GetMaterialsFromAnimationClip);
        }

        private static IEnumerable<Material> GetMaterialsFromAnimationClip(AnimationClip clip) {
            if (clip == null) return Enumerable.Empty<Material>();

            return AnimationUtility
                .GetObjectReferenceCurveBindings(clip)
                .Where(binding =>
                    binding.isPPtrCurve &&
                    typeof(Renderer).IsAssignableFrom(binding.type) &&
                    binding.propertyName.StartsWith("m_Materials"))
                .SelectMany(binding => AnimationUtility.GetObjectReferenceCurve(clip, binding))
                .Select(reference => reference.value as Material)
                .Where(material => material != null);
        }

        private List<MaterialMeta> GetMaterials() {
            var materialsToReturn = new List<Material>();

            materialsToReturn.AddRange(GetRenderersMaterials());
            materialsToReturn.AddRange(GetDescriptorMaterials());
            materialsToReturn.AddRange(GetVrcFuryMaterials());

            var materialMetas = materialsToReturn
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
            textureMetas.Sort((t1, t2) => t1.Size.CompareTo(t2.Size));

            return textureMetas;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace Azzmurr.Utils {
    internal class MaterialManager : CommonEditorWindow {
        public void CreateGUI() {
            var root = CreateRootUIElement();

            var actions = CreateActionsGUI();
            var materials = CreateMaterialsListGUI();

            var avatarSelector = CreateAvatarSelectorField(_ => {
                if (Avatar == null) materials.itemsSource = null;
                if (Avatar != null) materials.itemsSource = Avatar.materials;
            });

            root.Add(avatarSelector);

            ToggleLists(false);

            var scrollView = new ScrollView(ScrollViewMode.Vertical) {
                style = {
                    flexDirection = FlexDirection.Column
                }
            };

            scrollView.Add(actions);
            scrollView.Add(materials);
            root.Add(scrollView);
        }

        private MultiColumnListView CreateActionsGUI() {
            CreateActionsListGUI();

            ActionsListView.itemsSource = new List<ActionGroup> {
                new() {
                    Name = "Avatar",
                    Actions = new List<Button> {
                        new(() => { DoAndRedraw(() => Avatar.Recalculate()); }) { text = "Recalculate" },
                    }
                },
                new() {
                    Name = "Thry",
                    Actions = new List<Button> {
                        new(() => { DoAndRedraw(() => Avatar.UnlockMaterials()); }) { text = "Unlock Materials", style = { maxWidth = 162 }},
                        new(() => { DoAndRedraw(() => Avatar.LockMaterials()); }) { text = "Lock Materials", style = { maxWidth = 150 }},
                    }
                },
                new() {
                    Name = "Poiyomi",
                    Actions = new List<Button>() {
                        new(() => { DoAndRedraw(() => Avatar.UpdateMaterials()); }) { text = "Update To Latest", style = { maxWidth = 162 }},
                    }
                },
                new() {
                    Name = "PC Textures",
                    Actions = new List<Button> {
                        new(() => { DoAndRedraw(() => Avatar.ChangeAllPCTexturesSize(1024)); }) { text = "-> 1k", style = { maxWidth = 50}},
                        new(() => { DoAndRedraw(() => Avatar.ChangeAllPCTexturesSize(2048)); }) { text = "-> 2k", style = { maxWidth = 50}},
                        new(() => { DoAndRedraw(() => Avatar.ChangeAllPCTexturesSize(4096)); }) { text = "-> 4k", style = { maxWidth = 50}},
                        new(() => { DoAndRedraw(() => Avatar.SetBestPCTexturesFormat()); }) { text = "Set Best Format", style = { maxWidth = 150 }},
                        new(() => { DoAndRedraw(() => Avatar.CrunchThemAll()); }) { text = "CRUNCH THEM ALL", style = { maxWidth = 150 }},
                    }
                },
                new() {
                    Name = "Android Textures",
                    Actions = new List<Button> {
                        new(() => { DoAndRedraw(() => Avatar.ChangeAllAndroidTexturesSize(1024)); }) { text = "-> 1k", style = { maxWidth = 50}},
                        new(() => { DoAndRedraw(() => Avatar.ChangeAllAndroidTexturesSize(2048)); }) { text = "-> 2k", style = { maxWidth = 50}},
                        new(() => { DoAndRedraw(() => Avatar.ChangeAllAndroidTexturesSize(4096)); }) { text = "-> 4k", style = { maxWidth = 50}},
                        new(() => { DoAndRedraw(() => Avatar.SetBestAndroidTexturesFormat()); }) { text = "Set Best Format", style = { maxWidth = 150 }},
                        new(() => { DoAndRedraw(() => Avatar.MakeTexturesReadyForAndroid()); }) { text = "Prepare for Android", style = { maxWidth = 150 }},
                        new(() => { DoAndRedraw(() => Avatar.CreateQuestMaterialPresets()); }) { text = "Create Quest Material Presets", style = { maxWidth = 200 }},
                    }
                },
            };

            return ActionsListView;
        }

        private MultiColumnListView CreateMaterialsListGUI() {
            CreateMainListGUI();

            MainListView.columns.Add(new Column {
                title = "Preview",
                width = 110,
                stretchable = false,
                resizable = false,
                makeCell = () => new VisualElement {
                    style = {
                        width = 90,
                        height = 90,
                    }
                },
                bindCell = (element, index) => {
                    var material = (MaterialMeta)MainListView.viewController.GetItemForIndex(index);
                    var preview = AssetPreview.GetAssetPreview(material.Material);

                    if (preview != null) {
                        element.style.backgroundImage = AssetPreview.GetAssetPreview(material.Material);
                        var clickable = new Clickable(_ => EditorGUIUtility.PingObject(material.Material));
                        element.AddManipulator(clickable);
                    }

                    if (AssetPreview.IsLoadingAssetPreviews()) {
                        MainListView.RefreshItem(index);
                    }
                }
            });

            MainListView.columns.Add(new Column {
                title = "Information",
                width = 230,
                stretchable = false,
                resizable = false,
                makeCell = () => {
                    var cell = new MultiColumnListView {
                        focusable = true,
                        showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                        showBorder = true,
                        reorderMode = ListViewReorderMode.Animated,
                        virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                    };

                    cell.columns.Add(new Column {
                        title = "Title",
                        width = 75,
                        makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft } },
                        bindCell = (element, index) => {
                            var label = (Label)element;
                            var info = (MaterialQuickInfo)cell.viewController.GetItemForIndex(index);
                            label.text = info.title;
                        }
                    });

                    cell.columns.Add(new Column {
                        title = "Value",
                        width = 150,
                        makeCell = () => new VisualElement(),
                        bindCell = (element, index) => {
                            var info = (MaterialQuickInfo)cell.viewController.GetItemForIndex(index);
                            element.Clear();
                            element.Add(info.Content);
                        }
                    });

                    return cell;
                },

                bindCell = (element, index) => {
                    var list = (MultiColumnListView)element;
                    var material = (MaterialMeta)MainListView.viewController.GetItemForIndex(index);
                    list.itemsSource = new List<MaterialQuickInfo> {
                        new() { title = "Material", Content = new ObjectField() { value = material.Material, objectType = typeof(Material), style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft } } },
                        new() { title = "Shader", Content = new Label(material.ShaderName) { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft } } },
                        new() {
                            title = "Locked",
                            Content = new Label(material.ShaderLockedString) {
                                style = {
                                    flexGrow = 1,
                                    unityTextAlign = TextAnchor.MiddleLeft,
                                    color = material.ShaderLockedError switch {
                                        null => Color.white,
                                        true => Color.red,
                                        false => Color.green
                                    }
                                }
                            }
                        },
                        new() {
                            title = "Version",
                            Content = new Label(material.ShaderVersion) {
                                style = {
                                    color = material.ShaderVersionError switch {
                                        null => Color.white,
                                        true => Color.red,
                                        false => Color.green
                                    }
                                }
                            }
                        }
                    };
                }
            });

            MainListView.columns.Add(new Column {
                title = "Actions",
                width = 100,
                stretchable = false,
                resizable = false,
                bindCell = (element, index) => {
                    var material = (MaterialMeta)MainListView.viewController.GetItemForIndex(index);
                    element.Clear();

                    element.Add(new Button(() => {
                        var window = (MaterialChecklist)GetWindow(typeof(MaterialChecklist));
                        window.titleContent = new GUIContent("Material Checklist");
                        window.SetMaterial(material.Material);
                    }) { text = "Checklist" });

                    element.Add(new Button(() => DoAndRedraw(index, material.UnlockMaterial))
                        { text = "Unlock" });

                    element.Add(new Button(() => DoAndRedraw(index, material.UpdateMaterial))
                        { text = "Update" });

                    element.Add(new Button(() => DoAndRedraw(index, material.LockMaterial))
                        { text = "Lock" });
                }
            });

            MainListView.columns.Add(new Column {
                title = "Textures",
                width = Length.Auto(),
                minWidth = 90,
                stretchable = true,
                resizable = true,
                makeCell = () => new VisualElement
                    { style = { flexDirection = FlexDirection.Row, flexGrow = 1, flexShrink = 0 } },
                bindCell = (element, index) => {
                    var material = (MaterialMeta)MainListView.viewController.GetItemForIndex(index);
                    element.Clear();
                    material.Textures
                        .ConvertAll((texture) => {
                            var element = new VisualElement {
                                style = {
                                    width = 90,
                                    height = 90,
                                    marginRight = 8,
                                    flexShrink = 0,
                                    backgroundImage = texture.GetTextureBackground(),
                                }
                            };

                            var clickable = new Clickable(e => EditorGUIUtility.PingObject(texture.Texture));
                            element.AddManipulator(clickable);

                            return element;
                        })
                        .ForEach(element.Add);
                }
            });

            return MainListView;
        }

        [MenuItem("Azzmurr/Material Manager")]
        public static void Init() {
            var window = (MaterialManager)GetWindow(typeof(MaterialManager));
            window.titleContent = new GUIContent("Material Manager");
            window.SetAvatar(Selection.activeGameObject);
            window.Show();
        }

        [MenuItem("GameObject/Azzmurr/Material Manager", true)]
        public static bool CanShowFromSelection() {
            return Selection.activeGameObject != null;
        }

        [MenuItem("GameObject/Azzmurr/Material Manager", false)]
        public static void ShowFromSelection() {
            var window = (MaterialManager)GetWindow(typeof(MaterialManager));
            window.titleContent = new GUIContent("Material Manager");
            window.SetAvatar(Selection.activeGameObject);
            window.Show();
        }

        public static void Init(GameObject avatar) {
            var window = (MaterialManager)GetWindow(typeof(MaterialManager));
            window.titleContent = new GUIContent("Material Manager");
            window.SetAvatar(avatar);
            window.Show();
        }
    }
}

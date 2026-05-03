using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace Azzmurr.Utils {
    internal class MaterialManager : EditorWindow {
        private AvatarMeta _avatar;

        public AvatarMeta Avatar {
            get => _avatar;
            set {
                _avatar = value;
                AvatarSelector.value = _avatar?.GameObject;
            }
        }

        private ObjectField AvatarSelector =>
            rootVisualElement.Q<ObjectField>("AvatarGameObject");

        public void CreateGUI() {
            var root = rootVisualElement;
            root.style.paddingRight = 8;
            root.style.paddingLeft = 8;

            var avatarSelector = new VisualElement { style = { flexShrink = 0 } };
            var avatarGameObjectField = new ObjectField {
                objectType = typeof(GameObject),
                value = _avatar?.GameObject,
                name = "AvatarGameObject",
                label = "Avatar: ",
                style = {
                    flexShrink = 0,
                    flexGrow = 1,
                }
            };

            avatarSelector.Add(avatarGameObjectField);
            root.Add(avatarSelector);

            var actions = CreateActionsGUI();
            var materials = CreateMaterialsListGUI();

            actions.SetEnabled(false);
            materials.SetEnabled(false);

            avatarGameObjectField.RegisterValueChangedCallback(changeEvent => {
                _avatar = changeEvent.newValue != null ? new AvatarMeta(changeEvent.newValue as GameObject) : null;
                actions.SetEnabled(_avatar != null);
                materials.SetEnabled(_avatar != null);

                if (_avatar == null) materials.itemsSource = null;
                if (_avatar != null) materials.itemsSource = _avatar.materials;
            });


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
            var actions = new MultiColumnListView {
                name = "Actions",
                focusable = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                showBorder = true,
                reorderMode = ListViewReorderMode.Animated,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                style = {
                    marginTop = 8,
                    flexShrink = 0,
                }
            };

            actions.columns.Add(new Column {
                title = "Type",
                width = 80,
                makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft } },
                bindCell = (element, index) => {
                    var label = (Label)element;
                    var actionGroup = (ActionGroup)actions.viewController.GetItemForIndex(index);
                    label.text = actionGroup.Name;
                }
            });

            actions.columns.Add(new Column {
                title = "Actions",

                width = 400,
                makeCell = () => new VisualElement { style = { flexDirection = FlexDirection.Row } },
                bindCell = (element, index) => {
                    var actionGroup = (ActionGroup)actions.viewController.GetItemForIndex(index);
                    actionGroup.Actions
                        .ToList()
                        .ConvertAll((action) => {
                            action.style.flexGrow = 1;
                            return action;
                        })
                        .ForEach(element.Add);
                }
            });

            actions.itemsSource = new List<ActionGroup> {
                new() {
                    Name = "Avatar",
                    Actions = new List<Button> {
                        new(() => { DoAndRedraw(() => _avatar.Recalculate()); }) { text = "Recalculate" },
                    }
                },
                new() {
                    Name = "Poiyomi",
                    Actions = new List<Button> {
                        new(() => { DoAndRedraw(() => _avatar.UnlockMaterials()); }) { text = "Unlock" },
                        new(() => { DoAndRedraw(() => _avatar.UpdateMaterials()); }) { text = "Update" },
                        new(() => { DoAndRedraw(() => _avatar.LockMaterials()); }) { text = "Lock" },
                    }
                },
                new() {
                    Name = "Textures",
                    Actions = new List<Button> {
                        new(() => { DoAndRedraw(() => _avatar.MakeAllTextures2K()); }) { text = "-> 2k" },
                        new(() => { DoAndRedraw(() => _avatar.MakeTexturesReadyForAndroid()); })
                            { text = "Prepare for Android" },
                        new(() => { DoAndRedraw(() => _avatar.CrunchTextures()); }) { text = "Crunch" },
                    }
                },
                new() {
                    Name = "Quest",
                    Actions = new List<Button> {
                        new(() => { DoAndRedraw(() => _avatar.CreateQuestMaterialPresets()); })
                            { text = "Create Quest Presets" },
                    }
                },
            };

            return actions;
        }

        private MultiColumnListView CreateMaterialsListGUI() {
            var materialsListGUI = new MultiColumnListView {
                name = "Materials List",
                focusable = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                showBorder = true,
                reorderMode = ListViewReorderMode.Animated,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                style = {
                    marginTop = 8,
                }
            };

            materialsListGUI.columns.Add(new Column {
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
                    var material = (MaterialMeta)materialsListGUI.viewController.GetItemForIndex(index);
                    var preview = AssetPreview.GetAssetPreview(material.Material);

                    if (preview != null) {
                        element.style.backgroundImage = AssetPreview.GetAssetPreview(material.Material);
                        var clickable = new Clickable(e => EditorGUIUtility.PingObject(material.Material));
                        element.AddManipulator(clickable);
                    }

                    if (AssetPreview.IsLoadingAssetPreviews()) {
                        materialsListGUI.RefreshItem(index);
                    }
                }
            });

            materialsListGUI.columns.Add(new Column {
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
                    var material = (MaterialMeta)materialsListGUI.viewController.GetItemForIndex(index);
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

            materialsListGUI.columns.Add(new Column {
                title = "Actions",
                width = 100,
                stretchable = false,
                resizable = false,
                bindCell = (element, index) => {
                    var material = (MaterialMeta)materialsListGUI.viewController.GetItemForIndex(index);
                    element.Clear();

                    element.Add(new Button(() => {
                        var window = (MaterialChecklist)GetWindow(typeof(MaterialChecklist));
                        window.titleContent = new GUIContent("Material Checklist");
                        window.Material = material;
                    }) { text = "Checklist" });

                    element.Add(new Button(() => DoAndRedraw(materialsListGUI, index, () => material.UnlockMaterial()))
                        { text = "Unlock" });

                    element.Add(new Button(() => DoAndRedraw(materialsListGUI, index, () => material.UpdateMaterial()))
                        { text = "Update" });

                    element.Add(new Button(() => DoAndRedraw(materialsListGUI, index, () => material.LockMaterial()))
                        { text = "Lock" });
                }
            });

            materialsListGUI.columns.Add(new Column {
                title = "Textures",
                width = Length.Auto(),
                minWidth = 90,
                stretchable = true,
                resizable = true,
                makeCell = () => new VisualElement
                    { style = { flexDirection = FlexDirection.Row, flexGrow = 1, flexShrink = 0 } },
                bindCell = (element, index) => {
                    var material = (MaterialMeta)materialsListGUI.viewController.GetItemForIndex(index);
                    element.Clear();
                    material.Textures
                        .ConvertAll((texture) => {
                            var element = new VisualElement {
                                style = {
                                    width = 90,
                                    height = 90,
                                    marginRight = 8,
                                    flexShrink = 0,
                                    backgroundImage = Background.FromTexture2D((Texture2D)texture.Texture),
                                }
                            };

                            var clickable = new Clickable(e => EditorGUIUtility.PingObject(texture.Texture));
                            element.AddManipulator(clickable);

                            return element;
                        })
                        .ForEach(element.Add);
                }
            });

            return materialsListGUI;
        }

        private void DoAndRedraw(Action action) {
            var list = rootVisualElement.Q<MultiColumnListView>("Materials List");
            action.Invoke();
            list.RefreshItems();
        }

        private void DoAndRedraw(MultiColumnListView view, Action action) {
            action.Invoke();
            view.RefreshItems();
        }

        private void DoAndRedraw(MultiColumnListView view, int index, Action action) {
            action.Invoke();
            view.RefreshItem(index);
        }

        [MenuItem("Azzmurr/Material Manager")]
        public static void Init() {
            var window = (MaterialManager)GetWindow(typeof(MaterialManager));
            window.titleContent = new GUIContent("Material Manager");

            if (Selection.activeGameObject) {
                window.Avatar = new AvatarMeta(Selection.activeGameObject);
            }

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
            window.Avatar = new AvatarMeta(Selection.activeGameObject);
            window.Show();
        }

        public static void Init(GameObject avatar) {
            var window = (MaterialManager)GetWindow(typeof(MaterialManager));
            window.titleContent = new GUIContent("Material Manager");
            window.Avatar = new AvatarMeta(Selection.activeGameObject);
            window.Show();
        }
    }
}
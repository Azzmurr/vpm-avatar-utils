using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Azzmurr.Utils {
    internal class TextureManager : EditorWindow {
        private readonly List<TextureImporterFormat> _compressionFormatOptions = new() {
            TextureImporterFormat.Automatic,
            TextureImporterFormat.BC7,
            TextureImporterFormat.DXT1,
            TextureImporterFormat.DXT5,
            TextureImporterFormat.DXT1Crunched,
            TextureImporterFormat.DXT5Crunched
        };

        private readonly Dictionary<int, EventCallback<ChangeEvent<TextureImporterFormat>>> _registeredCallbacksFormat =
            new();

        private readonly Dictionary<int, EventCallback<ChangeEvent<int>>> _registeredCallbacksInt = new();

        private readonly List<int> _textureSizeOptions = new() { 128, 256, 512, 1024, 2048, 4096, 8192 };

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
            root.Add(actions);

            var textures = CreateTexturesGUI();
            root.Add(textures);

            avatarGameObjectField.RegisterValueChangedCallback(changeEvent => {
                _avatar = changeEvent.newValue != null ? new AvatarMeta(changeEvent.newValue as GameObject) : null;
                actions.SetEnabled(_avatar != null);
                textures.SetEnabled(_avatar != null);

                if (_avatar == null) textures.itemsSource = null;
                if (_avatar != null) textures.itemsSource = _avatar.textures;
            });
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
                    Name = "Textures",
                    Actions = new List<Button> {
                        new(() => { DoAndRedraw(() => _avatar.MakeAllTextures2K()); }) { text = "-> 2k" },
                        new(() => { DoAndRedraw(() => _avatar.MakeTexturesReadyForAndroid()); })
                            { text = "Prepare for Android" },
                        new(() => { DoAndRedraw(() => _avatar.CrunchTextures()); }) { text = "Crunch" },
                        new(() => {
                            DoAndRedraw((textures) => {
                                var list = _avatar.textures;
                                list.Sort((t1, t2) => t1.Size > t2.Size ? -1 : 1);

                                textures.itemsSource = list;
                            });
                        }) { text = "Sort by size" },
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

        private MultiColumnListView CreateTexturesGUI() {
            var textureListGUI = new MultiColumnListView {
                name = "Textures List",
                focusable = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                showBorder = true,
                reorderMode = ListViewReorderMode.Animated,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                style = {
                    marginTop = 8,
                }
            };

            textureListGUI.columns.Add(new Column {
                title = "Texture",
                width = 200,
                stretchable = true,
                resizable = true,
                makeCell = () => new ObjectField {
                    objectType = typeof(Texture2D),
                },
                bindCell = (element, index) => {
                    var field = (ObjectField)element;
                    var texture = (TextureMeta)textureListGUI.viewController.GetItemForIndex(index);
                    field.value = texture.Texture;
                }
            });

            textureListGUI.columns.Add(new Column {
                title = "Materials",
                width = 100,
                stretchable = false,
                resizable = true,
                makeCell = () => new Foldout { text = "Materials", value = false },
                bindCell = (element, index) => {
                    var foldout = (Foldout)element;
                    var texture = (TextureMeta)textureListGUI.viewController.GetItemForIndex(index);
                    element.Clear();

                    _avatar.ForeachTextureMaterial(texture, material => {
                        var materialField = new ObjectField {
                            objectType = typeof(Material),
                            value = material,
                            style = {
                                flexGrow = 1,
                                flexShrink = 1,
                            }
                        };
                        foldout.Add(materialField);
                    });
                }
            });

            textureListGUI.columns.Add(new Column {
                title = "Size",
                width = 100,
                stretchable = false,
                resizable = false,
                makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft } },
                bindCell = (element, index) => {
                    var label = (Label)element;
                    var texture = (TextureMeta)textureListGUI.viewController.GetItemForIndex(index);
                    label.text = texture.SizeString;
                }
            });

            textureListGUI.columns.Add(new Column {
                title = "PC Resolution",
                width = 100,
                stretchable = false,
                resizable = false,
                makeCell = () => new PopupField<int> {
                    choices = _textureSizeOptions,
                },
                bindCell = (element, index) => {
                    var popup = (PopupField<int>)element;
                    var texture = (TextureMeta)textureListGUI.viewController.GetItemForIndex(index);
                    popup.SetValueWithoutNotify(texture.PcResolution);
                    popup.SetEnabled(texture.TextureWithChangeableResolution);

                    RegisterCallBack(popup, (e) => {
                        texture.ChangeImportSize(e.newValue);
                        DoAndRedraw(textureListGUI, index, () => _avatar.Recalculate());
                    });
                },
                unbindCell = (element, index) => {
                    var popup = (PopupField<int>)element;
                    UnregisterCallBack(popup);
                }
            });

            textureListGUI.columns.Add(new Column {
                title = "Android Resolution",
                width = 100,
                stretchable = false,
                resizable = false,
                makeCell = () => new PopupField<int> {
                    choices = _textureSizeOptions,
                },
                bindCell = (element, index) => {
                    var popup = (PopupField<int>)element;
                    var texture = (TextureMeta)textureListGUI.viewController.GetItemForIndex(index);
                    popup.SetValueWithoutNotify(texture.AndroidResolution);
                    popup.SetEnabled(texture.TextureWithChangeableResolution);

                    RegisterCallBack(popup, (e) => {
                        texture.ChangeImportSizeAndroid(e.newValue);
                        DoAndRedraw(textureListGUI, index, () => _avatar.Recalculate());
                    });
                },
                unbindCell = (element, index) => {
                    var popup = (PopupField<int>)element;
                    UnregisterCallBack(popup);
                }
            });

            textureListGUI.columns.Add(new Column {
                title = "Format",
                width = 150,
                stretchable = false,
                resizable = true,
                makeCell = () => new PopupField<TextureImporterFormat> {
                    choices = _compressionFormatOptions,
                },
                bindCell = (element, index) => {
                    var popup = (PopupField<TextureImporterFormat>)element;
                    var texture = (TextureMeta)textureListGUI.viewController.GetItemForIndex(index);
                    popup.SetValueWithoutNotify((TextureImporterFormat)texture.Format);
                    popup.SetEnabled(texture.TextureWithChangeableFormat);

                    RegisterCallBack(popup, (e) => {
                        texture.ChangeImporterFormat(e.newValue);
                        DoAndRedraw(textureListGUI, index, () => _avatar.Recalculate());
                    });
                },
                unbindCell = (element, index) => {
                    var popup = (PopupField<TextureImporterFormat>)element;
                    UnregisterCallBack(popup);
                }
            });

            textureListGUI.columns.Add(new Column {
                title = "Actions",
                minWidth = 200,
                stretchable = true,
                resizable = true,
                bindCell = (element, index) => {
                    element.Clear();
                    var texture = (TextureMeta)textureListGUI.viewController.GetItemForIndex(index);

                    if (texture.Poiyomi) {
                        element.Add(new Label { text = "Poiyomi textures are ignored and can't be changed", style = { flexGrow = 1 } });
                    }

                    if (texture.BetterTextureFormat != null) {
                        element.Add(new Button(() => {
                            var changeFormatPopup = EditorUtility.DisplayDialog(
                                "Confirm Compression Format Change!",
                                $"You are about to change the compression format of texture '{texture.Texture.name}' from {texture.Format} => {texture.BetterTextureFormat}\n\n" +
                                $"If you wish to return this texture's compression to {texture.FormatString}, you will have to do so manually as this action is not undo-able.\n\nAre you sure?",
                                "Yes",
                                "No"
                            );

                            if (!changeFormatPopup) return;

                            texture.ChangeImporterFormat((TextureImporterFormat)texture.BetterTextureFormat);
                            DoAndRedraw(textureListGUI, index, () => _avatar.Recalculate());
                        }) { text = $"{texture.BetterTextureFormat} → -{texture.SavedSizeWithBetterTextureFormat}" });
                    }

                    if (texture.TextureTooBig) {
                        element.Add(new Button(() => {
                            texture.ChangeImportSize(2048);
                            DoAndRedraw(textureListGUI, index, () => _avatar.Recalculate());
                        }) { text = $"2k → -{texture.SaveSizeWithSmallerTexture}" });
                    }
                }
            });

            return textureListGUI;
        }

        private void RegisterCallBack(PopupField<int> field, Action<ChangeEvent<int>> action) {
            EventCallback<ChangeEvent<int>> callback = evt => action(evt);
            field.RegisterValueChangedCallback(callback);
            _registeredCallbacksInt.Add(field.GetHashCode(), callback);
        }

        private void RegisterCallBack(PopupField<TextureImporterFormat> field,
            Action<ChangeEvent<TextureImporterFormat>> action) {
            EventCallback<ChangeEvent<TextureImporterFormat>> callback = evt => action(evt);
            field.RegisterValueChangedCallback(callback);
            _registeredCallbacksFormat.Add(field.GetHashCode(), callback);
        }

        private void UnregisterCallBack(PopupField<int> field) {
            if (!_registeredCallbacksInt.TryGetValue(field.GetHashCode(), out var callback)) return;
            field.UnregisterValueChangedCallback(callback);
            _registeredCallbacksInt.Remove(field.GetHashCode());
        }

        private void UnregisterCallBack(PopupField<TextureImporterFormat> field) {
            if (!_registeredCallbacksFormat.TryGetValue(field.GetHashCode(), out var callback)) return;
            field.UnregisterValueChangedCallback(callback);
            _registeredCallbacksFormat.Remove(field.GetHashCode());
        }


        private void DoAndRedraw(Action action) {
            var list = rootVisualElement.Q<MultiColumnListView>("Textures List");
            action.Invoke();
            list.RefreshItems();
        }

        private void DoAndRedraw(Action<MultiColumnListView> action) {
            var list = rootVisualElement.Q<MultiColumnListView>("Textures List");
            action.Invoke(list);
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

        [MenuItem("Azzmurr/Texture Manager")]
        public static void Init() {
            var window = (TextureManager)GetWindow(typeof(TextureManager));
            window.titleContent = new GUIContent("Texture Manager");
            if (Selection.activeGameObject) {
                window.Avatar = new AvatarMeta(Selection.activeGameObject);
            }

            window.Show();
        }

        [MenuItem("GameObject/Azzmurr/Texture Manager", true, 0)]
        public static bool CanShowFromSelection() {
            return Selection.activeGameObject != null;
        }

        [MenuItem("GameObject/Azzmurr/Texture Manager", false, 0)]
        public static void ShowFromSelection() {
            var window = (TextureManager)GetWindow(typeof(TextureManager));
            window.titleContent = new GUIContent("Texture Manager");
            window.Avatar = new AvatarMeta(Selection.activeGameObject);
            window.Show();
        }

        public static void Init(GameObject avatar) {
            var window = (TextureManager)GetWindow(typeof(TextureManager));
            window.titleContent = new GUIContent("Texture Manager");
            window.Avatar = new AvatarMeta(Selection.activeGameObject);
            window.Show();
        }
    }
}
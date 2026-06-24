using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Azzmurr.Utils {
    internal class TextureManager : EditorWindow {
        private readonly List<TextureImporterFormat> _compressionPCFormatOptions = new() {
            TextureImporterFormat.Automatic,
            TextureImporterFormat.BC7,
            TextureImporterFormat.BC5,
            TextureImporterFormat.DXT1,
            TextureImporterFormat.DXT5,
            TextureImporterFormat.DXT1Crunched,
            TextureImporterFormat.DXT5Crunched
        };

        private readonly List<TextureImporterFormat> _compressionAndroidFormatOptions = new() {
            TextureImporterFormat.Automatic,
            TextureImporterFormat.ASTC_6x6,
            TextureImporterFormat.ASTC_4x4,
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
                Actions.SetEnabled(value != null);
            }
        }

        private ObjectField AvatarSelector =>
            rootVisualElement.Q<ObjectField>("AvatarGameObject");

        private MultiColumnListView Actions => rootVisualElement.Q<MultiColumnListView>("Actions");

        private MultiColumnListView TexturesListView => rootVisualElement.Q<MultiColumnListView>("Textures List");

        private Label TexturesMemory => rootVisualElement.Q<Label>("Textures Memory");

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

            root.Add(CreateTexturesMemoryLabel());

            var textures = CreateTexturesGUI();
            root.Add(textures);

            avatarGameObjectField.RegisterValueChangedCallback(changeEvent => {
                _avatar = changeEvent.newValue != null ? new AvatarMeta(changeEvent.newValue as GameObject) : null;
                actions.SetEnabled(_avatar != null);
                textures.SetEnabled(_avatar != null);

                if (_avatar == null) textures.itemsSource = null;
                if (_avatar != null) textures.itemsSource = _avatar.textures;

                refreshTexturesMemory();
                OnSortingChanged();
            });
        }

        private Label CreateTexturesMemoryLabel() {
            var label = new Label {
                name = "Textures Memory",
                style = {
                    marginTop = 8,
                }
            };

            return label;
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

            actions.SetEnabled(false);

            actions.columns.Add(new Column {
                title = "Type",
                width = 120,
                makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft } },
                bindCell = (element, index) => {
                    var label = (Label)element;
                    var actionGroup = (ActionGroup)actions.viewController.GetItemForIndex(index);
                    label.text = actionGroup.Name;
                }
            });

            actions.columns.Add(new Column {
                title = "Actions",
                minWidth = 800,
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
                        new(() => { DoAndRedraw(() => {
                            _avatar.Recalculate();
                            OnSortingChanged();
                        }); }) { text = "Reload" },
                    }
                },
                new() {
                    Name = "PC Textures",
                    Actions = new List<Button> {
                        new(() => { DoAndRedraw(() => _avatar.ChangeAllPCTexturesSize(1024)); }) { text = "-> 1k", style = { maxWidth = 50 } },
                        new(() => { DoAndRedraw(() => _avatar.ChangeAllPCTexturesSize(2048)); }) { text = "-> 2k", style = { maxWidth = 50 } },
                        new(() => { DoAndRedraw(() => _avatar.ChangeAllPCTexturesSize(4096)); }) { text = "-> 4k", style = { maxWidth = 50 } },
                        new(() => { DoAndRedraw(() => _avatar.SetBestPCTexturesFormat()); }) { text = "Set Best Format", style = { maxWidth = 150 } },
                        new(() => { DoAndRedraw(() => _avatar.CrunchThemAll()); }) { text = "CRUNCH THEM ALL", style = { maxWidth = 150 } },
                    }
                },
                new() {
                    Name = "Android Textures",
                    Actions = new List<Button> {
                        new(() => { DoAndRedraw(() => _avatar.ChangeAllAndroidTexturesSize(1024)); }) { text = "-> 1k", style = { maxWidth = 50 } },
                        new(() => { DoAndRedraw(() => _avatar.ChangeAllAndroidTexturesSize(2048)); }) { text = "-> 2k", style = { maxWidth = 50 } },
                        new(() => { DoAndRedraw(() => _avatar.ChangeAllAndroidTexturesSize(4096)); }) { text = "-> 4k", style = { maxWidth = 50 } },
                        new(() => { DoAndRedraw(() => _avatar.SetBestAndroidTexturesFormat()); }) { text = "Set Best Format", style = { maxWidth = 150 } },
                        new(() => { DoAndRedraw(() => _avatar.MakeTexturesReadyForAndroid()); }) { text = "Prepare for Android", style = { maxWidth = 150 } },
                        new(() => { DoAndRedraw(() => _avatar.CreateQuestMaterialPresets()); }) { text = "Create Quest Material Presets", style = { maxWidth = 200 } },
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
                sortingEnabled = true,
                sortColumnDescriptions = {
                    new SortColumnDescription("Size", SortDirection.Descending),
                },
                style = {
                    marginTop = 8,
                }
            };

            textureListGUI.columnSortingChanged += OnSortingChanged;

            textureListGUI.columns.Add(new Column {
                title = "Texture",
                name = "Texture",
                width = 200,
                stretchable = true,
                resizable = true,
                sortable = true,
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
                title = "Property Name",
                name = "Property Name",
                width = 100,
                resizable = true,
                sortable = true,
                makeCell = () => new Label(),
                bindCell = (element, index) => {
                    var field = (Label)element;
                    var texture = (TextureMeta)textureListGUI.viewController.GetItemForIndex(index);
                    field.text = texture.PropertyName;
                }
            });

            textureListGUI.columns.Add(new Column {
                title = "Materials",
                width = 100,
                stretchable = false,
                resizable = true,
                sortable = false,
                makeCell = () => new Foldout { text = "Materials", value = false },
                bindCell = (element, index) => {
                    var foldout = (Foldout)element;
                    var texture = (TextureMeta)textureListGUI.viewController.GetItemForIndex(index);
                    element.Clear();
                    var count = 0;

                    _avatar.ForeachTextureMaterial(texture, material => {
                        count++;
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

                    foldout.text = $"Materials ({count})";
                }
            });

            textureListGUI.columns.Add(new Column {
                title = "Size",
                name = "Size",
                width = 100,
                stretchable = false,
                resizable = false,
                sortable = true,
                makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft } },
                bindCell = (element, index) => {
                    var label = (Label)element;
                    var texture = (TextureMeta)textureListGUI.viewController.GetItemForIndex(index);
                    label.text = texture.SizeString;
                }
            });

            textureListGUI.columns.Add(new Column {
                title = "Default Resolution",
                name = "Default Resolution",
                width = 100,
                stretchable = false,
                resizable = false,
                sortable = true,
                makeCell = () => new PopupField<int> {
                    choices = _textureSizeOptions,
                },
                bindCell = (element, index) => {
                    var popup = (PopupField<int>)element;
                    var texture = (TextureMeta)textureListGUI.viewController.GetItemForIndex(index);
                    popup.SetValueWithoutNotify(texture.DefaultResolution);
                    popup.SetEnabled(texture.TextureWithChangeableResolution);

                    RegisterCallBack(popup, (e) => {
                        texture.ChangeDefaultImportSize(e.newValue);
                        DoAndRedraw(() => _avatar.Recalculate());
                    });
                },
                unbindCell = (element, index) => {
                    var popup = (PopupField<int>)element;
                    UnregisterCallBack(popup);
                }
            });

            textureListGUI.columns.Add(new Column {
                title = "PC Resolution",
                name = "PC Resolution",
                width = 100,
                stretchable = false,
                resizable = false,
                sortable = true,
                makeCell = () => new PopupField<int> {
                    choices = _textureSizeOptions,
                },
                bindCell = (element, index) => {
                    var popup = (PopupField<int>)element;
                    var texture = (TextureMeta)textureListGUI.viewController.GetItemForIndex(index);
                    popup.SetValueWithoutNotify(texture.PcResolution);
                    popup.SetEnabled(texture.TextureWithChangeableResolution);

                    RegisterCallBack(popup, (e) => {
                        texture.ChangePCImportSize(e.newValue);
                        DoAndRedraw(() => _avatar.Recalculate());
                    });
                },
                unbindCell = (element, index) => {
                    var popup = (PopupField<int>)element;
                    UnregisterCallBack(popup);
                }
            });

            textureListGUI.columns.Add(new Column {
                title = "Android Resolution",
                name = "Android Resolution",
                width = 100,
                stretchable = false,
                resizable = false,
                sortable = true,
                makeCell = () => new PopupField<int> {
                    choices = _textureSizeOptions,
                },
                bindCell = (element, index) => {
                    var popup = (PopupField<int>)element;
                    var texture = (TextureMeta)textureListGUI.viewController.GetItemForIndex(index);
                    popup.SetValueWithoutNotify(texture.AndroidResolution);
                    popup.SetEnabled(texture.TextureWithChangeableResolution);

                    RegisterCallBack(popup, (e) => {
                        texture.ChangeAndroidImportSize(e.newValue);
                        DoAndRedraw(() => _avatar.Recalculate());
                    });
                },
                unbindCell = (element, index) => {
                    var popup = (PopupField<int>)element;
                    UnregisterCallBack(popup);
                }
            });

            textureListGUI.columns.Add(new Column {
                title = "PC Format",
                name = "PC Format",
                width = 170,
                stretchable = false,
                resizable = true,
                sortable = true,
                makeCell = () => new PopupField<TextureImporterFormat> {
                    choices = _compressionPCFormatOptions,
                },
                bindCell = (element, index) => {
                    var popup = (PopupField<TextureImporterFormat>)element;
                    var texture = (TextureMeta)textureListGUI.viewController.GetItemForIndex(index);

                    if (texture.PCFormat != null) {
                        popup.SetValueWithoutNotify(texture.PCFormat.Value);
                    }

                    popup.SetEnabled(texture.TextureWithChangeableFormat);

                    RegisterCallBack(popup, (e) => {
                        texture.ChangePCImporterFormat(e.newValue);
                        DoAndRedraw(() => _avatar.Recalculate());
                    });
                },
                unbindCell = (element, index) => {
                    var popup = (PopupField<TextureImporterFormat>)element;
                    UnregisterCallBack(popup);
                }
            });

            textureListGUI.columns.Add(new Column {
                title = "Android Format",
                name = "Android Format",
                width = 170,
                stretchable = false,
                resizable = true,
                sortable = true,
                makeCell = () => new PopupField<TextureImporterFormat> {
                    choices = _compressionAndroidFormatOptions,
                },
                bindCell = (element, index) => {
                    var popup = (PopupField<TextureImporterFormat>)element;
                    var texture = (TextureMeta)textureListGUI.viewController.GetItemForIndex(index);

                    if (texture.AndroidFormat != null) {
                        popup.SetValueWithoutNotify(texture.AndroidFormat.Value);
                    }

                    popup.SetEnabled(texture.TextureWithChangeableFormat);

                    RegisterCallBack(popup, (e) => {
                        texture.ChangeAndroidImporterFormat(e.newValue);
                        DoAndRedraw(() => _avatar.Recalculate());
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
                sortable = false,
                bindCell = (element, index) => {
                    element.Clear();
                    var texture = (TextureMeta)textureListGUI.viewController.GetItemForIndex(index);

                    if (texture.Poiyomi) {
                        element.Add(new Label { text = "Poiyomi textures are ignored and can't be changed", style = { flexGrow = 1 } });
                    }

                    if (!texture.PCResolutionEqualsDefault) {
                        element.Add(new Button(() => {
                            DoAndRedraw(() => texture.ChangePCImportSize(texture.DefaultResolution));
                        }) { text = "Sync PC and Default resolutions", style = { flexGrow = 1 } });
                    }

                    if (texture.TextureTooBig) {
                        element.Add(new Button(() => {
                            texture.ChangePCImportSize(2048);
                            DoAndRedraw(() => _avatar.Recalculate());
                        }) { text = $"2k → -{texture.SaveSizeWithSmallerTexture}" });
                    }
                }
            });

            return textureListGUI;
        }


        private void OnSortingChanged() {
            var sortedColumns = TexturesListView.sortedColumns.ToList();
            if (sortedColumns.Count == 0) return;

            var primary = sortedColumns[0];

            _avatar.textures.Sort((a, b) => {
                var result = primary.columnName switch {
                    "Texture" => string.Compare(a.Texture.name, b.Texture.name, StringComparison.Ordinal),
                    "Property Name" => string.Compare(a.PropertyName, b.PropertyName, StringComparison.Ordinal),
                    "Size" => a.Size.CompareTo(b.Size),
                    "Default Resolution" => a.DefaultResolution.CompareTo(b.DefaultResolution),
                    "PC Resolution" => a.PcResolution.CompareTo(b.PcResolution),
                    "Android Resolution" => a.AndroidResolution.CompareTo(b.AndroidResolution),
                    "PC Format" => (a.PCFormat.HasValue ? (int)a.PCFormat.Value : int.MaxValue)
                        .CompareTo(b.PCFormat.HasValue ? (int)b.PCFormat.Value : int.MaxValue),
                    "Android Format" => (a.AndroidFormat.HasValue ? (int)a.AndroidFormat.Value : int.MaxValue)
                        .CompareTo(b.AndroidFormat.HasValue ? (int)b.AndroidFormat.Value : int.MaxValue),
                    _ => 0
                };
                return primary.direction == SortDirection.Descending ? -result : result;
            });

            TexturesListView.RefreshItems();
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
            action.Invoke();
            OnSortingChanged();
            refreshTexturesMemory();
        }

        private void refreshTexturesMemory() {
            if (_avatar != null) TexturesMemory.text = $"Textures Memory: {_avatar.GetTexturesMemory()}";
            if (_avatar == null) TexturesMemory.text = $"No Avatar Selected";
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

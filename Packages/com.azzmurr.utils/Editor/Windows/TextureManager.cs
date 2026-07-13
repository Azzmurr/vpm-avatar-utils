using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Azzmurr.Utils {
    internal class TextureManager : CommonEditorWindow {
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

        private readonly List<int> _textureSizeOptions = new() { 128, 256, 512, 1024, 2048, 4096, 8192 };



        private Label TexturesMemory => rootVisualElement.Q<Label>("Textures Memory");

        public void CreateGUI() {
            var root = CreateRootUIElement();
            var avatarSelector = CreateAvatarSelectorField(_ => {
                MainListView.itemsSource = Avatar?.textures;

                refreshTexturesMemory();
                OnSortingChanged();
            });


            root.Add(avatarSelector);

            var actions = CreateActionsGUI();
            root.Add(actions);

            root.Add(CreateTexturesMemoryLabel());

            var textures = CreateTexturesGUI();
            root.Add(textures);
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
            CreateActionsListGUI();

            ActionsListView.itemsSource = new List<ActionGroup> {
                new() {
                    Name = "Avatar",
                    Actions = new List<Button> {
                        new(() => { DoAndRedraw(() => {
                            Avatar.Recalculate();
                            OnSortingChanged();
                        }); }) { text = "Reload" },
                    }
                },
                new() {
                    Name = "PC Textures",
                    Actions = new List<Button> {
                        new(() => { DoAndRedraw(() => Avatar.ChangeAllPCTexturesSize(1024)); }) { text = "-> 1k", style = { maxWidth = 50 } },
                        new(() => { DoAndRedraw(() => Avatar.ChangeAllPCTexturesSize(2048)); }) { text = "-> 2k", style = { maxWidth = 50 } },
                        new(() => { DoAndRedraw(() => Avatar.ChangeAllPCTexturesSize(4096)); }) { text = "-> 4k", style = { maxWidth = 50 } },
                        new(() => { DoAndRedraw(() => Avatar.SetBestPCTexturesFormat()); }) { text = "Set Best Format", style = { maxWidth = 150 } },
                        new(() => { DoAndRedraw(() => Avatar.CrunchThemAll()); }) { text = "CRUNCH THEM ALL", style = { maxWidth = 150 } },
                    }
                },
                new() {
                    Name = "Android Textures",
                    Actions = new List<Button> {
                        new(() => { DoAndRedraw(() => Avatar.ChangeAllAndroidTexturesSize(1024)); }) { text = "-> 1k", style = { maxWidth = 50 } },
                        new(() => { DoAndRedraw(() => Avatar.ChangeAllAndroidTexturesSize(2048)); }) { text = "-> 2k", style = { maxWidth = 50 } },
                        new(() => { DoAndRedraw(() => Avatar.ChangeAllAndroidTexturesSize(4096)); }) { text = "-> 4k", style = { maxWidth = 50 } },
                        new(() => { DoAndRedraw(() => Avatar.SetBestAndroidTexturesFormat()); }) { text = "Set Best Format", style = { maxWidth = 150 } },
                        new(() => { DoAndRedraw(() => Avatar.MakeTexturesReadyForAndroid()); }) { text = "Prepare for Android", style = { maxWidth = 150 } },
                        new(() => { DoAndRedraw(() => Avatar.CreateQuestMaterialPresets()); }) { text = "Create Quest Material Presets", style = { maxWidth = 200 } },
                    }
                },
            };

            return ActionsListView;
        }

        private MultiColumnListView CreateTexturesGUI() {
            CreateMainListGUI();

            MainListView.columnSortingChanged += OnSortingChanged;

            MainListView.columns.Add(new Column {
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
                    var texture = (TextureMeta)MainListView.viewController.GetItemForIndex(index);
                    field.value = texture.Texture;
                }
            });

            MainListView.columns.Add(new Column {
                title = "Property Name",
                name = "Property Name",
                width = 100,
                resizable = true,
                sortable = true,
                makeCell = () => new Label(),
                bindCell = (element, index) => {
                    var field = (Label)element;
                    var texture = (TextureMeta)MainListView.viewController.GetItemForIndex(index);
                    field.text = texture.PropertyName;
                }
            });

            MainListView.columns.Add(new Column {
                title = "Materials",
                width = 100,
                stretchable = false,
                resizable = true,
                sortable = false,
                makeCell = () => new Foldout { text = "Materials", value = false },
                bindCell = (element, index) => {
                    var foldout = (Foldout)element;
                    var texture = (TextureMeta)MainListView.viewController.GetItemForIndex(index);
                    element.Clear();
                    var count = 0;

                    Avatar.ForeachTextureMaterial(texture, material => {
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

            MainListView.columns.Add(new Column {
                title = "Size",
                name = "Size",
                width = 100,
                stretchable = false,
                resizable = false,
                sortable = true,
                makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft } },
                bindCell = (element, index) => {
                    var label = (Label)element;
                    var texture = (TextureMeta)MainListView.viewController.GetItemForIndex(index);
                    label.text = texture.SizeString;
                }
            });

            MainListView.columns.Add(new Column {
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
                    var texture = (TextureMeta)MainListView.viewController.GetItemForIndex(index);
                    popup.SetValueWithoutNotify(texture.DefaultResolution);
                    popup.SetEnabled(texture.TextureWithChangeableResolution);

                    RegisterCallBack<int>(popup, (e) => {
                        DoAndRedraw(index, () => texture.ChangeDefaultImportSize(e.newValue));
                    });
                },
                unbindCell = (element, index) => {
                    var popup = (PopupField<int>)element;
                    UnregisterCallBack<int>(popup);
                }
            });

            MainListView.columns.Add(new Column {
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
                    var texture = (TextureMeta)MainListView.viewController.GetItemForIndex(index);
                    popup.SetValueWithoutNotify(texture.PcResolution);
                    popup.SetEnabled(texture.TextureWithChangeableResolution);

                    RegisterCallBack<int>(popup, (e) => {
                        DoAndRedraw(index, () => texture.ChangePCImportSize(e.newValue));
                    });
                },
                unbindCell = (element, index) => {
                    var popup = (PopupField<int>)element;
                    UnregisterCallBack<int>(popup);
                }
            });

            MainListView.columns.Add(new Column {
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
                    var texture = (TextureMeta)MainListView.viewController.GetItemForIndex(index);
                    popup.SetValueWithoutNotify(texture.AndroidResolution);
                    popup.SetEnabled(texture.TextureWithChangeableResolution);

                    RegisterCallBack<int>(popup, (e) => {
                        DoAndRedraw(index, () => texture.ChangeAndroidImportSize(e.newValue));
                    });
                },
                unbindCell = (element, index) => {
                    var popup = (PopupField<int>)element;
                    UnregisterCallBack<int>(popup);
                }
            });

            MainListView.columns.Add(new Column {
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
                    var texture = (TextureMeta)MainListView.viewController.GetItemForIndex(index);

                    if (texture.PCFormat != null) {
                        popup.SetValueWithoutNotify(texture.PCFormat.Value);
                    }

                    popup.SetEnabled(texture.TextureWithChangeableFormat);

                    RegisterCallBack<TextureImporterFormat>(popup, (e) => {
                        DoAndRedraw(index, () => texture.ChangePCImporterFormat(e.newValue));
                    });
                },
                unbindCell = (element, index) => {
                    var popup = (PopupField<TextureImporterFormat>)element;
                    UnregisterCallBack<TextureImporterFormat>(popup);
                }
            });

            MainListView.columns.Add(new Column {
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
                    var texture = (TextureMeta)MainListView.viewController.GetItemForIndex(index);

                    if (texture.AndroidFormat != null) {
                        popup.SetValueWithoutNotify(texture.AndroidFormat.Value);
                    }

                    popup.SetEnabled(texture.TextureWithChangeableFormat);

                    RegisterCallBack<TextureImporterFormat>(popup, (e) => {
                        DoAndRedraw(index, () => texture.ChangeAndroidImporterFormat(e.newValue));
                    });
                },
                unbindCell = (element, index) => {
                    var popup = (PopupField<TextureImporterFormat>)element;
                    UnregisterCallBack<TextureImporterFormat>(popup);
                }
            });

            MainListView.columns.Add(new Column {
                title = "Actions",
                minWidth = 200,
                stretchable = true,
                resizable = true,
                sortable = false,
                bindCell = (element, index) => {
                    element.Clear();
                    var texture = (TextureMeta)MainListView.viewController.GetItemForIndex(index);

                    if (texture.Poiyomi) {
                        element.Add(new Label { text = "Poiyomi textures are ignored and can't be changed", style = { flexGrow = 1 } });
                    }

                    if (!texture.PCResolutionEqualsDefault) {
                        element.Add(new Button(() => {
                            DoAndRedraw(index, () => texture.ChangePCImportSize(texture.DefaultResolution));
                        }) { text = "Sync PC and Default resolutions", style = { flexGrow = 1 } });
                    }

                    if (texture.TextureTooBig) {
                        element.Add(new Button(() => {
                            DoAndRedraw(index, () => texture.ChangePCImportSize(2048));
                        }) { text = $"2k → -{texture.SaveSizeWithSmallerTexture}" });
                    }
                }
            });

            return MainListView;
        }


        private void OnSortingChanged() {
            var sortedColumns = MainListView.sortedColumns.ToList();
            if (sortedColumns.Count == 0) return;

            var primary = sortedColumns[0];

            Avatar.textures.Sort((a, b) => {
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

            MainListView.RefreshItems();
        }




        private void refreshTexturesMemory() {
            if (Avatar != null) TexturesMemory.text = $"Textures Memory: {Avatar.TexturesMemory}";
            if (Avatar == null) TexturesMemory.text = $"No Avatar Selected";
        }

        [MenuItem("Azzmurr/Texture Manager")]
        public static void Init() {
            var window = (TextureManager)GetWindow(typeof(TextureManager));
            window.titleContent = new GUIContent("Texture Manager");
            window.SetAvatar(Selection.activeGameObject);
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
            window.SetAvatar(Selection.activeGameObject);
            window.Show();
        }

        public static void Init(GameObject avatar) {
            var window = (TextureManager)GetWindow(typeof(TextureManager));
            window.titleContent = new GUIContent("Texture Manager");
            window.SetAvatar(Selection.activeGameObject);
            window.Show();
        }
    }
}

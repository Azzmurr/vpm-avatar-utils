using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Azzmurr.Utils {
    internal class CommonEditorWindow : EditorWindow {
        public string rootPath = "Packages/com.azzmurr.utils";

        protected ObjectField AvatarSelectorField;
        protected GameObject AvatarGameObject;
        protected AvatarMeta Avatar;

        protected ObjectField FolderSelectorField;
        protected DefaultAsset SelectedFolder;

        protected ObjectField MaterialSelectorField;
        protected MaterialMeta Material;

        protected MultiColumnListView ActionsListView;
        protected MultiColumnListView MainListView;

        private readonly Dictionary<VisualElement, Delegate> _registeredCallbacks = new();

        public void SetAvatar(GameObject avatar) {
            AvatarGameObject = avatar;
            Avatar = avatar != null ? new AvatarMeta(avatar) : null;
            AvatarSelectorField.value = AvatarGameObject;
            ToggleLists(Avatar != null);
        }

        public void SetFolder(DefaultAsset folder) {
            SelectedFolder = folder;
            FolderSelectorField.value = SelectedFolder;
            ToggleLists(SelectedFolder != null);
        }

        public void SetMaterial(Material material) {
            Material = material != null ? new MaterialMeta(material) : null;
            MaterialSelectorField.value = material;
            ToggleLists(Material != null);
        }

        protected VisualElement CreateRootUIElement() {
            var root = rootVisualElement;
            root.style.paddingRight = 8;
            root.style.paddingLeft = 8;
            return root;
        }

        protected VisualElement CreateAvatarSelectorField(Action<AvatarMeta> onChange) {
            var selectorWrapper = new VisualElement { style = { flexShrink = 0 } };
            var selectorField = new ObjectField {
                objectType = typeof(GameObject),
                value = AvatarGameObject,
                name = "AvatarSelector",
                label = "Avatar: ",
                style = {
                    flexShrink = 0,
                    flexGrow = 1,
                }
            };

            AvatarSelectorField = selectorField;

            selectorField.RegisterValueChangedCallback(e => {
                SetAvatar((GameObject)e.newValue);
                onChange(Avatar);
            });

            selectorWrapper.Add(selectorField);
            return selectorWrapper;
        }

        protected VisualElement CreateFolderSelector(Action<DefaultAsset> onChange) {
            var selectorWrapper = new VisualElement { style = { flexShrink = 0 } };
            var selectedFolder = GetSelectedFolder();
            var selectorField = new ObjectField {
                objectType = typeof(DefaultAsset),
                value = selectedFolder,
                name = "Folder",
                label = "Folder: ",
                style = {
                    flexShrink = 0,
                    flexGrow = 1,
                }
            };

            FolderSelectorField = selectorField;
            SetFolder(selectedFolder);

            selectorField.RegisterValueChangedCallback(e => {
                SetFolder((DefaultAsset)e.newValue);
                onChange(SelectedFolder);
            });

            selectorWrapper.Add(selectorField);
            return selectorWrapper;
        }

        protected VisualElement CreateMaterialSelectorField(Action<MaterialMeta> onChange) {
            var selectorWrapper = new VisualElement { style = { flexShrink = 0 } };
            var selectorField = new ObjectField {
                objectType = typeof(Material),
                value = Material?.Material,
                name = "MaterialSelector",
                label = "Material: ",
                style = {
                    flexShrink = 0,
                    flexGrow = 1,
                }
            };

            MaterialSelectorField = selectorField;
            selectorField.RegisterValueChangedCallback(e => {
                SetMaterial((Material)e.newValue);
                onChange(Material);
            });

            selectorWrapper.Add(selectorField);
            return selectorWrapper;
        }

        protected MultiColumnListView CreateMainListGUI() {
            MainListView = new MultiColumnListView {
                name = "Main List View",
                focusable = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                showBorder = true,
                reorderMode = ListViewReorderMode.Animated,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                style = {
                    marginTop = 8,
                }
            };

            return MainListView;
        }

        protected MultiColumnListView CreateActionsListGUI() {
            ActionsListView = new MultiColumnListView {
                name = "Main List View",
                focusable = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                showBorder = true,
                reorderMode = ListViewReorderMode.Animated,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                style = {
                    marginTop = 8,
                }
            };

            ActionsListView.columns.Add(new Column {
                title = "Type",
                width = 120,
                makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft } },
                bindCell = (element, index) => {
                    var label = (Label)element;
                    var actionGroup = (ActionGroup)ActionsListView.viewController.GetItemForIndex(index);
                    label.text = actionGroup.Name;
                }
            });

            ActionsListView.columns.Add(new Column {
                title = "Actions",
                minWidth = 800,
                makeCell = () => new VisualElement { style = { flexDirection = FlexDirection.Row } },
                bindCell = (element, index) => {
                    var actionGroup = (ActionGroup)ActionsListView.viewController.GetItemForIndex(index);
                    actionGroup.Actions
                        .ToList()
                        .ConvertAll((action) => {
                            action.style.flexGrow = 1;
                            return action;
                        })
                        .ForEach(element.Add);
                }
            });

            return ActionsListView;
        }


        private DefaultAsset GetSelectedFolder() {
            var selected = Selection.activeObject;

            if (selected == null) {
                return AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets");
            }

            var path = AssetDatabase.GetAssetPath(selected);

            if (string.IsNullOrEmpty(path)) {
                path = ((GameObject)selected).scene.path;
            }

            if (!AssetDatabase.IsValidFolder(path)) {
                path = Path.GetDirectoryName(path);
            }

            if (!string.IsNullOrEmpty(path)) return AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);

            EditorUtility.DisplayDialog("Batch Avatar Uploader", "Could not determine folder path.", "OK");
            return null;

        }

        protected void DoAndRedraw(Action action) {
            action.Invoke();
            MainListView.RefreshItems();
        }

        protected void DoAndRedraw(int index, Action action) {
            action.Invoke();
            MainListView.RefreshItem(index);
        }

        protected void ToggleLists(bool enabled) {
            MainListView?.SetEnabled(enabled);
            ActionsListView?.SetEnabled(enabled);
        }

        protected void RegisterCallBack<T>(VisualElement element, Action<ChangeEvent<T>> action) where T : notnull {
            if (element is not INotifyValueChanged<T> field) {
                throw new ArgumentException($"{element.GetType()} does not implement INotifyValueChanged<{typeof(T)}>");
            }

            EventCallback<ChangeEvent<T>> callback = evt => action(evt);
            field.RegisterValueChangedCallback(callback);
            _registeredCallbacks.Add(element, callback);
        }

        protected void UnregisterCallBack<T>(VisualElement element) where T : notnull {
            if (!_registeredCallbacks.TryGetValue(element, out var callback)) return;
            if (element is INotifyValueChanged<T> field) {
                field.UnregisterValueChangedCallback((EventCallback<ChangeEvent<T>>)callback);
            }

            _registeredCallbacks.Remove(element);
        }

    }
}

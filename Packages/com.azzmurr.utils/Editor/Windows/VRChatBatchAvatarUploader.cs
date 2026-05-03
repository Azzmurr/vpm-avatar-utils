using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VRC.Core;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3A.Editor;
using VRC.SDKBase.Editor;
using VRC.SDKBase.Editor.Api;
using Object = UnityEngine.Object;

namespace Azzmurr.Utils {
    internal class VRChatBatchAvatarUploader : EditorWindow {
        public static readonly string AgreementText = "By clicking OK, I certify that I have the necessary rights to upload this content and that it will not infringe on any third-party legal or intellectual property rights.";
        private MultiColumnListView _actionsGUI;
        private MultiColumnListView _avatarsGUI;

        private CancellationTokenSource _cts;

        private ObjectField _folderSelectorField;
        private Label _statusLabel;

        private void OnDestroy() {
            _cts?.Cancel();
        }

        private void CreateGUI() {
            var root = CreateRootElement();
            root.Add(CreateFolderSelector());
            root.Add(CreateActionsGUI());
            root.Add(CreateStatusLabel());
            root.Add(CreateAvatarListView());
        }

        private VisualElement CreateRootElement() {
            var root = rootVisualElement;
            root.style.paddingRight = 8;
            root.style.paddingLeft = 8;

            return root;
        }

        private VisualElement CreateFolderSelector() {
            var folderSelector = new VisualElement { style = { flexShrink = 0 } };
            var selectedFolder = GetSelectedFolder();
            var folderSelectorField = new ObjectField {
                objectType = typeof(DefaultAsset),
                value = selectedFolder,
                name = "Folder",
                label = "Folder: ",
                style = {
                    flexShrink = 0,
                    flexGrow = 1,
                }
            };

            _folderSelectorField = folderSelectorField;

            folderSelector.Add(folderSelectorField);
            return folderSelector;
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
                    Name = "Folder",
                    Actions = new List<Button> {
                        new(RescanSelectedFolder) { text = "Rescan Folder" }
                    },
                },
                new() {
                    Name = "Select",
                    Actions = new List<Button> {
                        new(() => { SetAllSelected(true); }) { text = "All" },
                        new(() => { SetAllSelected(false); }) { text = "None" },
                        new(FlipSelection) { text = "Flip selection" },
                    }
                },
                new() {
                    Name = "Upload",
                    Actions = new List<Button> {
                        new(() => UploadAvatars(false)) { text = "Upload Selected" },
                        new(() => UploadAvatars(true)) { text = "Upload All" },
                        new(CancelUpload) { text = "Cancel Upload", visible = false },
                    }
                }
            };

            _actionsGUI = actions;

            return actions;
        }

        private MultiColumnListView CreateAvatarListView() {
            var avatarList = new MultiColumnListView {
                name = "Avatars List",
                focusable = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                showBorder = true,
                reorderMode = ListViewReorderMode.Animated,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                style = {
                    marginTop = 8,
                }
            };

            avatarList.columns.Add(new Column {
                title = "Selected",
                width = 200,
                makeCell = () => new Toggle(),
                bindCell = (element, index) => {
                    var avatarEntry = (AvatarEntry)avatarList.viewController.GetItemForIndex(index);
                    var toggle = (Toggle)element;
                    toggle.value = avatarEntry.Selected;

                    toggle.RegisterValueChangedCallback(evt => { avatarEntry.Selected = evt.newValue; });
                }
            });

            avatarList.columns.Add(new Column {
                title = "Scene",
                width = 200,
                makeCell = () => {
                    var objectField = new ObjectField {
                        objectType = typeof(SceneAsset),
                        allowSceneObjects = false
                    };
                    return objectField;
                },
                bindCell = (element, index) => {
                    var avatarEntry = (AvatarEntry)avatarList.viewController.GetItemForIndex(index);
                    ((ObjectField)element).value = avatarEntry.AvatarScene;
                }
            });

            avatarList.columns.Add(new Column {
                title = "Game Object",
                width = 200,
                makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 8 } },
                bindCell = (element, index) => {
                    var avatarEntry = (AvatarEntry)avatarList.viewController.GetItemForIndex(index);
                    ((Label)element).text = avatarEntry.Name;
                }
            });

            avatarList.columns.Add(new Column {
                title = "Blueprint ID",
                width = 200,
                makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 8 } },
                bindCell = (element, index) => {
                    var avatarEntry = (AvatarEntry)avatarList.viewController.GetItemForIndex(index);
                    var hasBlueprint = string.IsNullOrEmpty(avatarEntry.BlueprintId);
                    ((Label)element).text = hasBlueprint
                        ? "○ No Blueprint ID"
                        : $"✓ {avatarEntry.BlueprintId}";

                    ((Label)element).style.color = hasBlueprint
                        ? new Color(0.8f, 0.6f, 0.2f)
                        : new Color(0.4f, 0.85f, 0.4f);
                }
            });

            avatarList.columns.Add(new Column {
                title = "Status",
                width = 200,
                makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 8 } },
                bindCell = (element, index) => {
                    var avatarEntry = (AvatarEntry)avatarList.viewController.GetItemForIndex(index);
                    ((Label)element).text = avatarEntry.State switch {
                        "InProgress" => $"○ {avatarEntry.Status}",
                        "Error" => $"✗ {avatarEntry.Status}",
                        "Success" => $"✓ {avatarEntry.Status}",
                        _ => avatarEntry.Status
                    };

                    ((Label)element).style.color = avatarEntry.State switch {
                        "InProgress" => new Color(0.9f, 0.4f, 0.0f),
                        "Error" => new Color(0.8f, 0.2f, 0.2f),
                        "Success" => new Color(0.2f, 0.8f, 0.2f),
                        _ => Color.white
                    };
                }
            });

            _avatarsGUI = avatarList;

            return avatarList;
        }

        private Label CreateStatusLabel() {
            _statusLabel = new Label { name = "status-label", text = "Ready", style = { marginTop = 8 } };
            return _statusLabel;
        }

        [MenuItem("Azzmurr/Batch Avatar Uploader")]
        public static void ShowWindow() {
            var window = GetWindow<VRChatBatchAvatarUploader>("Batch Avatar Uploader");
            window.minSize = new Vector2(600, 400);
        }

        private void RescanSelectedFolder() {
            if (_folderSelectorField.value == null) {
                _avatarsGUI.itemsSource = null;
            }

            _avatarsGUI.itemsSource = ScanFolder(_folderSelectorField.value);
            _avatarsGUI.RefreshItems();
        }

        private DefaultAsset GetSelectedFolder() {
            var selected = Selection.activeObject;

            if (selected == null) {
                return AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets");
            }

            var path = AssetDatabase.GetAssetPath(selected);
            if (!AssetDatabase.IsValidFolder(path)) {
                path = Path.GetDirectoryName(path);
            }

            if (string.IsNullOrEmpty(path)) {
                EditorUtility.DisplayDialog("Batch Avatar Uploader", "Could not determine folder path.", "OK");
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
        }

        private List<AvatarEntry> ScanFolder(Object folder) {
            if (folder == null) {
                return new List<AvatarEntry>();
            }

            var currentScene = SceneManager.GetSceneAt(0).path;

            var folderPath = AssetDatabase.GetAssetPath(folder);
            var sceneGUIDs = AssetDatabase.FindAssets("t:Scene", new[] { folderPath });
            var allAvatars = new List<AvatarEntry>();

            foreach (var guid in sceneGUIDs) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                var descriptors = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<VRCAvatarDescriptor>());

                allAvatars.AddRange(descriptors.Select(descriptor => new AvatarEntry(descriptor.gameObject)).Where(entry => !string.IsNullOrEmpty(entry.BlueprintId)));

                EditorSceneManager.CloseScene(scene, true);
            }

            if (!string.IsNullOrEmpty(currentScene)) {
                EditorSceneManager.OpenScene(currentScene, OpenSceneMode.Single);
            }

            return allAvatars;
        }

        private void SetAllSelected(bool selected) {
            if (_avatarsGUI.itemsSource is not List<AvatarEntry> avatars) return;

            foreach (var entry in avatars) entry.Selected = selected;
            _avatarsGUI.RefreshItems();
        }

        private void FlipSelection() {
            if (_avatarsGUI.itemsSource is not List<AvatarEntry> avatars) return;

            foreach (var entry in avatars) entry.Selected = !entry.Selected;
            _avatarsGUI.RefreshItems();
        }

        private async void UploadAvatars(bool all) {
            EditorApplication.ExecuteMenuItem("VRChat SDK/Show Control Panel");

            if (!VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var builder)) {
                EditorUtility.DisplayDialog("Batch Avatar Uploader", "VRChat SDK Builder not found. Please open the VRChat SDK Control Panel first.", "OK");
                return;
            }

            if (_avatarsGUI.itemsSource is not List<AvatarEntry> avatars) {
                EditorUtility.DisplayDialog("Batch Avatar Uploader", "No avatars found in selected folder", "OK");
                return;
            }

            var toUpload = all ? avatars : avatars.Where(a => a.Selected).ToList();
            if (toUpload.Count == 0) {
                EditorUtility.DisplayDialog("Batch Avatar Uploader", "No avatars selected for upload.", "OK");
                return;
            }

            var confirm = EditorUtility.DisplayDialog(
                "Upload Avatars",
                $"You are about to upload {toUpload.Count} avatar(s).\n\nEach avatar's scene will be opened, the VRCSDK builder triggered, and the scene closed.\n\nContinue?",
                "Upload", "Cancel");

            if (!confirm) return;

            _cts = new CancellationTokenSource();
            _actionsGUI.SetEnabled(false);

            var completed = 0;
            var failed = 0;

            foreach (var entry in toUpload) {
                if (_cts.IsCancellationRequested) {
                    entry.Status = "Cancelled";
                    _avatarsGUI.RefreshItems();
                    continue;
                }

                _statusLabel.text = $"Uploading {completed + failed + 1}/{toUpload.Count}: {entry.Name}...";

                var success = await UploadAvatar(builder, entry, _cts.Token);
                if (success) completed++;
                else failed++;
            }

            _statusLabel.text = $"Done. {completed} uploaded, {failed} failed.";
            _cts = null;
            _actionsGUI.SetEnabled(true);
        }

        private async Task<bool> UploadAvatar(IVRCSdkAvatarBuilderApi builder, AvatarEntry entry, CancellationToken ct) {
            try {
                entry.Status = "Uploading...";
                entry.State = "InProgress";
                _avatarsGUI.RefreshItems();
                await AddCopyrightAgreement(entry.BlueprintId);

                var scene = EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(entry.AvatarScene), OpenSceneMode.Additive);

                VRCAvatarDescriptor targetDescriptor = null;
                foreach (var root in scene.GetRootGameObjects()) {
                    var descs = root.GetComponentsInChildren<VRCAvatarDescriptor>(true);
                    targetDescriptor = descs.FirstOrDefault(d => d.gameObject.name == entry.Name);
                    if (targetDescriptor != null) break;
                }

                if (targetDescriptor == null) {
                    Debug.LogWarning($"Batch Avatar Uploader: Could not find avatar '{entry.Name}' in scene '{entry.AvatarScene.name}'");
                    entry.Status = "Failed to find avatar";
                    _avatarsGUI.RefreshItems();
                    return false;
                }

                builder.OnSdkBuildProgress += (sender, message) => {
                    entry.Status = message;
                    _avatarsGUI.RefreshItems();
                };
                builder.OnSdkBuildFinish += (sender, message) => {
                    entry.Status = message;
                    _avatarsGUI.RefreshItems();
                };

                builder.OnSdkUploadProgress += (sender, message) => {
                    entry.Status = $"{message.status} / {message.percentage}";
                    _avatarsGUI.RefreshItems();
                };

                var av = await VRCApi.GetAvatar(entry.BlueprintId, false, ct);
                await builder.BuildAndUpload(targetDescriptor.gameObject, av, cancellationToken: ct);

                entry.Status = "Done";
                entry.State = "Success";
                _avatarsGUI.RefreshItems();
                return true;
            }
            catch (NullReferenceException e) {
                entry.Status = "SDK Control Panel not opened";
                entry.State = "Error";
                EditorUtility.DisplayDialog("Batch Avatar Uploader", "SDK Control Panel hasn't been opened yet. Please open the SDK Control Panel and try again.", "OK");
                Debug.LogError(e.Message + e.StackTrace);
                return false;
            }
            catch (BuilderException e) {
                Thread.Sleep(4000);
                Debug.LogError(e.Message + e.StackTrace);
                if (e.Message.Contains("Avatar validation failed")) {
                    entry.Status = "Validation Failed";
                    entry.State = "Error";
                    _avatarsGUI.RefreshItems();
                    EditorUtility.DisplayDialog("Batch Avatar Uploader", "Validation Failed for " + entry.Name + ".\nPlease fix the errors and try again.", "OK");
                }

                return false;
            }
            catch (ValidationException e) {
                entry.Status = e.Message;
                entry.State = "Error";
                _avatarsGUI.RefreshItems();
                EditorUtility.DisplayDialog("Batch Avatar Uploader", e.Message + "\n" + string.Join("\n", e.Errors), "OK");
                Debug.LogError(e.Message + e.StackTrace);
                EditorUtility.DisplayDialog("Batch Avatar Uploader", "Please fix the errors and try again.", "OK");
                return false;
            }
            catch (OwnershipException e) {
                entry.Status = e.Message;
                entry.State = "Error";
                _avatarsGUI.RefreshItems();
                EditorUtility.DisplayDialog("Batch Avatar Uploader", e.Message, "OK");
                Debug.LogError(e.Message + e.StackTrace);
                return false;
            }
            catch (UploadException e) {
                entry.Status = e.Message;
                entry.State = "Error";
                _avatarsGUI.RefreshItems();
                EditorUtility.DisplayDialog("Batch Avatar Uploader", e.Message, "OK");
                Debug.LogError(e.Message + e.StackTrace);
                return false;
            }
            catch (OperationCanceledException) {
                entry.Status = "Cancelled";
                entry.State = "Error";
                _avatarsGUI.RefreshItems();
                return false;
            }
            catch (Exception e) {
                entry.Status = "Unknown Error";
                entry.State = "Error";
                _avatarsGUI.RefreshItems();
                Debug.LogError(e.Message + e.StackTrace);
                EditorUtility.DisplayDialog("Batch Avatar Uploader", e.Message + "\n" + e.StackTrace, "OK");
                return false;
            }
        }

        private void CancelUpload() {
            _cts?.Cancel();
            _statusLabel.text = "Cancelling...";
        }

        private static async Task AddCopyrightAgreement(string blueprint) {
            const string key = "VRCSdkControlPanel.CopyrightAgreement.ContentList";
            var keyText = SessionState.GetString(key, "");
            var list = string.IsNullOrEmpty(keyText) ? new List<string>() : SessionState.GetString(key, "").Split(';').ToList();
            if (list.Contains(blueprint)) return;
            list.Add(blueprint);
            SessionState.SetString(key, string.Join(";", list));

            await VRCApi.ContentUploadConsent(new VRCAgreement {
                AgreementCode = "content.copyright.owned",
                AgreementFulltext = AgreementText,
                ContentId = blueprint,
                Version = 1,
            });
        }

        private class AvatarEntry {
            public readonly SceneAsset AvatarScene;
            public readonly string BlueprintId;
            public readonly string Name;
            public bool Selected;
            public string State;
            public string Status;

            public AvatarEntry(GameObject avatar) {
                Name = avatar.name;
                AvatarScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(avatar.scene.path);

                var pipeline = avatar.GetComponent<PipelineManager>();
                if (pipeline) {
                    BlueprintId = pipeline.blueprintId;
                }

                Selected = false;
                Status = "Pending";
                State = "Pending";
            }
        }
    }
}

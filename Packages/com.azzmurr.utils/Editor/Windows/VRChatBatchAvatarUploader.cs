using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using VRC.SDKBase.Editor.Api;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Azzmurr.Utils {
    internal class VRChatBatchAvatarUploader : CommonEditorWindow {
        private const string AgreementText = "By clicking OK, I certify that I have the necessary rights to upload this content and that it will not infringe on any third-party legal or intellectual property rights.";

        private readonly Queue<Action> _mainThreadQueue = new();

        private CancellationTokenSource _cts;

        private Label _statusLabel;
        private bool _setBestPCTextureFormatBeforeUpload;
        private bool _setCrunchPCTextureFormatBeforeUpload;

        private void OnEnable() => EditorApplication.update += FlushMainThreadQueue;
        private void OnDisable() => EditorApplication.update -= FlushMainThreadQueue;

        private void OnDestroy() {
            _cts?.Cancel();
        }

        private void CreateGUI() {
            var root = CreateRootUIElement();
            root.Add(CreateFolderSelector(_ => {}));
            root.Add(CreateActionsGUI());
            root.Add(CreateTextureUpdateCheckbox());
            root.Add(CreateStatusLabel());
            root.Add(CreateAvatarListView());
        }

        private void FlushMainThreadQueue() {
            while (_mainThreadQueue.Count > 0)
                _mainThreadQueue.Dequeue()?.Invoke();
        }

        private void RunOnMainThread(Action action) => _mainThreadQueue.Enqueue(action);

        private MultiColumnListView CreateActionsGUI() {
            CreateActionsListGUI();

            ActionsListView.itemsSource = new List<ActionGroup> {
                new() {
                    Name = "Folder",
                    Actions = new List<Button> {
                        new(RescanSelectedFolder) { text = "Scan Folder" }
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
                        new(() => UploadAvatars(false)) { text = "Build & Publish Selected" },
                        new(() => UploadAvatars(true)) { text = "Build & Publish All" },
                    }
                }
            };

            return ActionsListView;
        }

        private MultiColumnListView CreateAvatarListView() {
            CreateMainListGUI();

            MainListView.columns.Add(new Column {
                title = "",
                width = 50,
                makeCell = () => new Toggle(),
                bindCell = (element, index) => {
                    var avatarEntry = (AvatarEntry)MainListView.viewController.GetItemForIndex(index);
                    var toggle = (Toggle)element;
                    toggle.value = avatarEntry.Selected;

                    toggle.RegisterValueChangedCallback(evt => { avatarEntry.Selected = evt.newValue; });
                }
            });

            MainListView.columns.Add(new Column {
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
                    var avatarEntry = (AvatarEntry)MainListView.viewController.GetItemForIndex(index);
                    ((ObjectField)element).value = avatarEntry.AvatarScene;
                }
            });

            MainListView.columns.Add(new Column {
                title = "Game Object",
                width = 200,
                makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 8 } },
                bindCell = (element, index) => {
                    var avatarEntry = (AvatarEntry)MainListView.viewController.GetItemForIndex(index);
                    ((Label)element).text = avatarEntry.Name;
                }
            });

            MainListView.columns.Add(new Column {
                title = "Blueprint ID",
                width = 100,
                makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 8 } },
                bindCell = (element, index) => {
                    var avatarEntry = (AvatarEntry)MainListView.viewController.GetItemForIndex(index);
                    var hasBlueprint = string.IsNullOrEmpty(avatarEntry.BlueprintId);
                    ((Label)element).text = hasBlueprint
                        ? "○ No Blueprint ID"
                        : $"✓ {avatarEntry.BlueprintId}";

                    ((Label)element).style.color = hasBlueprint
                        ? new Color(0.8f, 0.6f, 0.2f)
                        : new Color(0.4f, 0.85f, 0.4f);
                }
            });

            MainListView.columns.Add(new Column {
                title = "Status",
                width = 300,
                makeCell = () => new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 8 } },
                bindCell = (element, index) => {
                    var avatarEntry = (AvatarEntry)MainListView.viewController.GetItemForIndex(index);
                    var time = avatarEntry.TimeTaken.TotalSeconds > 0
                        ? $"({avatarEntry.TimeTaken.Minutes:D2}:{avatarEntry.TimeTaken.Seconds:D2})"
                        : "";

                    ((Label)element).text = avatarEntry.State switch {
                        "InProgress" => $"○ {avatarEntry.Status}",
                        "Error" => $"✗ {avatarEntry.Status}. {time}",
                        "Success" => $"✓ {avatarEntry.Status}. {time}",
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

            return MainListView;
        }

        private Label CreateStatusLabel() {
            _statusLabel = new Label { name = "status-label", text = "Ready", style = { marginTop = 8 } };
            return _statusLabel;
        }

        private VisualElement CreateTextureUpdateCheckbox() {
            var visualElement = new VisualElement { style = { flexShrink = 0, marginTop = 8 } };

            var setBestPCTextureFormatBeforeUpload = new Toggle("Set best PC texture format before upload") { value = false };
            setBestPCTextureFormatBeforeUpload.RegisterValueChangedCallback(evt => {
                _setBestPCTextureFormatBeforeUpload = evt.newValue;
            });

            visualElement.Add(setBestPCTextureFormatBeforeUpload);

            var setCrunchPCTextureFormatBeforeUpload = new Toggle("Set crunch PC texture format before upload") { value = false };
            setCrunchPCTextureFormatBeforeUpload.RegisterValueChangedCallback(evt => {
                _setCrunchPCTextureFormatBeforeUpload = evt.newValue;
            });

            visualElement.Add(setCrunchPCTextureFormatBeforeUpload);

            return visualElement;
        }

        [MenuItem("Azzmurr/Batch Avatar Uploader")]
        public static void ShowWindow() {
            var window = GetWindow<VRChatBatchAvatarUploader>("Batch Avatar Uploader");
            window.minSize = new Vector2(600, 400);
        }

        private void RescanSelectedFolder() {
            if (SelectedFolder) {
                MainListView.itemsSource = null;
            }

            MainListView.itemsSource = ScanFolder(SelectedFolder);
            MainListView.RefreshItems();
        }


        private static List<AvatarEntry> ScanFolder(Object folder) {
            if (folder == null) {
                return new List<AvatarEntry>();
            }

            var currentScene = SceneManager.GetSceneAt(0);
            var currentScenePath = currentScene.path;

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

            if (!string.IsNullOrEmpty(currentScenePath)) {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }

            return allAvatars;
        }

        private void SetAllSelected(bool selected) {
            if (MainListView.itemsSource is not List<AvatarEntry> avatars) return;

            foreach (var entry in avatars) entry.Selected = selected;
            MainListView.RefreshItems();
        }

        private void FlipSelection() {
            if (MainListView.itemsSource is not List<AvatarEntry> avatars) return;

            foreach (var entry in avatars) entry.Selected = !entry.Selected;
            MainListView.RefreshItems();
        }

        private async void UploadAvatars(bool all) {
            try {
                var currentScene = SceneManager.GetSceneAt(0);
                var currentScenePath = currentScene.path;

                EditorApplication.ExecuteMenuItem("VRChat SDK/Show Control Panel");

                if (!VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var builder)) {
                    EditorUtility.DisplayDialog("Batch Avatar Uploader", "VRChat SDK Builder not found. Please open the VRChat SDK Control Panel first.", "OK");
                    return;
                }

                if (MainListView.itemsSource is not List<AvatarEntry> avatars) {
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
                    $"{AgreementText} \n\n You are about to upload {toUpload.Count} avatar(s).\n\nEach avatar's scene will be opened, the VRCSDK builder triggered, and the scene closed.",
                    "OK", "Cancel");

                if (!confirm) return;

                _cts = new CancellationTokenSource();
                ActionsListView.SetEnabled(false);

                var completed = 0;
                var failed = 0;
                var sw = Stopwatch.StartNew();

                foreach (var entry in avatars) {
                    entry.State = "Pending";
                    entry.Status = "";
                }

                foreach (var entry in toUpload) {
                    entry.Status = "Pending";
                }

                MainListView.RefreshItems();

                foreach (var entry in toUpload) {
                    if (_cts.IsCancellationRequested) {
                        entry.Status = "Cancelled";
                        MainListView.RefreshItems();
                        continue;
                    }

                    _statusLabel.text = $"Uploading {completed + failed + 1}/{toUpload.Count}: {entry.Name}...";

                    var success = await UploadAvatar(builder, entry, _cts.Token);
                    if (success) completed++;
                    else failed++;
                }

                sw.Stop();
                var t = sw.Elapsed;

                _statusLabel.text = $"Done. {completed} uploaded, {failed} failed. Time taken: {t.Hours:D2} hour(s) {t.Minutes:D2} minute(s) {t.Seconds:D2} second(s)";
                _cts = null;
                ActionsListView.SetEnabled(true);

                if (!string.IsNullOrEmpty(currentScenePath)) {
                    EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
                }
            }
            catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private async Task<bool> UploadAvatar(IVRCSdkAvatarBuilderApi builder, AvatarEntry entry, CancellationToken ct) {
            var scene = EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(entry.AvatarScene), OpenSceneMode.Additive);
            var sw = Stopwatch.StartNew();

            EventHandler<object> onBuildStart = null;
            EventHandler<string> onBuildProgress = null;
            EventHandler<string> onBuildSuccess = null;
            EventHandler<string> onBuildError = null;
            EventHandler onUploadStart = null;
            EventHandler<string> onUploadSuccess = null;
            EventHandler<string> onUploadError = null;
            EventHandler<(string status, float percentage)> onUploadProgress = null;

            try {
                GameObject avatarObject = null;

                MainListView.RefreshItems();
                await AddCopyrightAgreement(entry.BlueprintId);

                VRCAvatarDescriptor targetDescriptor = null;
                foreach (var root in scene.GetRootGameObjects()) {
                    var descs = root.GetComponentsInChildren<VRCAvatarDescriptor>(true);
                    targetDescriptor = descs.FirstOrDefault(d => d.GetComponent<PipelineManager>().blueprintId == entry.BlueprintId);
                    if (targetDescriptor != null) {
                        avatarObject = root;
                        break;
                    };
                }

                if (_setBestPCTextureFormatBeforeUpload) {
                    new AvatarMeta(avatarObject).SetBestPCTexturesFormat();
                }

                if (_setCrunchPCTextureFormatBeforeUpload) {
                    new AvatarMeta(avatarObject).CrunchThemAll();
                }

                if (targetDescriptor == null) {
                    Debug.LogWarning($"Batch Avatar Uploader: Could not find avatar '{entry.Name}' in scene '{entry.AvatarScene.name}'");
                    entry.Status = "Failed to find avatar";
                    entry.State = "Failed";
                    MainListView.RefreshItems();
                    return false;
                }

                onBuildStart = (_, _) => RunOnMainThread(() => {
                    entry.Status = "Building...";
                    entry.State = "InProgress";
                    MainListView.RefreshItems();
                });
                onBuildProgress = (_, m) => RunOnMainThread(() => {
                    entry.Status = m;
                    MainListView.RefreshItems();
                });
                onBuildSuccess = (_, m) => RunOnMainThread(() => {
                    entry.Status = m;
                    MainListView.RefreshItems();
                });
                onBuildError = (_, m) => RunOnMainThread(() => {
                    sw.Stop();
                    entry.TimeTaken = sw.Elapsed;
                    entry.Status = m;
                    entry.State = "Error";
                    MainListView.RefreshItems();
                });
                onUploadStart = (_, _) => RunOnMainThread(() => {
                    entry.Status = "Uploading...";
                    MainListView.RefreshItems();
                });
                onUploadProgress = (_, m) => RunOnMainThread(() => {
                    entry.Status = m.status;
                    MainListView.RefreshItems();
                });
                onUploadSuccess = (_, _) => RunOnMainThread(() => {
                    sw.Stop();
                    entry.TimeTaken = sw.Elapsed;
                    entry.Status = "Uploaded!";
                    entry.State = "Success";
                    MainListView.RefreshItems();
                });
                onUploadError = (_, m) => RunOnMainThread(() => {
                    sw.Stop();
                    entry.TimeTaken = sw.Elapsed;
                    entry.Status = m;
                    entry.State = "Error";
                    MainListView.RefreshItems();
                });

                builder.OnSdkBuildStart += onBuildStart;
                builder.OnSdkBuildProgress += onBuildProgress;
                builder.OnSdkBuildSuccess += onBuildSuccess;
                builder.OnSdkBuildError += onBuildError;
                builder.OnSdkUploadStart += onUploadStart;
                builder.OnSdkUploadProgress += onUploadProgress;
                builder.OnSdkUploadSuccess += onUploadSuccess;
                builder.OnSdkUploadError += onUploadError;

                var av = await VRCApi.GetAvatar(entry.BlueprintId, true, ct);
                await builder.BuildAndUpload(targetDescriptor.gameObject, av, null, ct);
                return true;
            }

            catch (ApiErrorException e) {
                if (entry.State != "Error") {
                    sw.Stop();
                    entry.TimeTaken = sw.Elapsed;
                    entry.State = "Error";
                    entry.Status = e.ErrorMessage;
                    MainListView.RefreshItems();
                }

                Debug.LogError(e.Message + e.StackTrace);
                return false;
            }

            catch (Exception e) {
                if (entry.State != "Error") {
                    sw.Stop();
                    entry.TimeTaken = sw.Elapsed;
                    entry.State = "Error";
                    entry.Status = e.Message;
                    MainListView.RefreshItems();
                }

                Debug.LogError(e.Message + e.StackTrace);
                return false;
            }
            finally {
                builder.OnSdkBuildStart -= onBuildStart;
                builder.OnSdkBuildProgress -= onBuildProgress;
                builder.OnSdkBuildSuccess -= onBuildSuccess;
                builder.OnSdkBuildError -= onBuildError;
                builder.OnSdkUploadStart -= onUploadStart;
                builder.OnSdkUploadProgress -= onUploadProgress;
                builder.OnSdkUploadSuccess -= onUploadSuccess;
                builder.OnSdkUploadError -= onUploadError;
                EditorSceneManager.CloseScene(scene, true);
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
            public TimeSpan TimeTaken;

            public AvatarEntry(GameObject avatar) {
                Name = avatar.name;
                AvatarScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(avatar.scene.path);

                var pipeline = avatar.GetComponent<PipelineManager>();
                if (pipeline) {
                    BlueprintId = pipeline.blueprintId;
                }

                Selected = false;
                State = "Pending";
            }
        }
    }
}

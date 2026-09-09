using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const string AgreementText =
            "By clicking OK, I certify that I have the necessary rights to upload this content and that it will not infringe on any third-party legal or intellectual property rights.";

        private readonly Queue<Action> _mainThreadQueue = new();

        private CancellationTokenSource _cts;

        private Label _statusLabel;
        private bool _setBestPCTextureFormatBeforeUpload;
        private bool _setCrunchPCTextureFormatBeforeUpload;
        private bool _retryFailedUploads = true;
        private int _retryCount = 5;
        private int _retryDelay = 1000;

        private void OnEnable() => EditorApplication.update += FlushMainThreadQueue;
        private void OnDisable() => EditorApplication.update -= FlushMainThreadQueue;

        private void OnDestroy() {
            _cts?.Cancel();
        }

        private void CreateGUI() {
            var root = CreateRootUIElement();
            root.Add(CreateFolderSelector(_ => { }));
            root.Add(CreateActionsGUI());
            root.Add(CreateTextureUpdateCheckbox());
            root.Add(CreateRetrySection());
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
                    toggle.SetEnabled(!string.IsNullOrEmpty(avatarEntry.BlueprintId));

                    RegisterCallBack<bool>(toggle, (evt) => { avatarEntry.Selected = evt.newValue; });
                },
                unbindCell = (toggle, index) => { UnregisterCallBack<bool>(toggle); }
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
                makeCell = () => new Label
                    { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 8 } },
                bindCell = (element, index) => {
                    var avatarEntry = (AvatarEntry)MainListView.viewController.GetItemForIndex(index);
                    ((Label)element).text = avatarEntry.Name;
                }
            });

            MainListView.columns.Add(new Column {
                title = "Blueprint ID",
                width = 100,
                makeCell = () => new Label
                    { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 8 } },
                bindCell = (element, index) => {
                    var avatarEntry = (AvatarEntry)MainListView.viewController.GetItemForIndex(index);
                    var noBlueprint = string.IsNullOrEmpty(avatarEntry.BlueprintId);
                    ((Label)element).text = noBlueprint
                        ? "○ No Blueprint ID"
                        : $"✓ {avatarEntry.BlueprintId}";

                    ((Label)element).style.color = noBlueprint
                        ? new Color(0.8f, 0.6f, 0.2f)
                        : new Color(0.4f, 0.85f, 0.4f);
                }
            });

            MainListView.columns.Add(new Column {
                title = "Status",
                width = 300,
                makeCell = () => new Label
                    { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft, marginLeft = 8 } },
                bindCell = (element, index) => {
                    var avatarEntry = (AvatarEntry)MainListView.viewController.GetItemForIndex(index);
                    var time = avatarEntry.TimeTaken.TotalSeconds > 0
                        ? $"({avatarEntry.TimeTaken.Minutes:D2}:{avatarEntry.TimeTaken.Seconds:D2})"
                        : "";

                    ((Label)element).text = avatarEntry.State switch {
                        AvatarEntryState.InProgress => $"○ {avatarEntry.Message}",
                        AvatarEntryState.GenericError => $"✗ {avatarEntry.Message}. {time}",
                        AvatarEntryState.BuildError => $"✗ {avatarEntry.Message}. {time}",
                        AvatarEntryState.UploadError => $"✗ {avatarEntry.Message}. {time}",
                        AvatarEntryState.Success => $"✓ {avatarEntry.Message}. {time}",
                        _ => avatarEntry.Message
                    };

                    ((Label)element).style.color = avatarEntry.State switch {
                        AvatarEntryState.InProgress => new Color(0.9f, 0.4f, 0.0f),
                        AvatarEntryState.GenericError => new Color(0.8f, 0.2f, 0.2f),
                        AvatarEntryState.BuildError => new Color(0.8f, 0.2f, 0.2f),
                        AvatarEntryState.UploadError => new Color(0.8f, 0.2f, 0.2f),
                        AvatarEntryState.Success => new Color(0.2f, 0.8f, 0.2f),
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

            var setBestPCTextureFormatBeforeUpload = new Toggle("Set best PC texture format before upload")
                { value = false };
            setBestPCTextureFormatBeforeUpload.RegisterValueChangedCallback(evt => {
                _setBestPCTextureFormatBeforeUpload = evt.newValue;
            });

            visualElement.Add(setBestPCTextureFormatBeforeUpload);

            var setCrunchPCTextureFormatBeforeUpload = new Toggle("Set crunch PC texture format before upload")
                { value = false };
            setCrunchPCTextureFormatBeforeUpload.RegisterValueChangedCallback(evt => {
                _setCrunchPCTextureFormatBeforeUpload = evt.newValue;
            });

            visualElement.Add(setCrunchPCTextureFormatBeforeUpload);

            return visualElement;
        }

        private VisualElement CreateRetrySection() {
            var visualElement = new VisualElement { style = { flexShrink = 0, marginTop = 8 } };

            var retryCheckbox = new Toggle("Retry failed uploads") { value = _retryFailedUploads };
            retryCheckbox.RegisterValueChangedCallback(evt => { _retryFailedUploads = evt.newValue; });

            visualElement.Add(retryCheckbox);

            var retryCountField = new IntegerField("Retry count") { value = _retryCount };
            retryCountField.RegisterValueChangedCallback(evt => { _retryCount = evt.newValue < 1 ? 1 : evt.newValue; });

            visualElement.Add(retryCountField);

            return visualElement;
        }

        [MenuItem("Azzmurr/Batch Avatar Uploader")]
        public static void ShowWindow() {
            var window = GetWindow<VRChatBatchAvatarUploader>("Batch Avatar Uploader");
            window.minSize = new Vector2(600, 400);
        }

        private void RescanSelectedFolder() {
            MainListView.itemsSource = ScanFolder(SelectedFolder);
            MainListView.RefreshItems();
        }

        private static List<AvatarEntry> ScanFolder(Object folder) {
            if (folder == null) {
                return new List<AvatarEntry>();
            }

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            var currentScene = EditorSceneManager.GetSceneManagerSetup();

            var folderPath = AssetDatabase.GetAssetPath(folder);
            var sceneGUIDs = AssetDatabase.FindAssets("t:Scene", new[] { folderPath });
            var allAvatars = new List<AvatarEntry>();

            foreach (var guid in sceneGUIDs) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                var descriptors = scene.GetRootGameObjects()
                    .SelectMany(go => go.GetComponentsInChildren<VRCAvatarDescriptor>());

                allAvatars.AddRange(
                    descriptors
                        .Select(descriptor => new AvatarEntry(descriptor.gameObject))
                        .Where(entry => !string.IsNullOrEmpty(entry.BlueprintId))
                );

                EditorSceneManager.CloseScene(scene, true);
            }

            foreach (var entry in allAvatars) {
                entry.Index = allAvatars.IndexOf(entry);
            }
            
            EditorSceneManager.RestoreSceneManagerSetup(currentScene);

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
            var currentScene = EditorSceneManager.GetSceneManagerSetup();

            try {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

                EditorApplication.ExecuteMenuItem("VRChat SDK/Show Control Panel");

                if (!VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var builder)) {
                    EditorUtility.DisplayDialog("Batch Avatar Uploader",
                        "VRChat SDK Builder not found. Please open the VRChat SDK Control Panel first.", "OK");
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
                    entry.Pending("");
                }

                foreach (var entry in toUpload) {
                    entry.Pending("Pending to build");
                }

                MainListView.RefreshItems();

                foreach (var entry in toUpload) {
                    if (_cts.IsCancellationRequested) {
                        entry.GenericError("Cancelled");
                        MainListView.RefreshItems();
                        continue;
                    }

                    _statusLabel.text = $"Uploading {completed + failed + 1}/{toUpload.Count}: {entry.Name}...";

                    var success = _retryFailedUploads
                        ? await UploadAvatarWithRetry(builder, entry, _cts.Token)
                        : await UploadAvatar(builder, entry, _cts.Token);
                    if (success) completed++;
                    else failed++;
                }

                sw.Stop();
                var t = sw.Elapsed;

                _statusLabel.text =
                    $"Done. {completed} uploaded, {failed} failed. Time taken: {t.Hours:D2} hour(s) {t.Minutes:D2} minute(s) {t.Seconds:D2} second(s)";
            }
            catch (Exception e) {
                Debug.LogException(e);
            }
            finally {
                ActionsListView.SetEnabled(true);
                EditorSceneManager.RestoreSceneManagerSetup(currentScene);
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task<bool> UploadAvatarWithRetry(IVRCSdkAvatarBuilderApi builder, AvatarEntry entry,
            CancellationToken ct) {
            for (var i = 0; i < _retryCount; i++) {
                var success = await UploadAvatar(builder, entry, ct);
                if (success) return true;

                if (entry.State == AvatarEntryState.UploadError) {
                    await Task.Delay(_retryDelay, ct);
                }
                else {
                    return false;
                }
            }

            return false;
        }

        private async Task<bool> UploadAvatar(IVRCSdkAvatarBuilderApi builder, AvatarEntry entry,
            CancellationToken ct) {
            var scene = EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(entry.AvatarScene),
                OpenSceneMode.Additive);
            entry.InProgress("Starting...");
            MainListView.RefreshItem(entry.Index);

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
                var added = await AddCopyrightAgreement(entry.BlueprintId);

                if (!added) {
                    entry.GenericError("Failed to add copyright agreement");
                    MainListView.RefreshItem(entry.Index);
                    return false;
                }

                VRCAvatarDescriptor targetDescriptor = null;
                foreach (var root in scene.GetRootGameObjects()) {
                    var descs = root.GetComponentsInChildren<VRCAvatarDescriptor>(true);
                    targetDescriptor = descs.FirstOrDefault(d =>
                        d.TryGetComponent<PipelineManager>(out var p) && p.blueprintId == entry.BlueprintId);
                    if (targetDescriptor != null) {
                        avatarObject = targetDescriptor.gameObject;
                        break;
                    }

                    ;
                }

                if (targetDescriptor == null) {
                    Debug.LogWarning(
                        $"Batch Avatar Uploader: Could not find avatar '{entry.Name}' in scene '{entry.AvatarScene.name}'");
                    entry.GenericError("Failed to find avatar");
                    MainListView.RefreshItems();
                    return false;
                }

                var avatarMeta = new AvatarMeta(avatarObject);

                if (_setBestPCTextureFormatBeforeUpload) {
                    avatarMeta.SetBestPCTexturesFormat();
                }

                if (_setCrunchPCTextureFormatBeforeUpload) {
                    avatarMeta.CrunchThemAll();
                }

                onBuildStart = (_, _) => RunOnMainThread(() => {
                    entry.InProgress("Building...");
                    MainListView.RefreshItem(entry.Index);
                });
                onBuildProgress = (_, m) => RunOnMainThread(() => {
                    entry.InProgress(m);
                    MainListView.RefreshItem(entry.Index);
                });
                onBuildSuccess = (_, m) => RunOnMainThread(() => {
                    entry.InProgress(m);
                    MainListView.RefreshItem(entry.Index);
                });
                onBuildError = (_, m) => RunOnMainThread(() => {
                    entry.BuildError(m);
                    MainListView.RefreshItem(entry.Index);
                });
                onUploadStart = (_, _) => RunOnMainThread(() => {
                    entry.InProgress("Uploading...");
                    MainListView.RefreshItem(entry.Index);
                });
                onUploadProgress = (_, m) => RunOnMainThread(() => {
                    entry.InProgress(m.status);
                    MainListView.RefreshItem(entry.Index);
                });
                onUploadSuccess = (_, _) => RunOnMainThread(() => {
                    entry.Success("Uploaded!");
                    MainListView.RefreshItem(entry.Index);
                });
                onUploadError = (_, m) => RunOnMainThread(() => {
                    entry.UploadError(m);
                    MainListView.RefreshItem(entry.Index);
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
                if (entry.State is not AvatarEntryState.UploadError) {
                    entry.UploadError(e.ErrorMessage);
                    MainListView.RefreshItems();
                }

                Debug.LogError(e.Message + e.StackTrace);
                return false;
            }

            catch (Exception e) {
                if (entry.State is not (AvatarEntryState.BuildError or AvatarEntryState.UploadError
                    or AvatarEntryState.GenericError)) {
                    entry.GenericError(e.Message);
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

        private static async Task<bool> AddCopyrightAgreement(string blueprint) {
            try {
                await VRCApi.ContentUploadConsent(new VRCAgreement {
                    AgreementCode = "content.copyright.owned",
                    AgreementFulltext = AgreementText,
                    ContentId = blueprint,
                    Version = 1,
                });

                const string key = "VRCSdkControlPanel.CopyrightAgreement.ContentList";
                var keyText = SessionState.GetString(key, "");
                var list = string.IsNullOrEmpty(keyText)
                    ? new List<string>()
                    : SessionState.GetString(key, "").Split(';').ToList();

                if (list.Contains(blueprint)) {
                    return true;
                }

                list.Add(blueprint);
                SessionState.SetString(key, string.Join(";", list));

                return true;
            }
            catch (Exception e) {
                return false;
            }
        }

        private class AvatarEntry {
            public readonly SceneAsset AvatarScene;
            public readonly string BlueprintId;
            public readonly string Name;
            public bool Selected;
            public AvatarEntryState State;
            public string Message;
            public TimeSpan TimeTaken;
            public int Index;

            private Stopwatch _stopwatch;

            public AvatarEntry(GameObject avatar) {
                Name = avatar.name;
                AvatarScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(avatar.scene.path);

                var pipeline = avatar.GetComponent<PipelineManager>();
                if (pipeline) {
                    BlueprintId = pipeline.blueprintId;
                }

                Selected = false;
                State = AvatarEntryState.Pending;
            }

            public void Pending(string message) {
                State = AvatarEntryState.Pending;
                Message = message;
            }

            public void BuildError(string message) {
                State = AvatarEntryState.BuildError;
                Message = message;
                _stopwatch.Stop();
                TimeTaken = _stopwatch.Elapsed;
                _stopwatch = null;
            }

            public void UploadError(string message) {
                State = AvatarEntryState.UploadError;
                Message = message;
                _stopwatch.Stop();
                TimeTaken = _stopwatch.Elapsed;
                _stopwatch = null;
            }

            public void GenericError(string message) {
                State = AvatarEntryState.GenericError;
                Message = message;
                _stopwatch.Stop();
                TimeTaken = _stopwatch.Elapsed;
                _stopwatch = null;
            }

            public void Success(string message) {
                State = AvatarEntryState.Success;
                Message = message;
                _stopwatch.Stop();
                TimeTaken = _stopwatch.Elapsed;
                _stopwatch = null;
            }

            public void InProgress(string message) {
                State = AvatarEntryState.InProgress;
                Message = message;
                
                _stopwatch ??= Stopwatch.StartNew();
            }
        }

        private enum AvatarEntryState {
            Pending,
            GenericError,
            BuildError,
            UploadError,
            InProgress,
            Success,
        }
    }
}
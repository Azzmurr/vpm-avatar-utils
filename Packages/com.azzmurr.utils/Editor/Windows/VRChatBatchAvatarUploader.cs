using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.Core;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3A.Editor;
using VRC.SDKBase.Editor.Api;

namespace Azzmurr.Utils {
    internal class VRChatBatchAvatarUploader : EditorWindow {
        private List<AvatarEntry> _avatars = new();
        private Button _cancelButton;
        private CancellationTokenSource _cts;
        private bool _isUploading;
        private ListView _listView;
        private Label _statusLabel;
        private Button _uploadAllButton;
        private Button _uploadSelectedButton;

        private void OnDestroy() {
            _cts?.Cancel();
        }

        private void CreateGUI() {
            var root = rootVisualElement;
            root.style.paddingTop = 4;
            root.style.paddingBottom = 4;
            root.style.paddingLeft = 4;
            root.style.paddingRight = 4;

            // Search controls
            var searchRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };

            var scanProjectButton = new Button(ScanProject) { text = "Scan Entire Project", style = { flexGrow = 1 } };
            var scanFolderButton = new Button(ScanSelectedFolder) { text = "Scan Selected Folder", style = { flexGrow = 1 } };

            searchRow.Add(scanProjectButton);
            searchRow.Add(scanFolderButton);
            root.Add(searchRow);

            // Select/Deselect controls
            var selectRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };
            selectRow.Add(new Button(() => SetAllSelected(true)) { text = "Select All", style = { flexGrow = 1 } });
            selectRow.Add(new Button(() => SetAllSelected(false)) { text = "Deselect All", style = { flexGrow = 1 } });
            root.Add(selectRow);

            // Avatar list
            _listView = new ListView {
                makeItem = MakeAvatarItem,
                bindItem = BindAvatarItem,
                itemsSource = _avatars,
                fixedItemHeight = 28,
                selectionType = SelectionType.None,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                showBorder = true,
                style = { flexGrow = 1, marginBottom = 4 }
            };
            root.Add(_listView);

            // Status
            _statusLabel = new Label("Ready") { style = { marginBottom = 4 } };
            root.Add(_statusLabel);

            // Upload controls
            var uploadRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            _uploadSelectedButton = new Button(() => StartUpload(false)) { text = "Upload Selected", style = { flexGrow = 1 } };
            _uploadAllButton = new Button(() => StartUpload(true)) { text = "Upload All", style = { flexGrow = 1 } };
            _cancelButton = new Button(CancelUpload) { text = "Cancel", style = { flexGrow = 1 }, visible = false };

            uploadRow.Add(_uploadSelectedButton);
            uploadRow.Add(_uploadAllButton);
            uploadRow.Add(_cancelButton);
            root.Add(uploadRow);
        }

        [MenuItem("Azzmurr/Batch Avatar Uploader")]
        public static void ShowWindow() {
            var window = GetWindow<VRChatBatchAvatarUploader>("Batch Avatar Uploader");
            window.minSize = new Vector2(600, 400);
        }

        private VisualElement MakeAvatarItem() {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            var toggle = new Toggle { name = "select-toggle", style = { marginRight = 4 } };
            row.Add(toggle);

            var nameLabel = new Label { name = "avatar-name", style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft } };
            row.Add(nameLabel);

            var idLabel = new Label { name = "blueprint-id", style = { width = 200, unityTextAlign = TextAnchor.MiddleLeft, color = Color.gray } };
            row.Add(idLabel);

            var statusLabel = new Label { name = "avatar-status", style = { width = 120, unityTextAlign = TextAnchor.MiddleRight } };
            row.Add(statusLabel);

            var uploadButton = new Button { name = "upload-single", text = "Upload", style = { width = 60 } };
            row.Add(uploadButton);

            return row;
        }

        private void BindAvatarItem(VisualElement element, int index) {
            if (index < 0 || index >= _avatars.Count) return;
            var entry = _avatars[index];

            var toggle = element.Q<Toggle>("select-toggle");
            toggle.SetValueWithoutNotify(entry.Selected);
            toggle.RegisterValueChangedCallback(evt => entry.Selected = evt.newValue);

            element.Q<Label>("avatar-name").text = entry.Name;
            element.Q<Label>("blueprint-id").text = string.IsNullOrEmpty(entry.BlueprintId) ? "(new avatar)" : entry.BlueprintId;
            element.Q<Label>("avatar-status").text = entry.Status ?? "";

            var uploadBtn = element.Q<Button>("upload-single");
            uploadBtn.SetEnabled(!_isUploading);
            uploadBtn.clickable = new Clickable(() => UploadSingle(index));
        }

        private void ScanProject() {
            ScanFolder("Assets");
        }

        private void ScanSelectedFolder() {
            var selected = Selection.activeObject;
            if (selected == null) {
                EditorUtility.DisplayDialog("Batch Avatar Uploader", "Please select a folder in the Project window first.", "OK");
                return;
            }

            var path = AssetDatabase.GetAssetPath(selected);
            if (!AssetDatabase.IsValidFolder(path)) {
                path = Path.GetDirectoryName(path);
            }

            if (string.IsNullOrEmpty(path)) {
                EditorUtility.DisplayDialog("Batch Avatar Uploader", "Could not determine folder path.", "OK");
                return;
            }

            ScanFolder(path);
        }

        private void ScanFolder(string folderPath) {
            _avatars.Clear();

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
            foreach (var guid in guids) {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null) continue;

                var descriptor = prefab.GetComponent<VRCAvatarDescriptor>();
                if (descriptor == null) continue;

                var pm = prefab.GetComponent<PipelineManager>();
                var blueprintId = pm != null ? pm.blueprintId : "";

                _avatars.Add(new AvatarEntry {
                    Prefab = prefab,
                    Name = prefab.name,
                    BlueprintId = blueprintId,
                    AssetPath = assetPath,
                    Selected = true,
                    Status = ""
                });
            }

            // Also scan scene for non-prefab avatars
            var sceneDescriptors = FindObjectsOfType<VRCAvatarDescriptor>();
            foreach (var descriptor in sceneDescriptors) {
                // Skip if already found as prefab
                if (_avatars.Any(a => a.Prefab == descriptor.gameObject)) continue;

                var pm = descriptor.GetComponent<PipelineManager>();
                var blueprintId = pm != null ? pm.blueprintId : "";

                _avatars.Add(new AvatarEntry {
                    Prefab = descriptor.gameObject,
                    Name = descriptor.gameObject.name,
                    BlueprintId = blueprintId,
                    AssetPath = "",
                    Selected = true,
                    Status = ""
                });
            }

            _listView.itemsSource = _avatars;
            _listView.Rebuild();
            _statusLabel.text = $"Found {_avatars.Count} avatar(s)";
        }

        private void SetAllSelected(bool selected) {
            foreach (var entry in _avatars) entry.Selected = selected;
            _listView.Rebuild();
        }

        private async void UploadSingle(int index) {
            if (_isUploading || index < 0 || index >= _avatars.Count) return;

            if (!VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var builder)) {
                EditorUtility.DisplayDialog("Batch Avatar Uploader", "VRChat SDK Builder not found. Please open the VRChat SDK Control Panel first.", "OK");
                return;
            }

            _isUploading = true;
            _cts = new CancellationTokenSource();
            UpdateUploadUI(true);

            var entry = _avatars[index];
            await UploadAvatar(builder, entry, _cts.Token);

            _isUploading = false;
            _cts = null;
            UpdateUploadUI(false);
            _listView.Rebuild();
        }

        private async void StartUpload(bool all) {
            if (_isUploading) return;

            if (!VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var builder)) {
                EditorUtility.DisplayDialog("Batch Avatar Uploader", "VRChat SDK Builder not found. Please open the VRChat SDK Control Panel first.", "OK");
                return;
            }

            var toUpload = all ? _avatars : _avatars.Where(a => a.Selected).ToList();
            if (toUpload.Count == 0) {
                EditorUtility.DisplayDialog("Batch Avatar Uploader", "No avatars selected for upload.", "OK");
                return;
            }

            _isUploading = true;
            _cts = new CancellationTokenSource();
            UpdateUploadUI(true);

            var completed = 0;
            var failed = 0;

            foreach (var entry in toUpload) {
                if (_cts.IsCancellationRequested) {
                    entry.Status = "Cancelled";
                    continue;
                }

                _statusLabel.text = $"Uploading {completed + failed + 1}/{toUpload.Count}: {entry.Name}...";

                var success = await UploadAvatar(builder, entry, _cts.Token);
                if (success) completed++;
                else failed++;

                _listView.Rebuild();
            }

            _statusLabel.text = $"Done. {completed} uploaded, {failed} failed.";
            _isUploading = false;
            _cts = null;
            UpdateUploadUI(false);
            _listView.Rebuild();
        }

        private async Task<bool> UploadAvatar(IVRCSdkAvatarBuilderApi builder, AvatarEntry entry, CancellationToken ct) {
            try {
                entry.Status = "Uploading...";
                _listView.Rebuild();

                var avatar = new VRCAvatar { Name = entry.Name };
                await builder.BuildAndUpload(entry.Prefab, avatar, null, ct);

                entry.Status = "Done";
                return true;
            }
            catch (OperationCanceledException) {
                entry.Status = "Cancelled";
                return false;
            }
            catch (Exception e) {
                entry.Status = "Error";
                Debug.LogError($"[Batch Avatar Uploader] Failed to upload {entry.Name}: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        private void CancelUpload() {
            _cts?.Cancel();
            _statusLabel.text = "Cancelling...";
        }

        private void UpdateUploadUI(bool uploading) {
            _uploadSelectedButton.SetEnabled(!uploading);
            _uploadAllButton.SetEnabled(!uploading);
            _cancelButton.visible = uploading;
            _listView.Rebuild();
        }

        private class AvatarEntry {
            public string AssetPath;
            public string BlueprintId;
            public string Name;
            public GameObject Prefab;
            public bool Selected;
            public string Status;
        }
    }
}
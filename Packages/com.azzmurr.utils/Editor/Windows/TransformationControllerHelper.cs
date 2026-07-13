using System;
using System.Collections.Generic;
using System.Linq;
using com.vrcfury.api;
using com.vrcfury.api.Components;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Azzmurr.Utils {
    internal class TransformationControllerHelper : CommonEditorWindow {
        private string controllerAnimsPath;

        // TF Time
        private AnimationClip _tfTime20Animation;
        private AnimationClip _tfTime15Animation;
        private AnimationClip _tfTime10Animation;
        private AnimationClip _tfTime5Animation;
        private AnimationClip _tfTime2Animation;
        private AnimationClip _tfTime1Animation;

        //TF Time Reverse
        private AnimationClip _tfTimeReverse20Animation;
        private AnimationClip _tfTimeReverse15Animation;
        private AnimationClip _tfTimeReverse10Animation;
        private AnimationClip _tfTimeReverse5Animation;
        private AnimationClip _tfTimeReverse2Animation;
        private AnimationClip _tfTimeReverse1Animation;

        // COMMON
        private AnimationClip _audioSourceOnAnimation;
        private AnimationClip _physbonesOffAnimation;

        // CTF
        private AnimationClip _cookieTfAnimation;
        private AnimationClip _horseTfAnimation;
        private AnimationClip _knotTfAnimation;
        private AnimationClip _tapperTfAnimation;

        // POOL TOY
        private AnimationClip _accessoriesAnimation;
        private AnimationClip _mouthDoNotOpenAnimation;
        private AnimationClip _mouthOpenAnimation;
        private AnimationClip _valveDoNotLightbulbAnimation;
        private AnimationClip _valveLightbulbAnimation;
        private AnimationClip _valveDoNotShowAnimation;
        private AnimationClip _valveShowAnimation;

        private List<DropdownChoice<GameObject>> _choices;
        private Dictionary<string, Func<ObjectMeta, bool>> _possibleModules;

        private void OnEnable() {
            controllerAnimsPath = $"{rootPath}/Controller/Anims";

            _tfTime20Animation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/TF Time/TF Time 20 min.anim");
            _tfTime15Animation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/TF Time/TF Time 15 min.anim");
            _tfTime10Animation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/TF Time/TF Time 10 min.anim");
            _tfTime5Animation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/TF Time/TF Time 5 min.anim");
            _tfTime2Animation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/TF Time/TF Time 2 min.anim");
            _tfTime1Animation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/TF Time/TF Time 1 min.anim");

            _tfTimeReverse20Animation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/TF Time Reverse/TF Time Reverse 20 min.anim");
            _tfTimeReverse15Animation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/TF Time Reverse/TF Time Reverse 15 min.anim");
            _tfTimeReverse10Animation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/TF Time Reverse/TF Time Reverse 10 min.anim");
            _tfTimeReverse5Animation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/TF Time Reverse/TF Time Reverse 5 min.anim");
            _tfTimeReverse2Animation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/TF Time Reverse/TF Time Reverse 2 min.anim");
            _tfTimeReverse1Animation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/TF Time Reverse/TF Time Reverse 1 min.anim");

            _audioSourceOnAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/Audio Source On.anim");
            _physbonesOffAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/Physbones/Physbones Off.anim");

            _cookieTfAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/CTF/Cookie Transformation.anim");
            _horseTfAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/CTF/Horse Transformation.anim");
            _knotTfAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/CTF/Knot Trnasformation.anim");
            _tapperTfAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/CTF/Tapper Transformation.anim");

            _accessoriesAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/Pool Toy/Accessories.anim");
            _mouthDoNotOpenAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/Pool Toy/Mouth Do Not Open.anim");
            _mouthOpenAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/Pool Toy/Mouth Open.anim");
            _valveDoNotLightbulbAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/Pool Toy/Valve Do Not lightbulb.anim");
            _valveLightbulbAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/Pool Toy/Valve lightbulb.anim");
            _valveDoNotShowAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/Pool Toy/Valve Do Not Show.anim");
            _valveShowAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{controllerAnimsPath}/Pool Toy/Valve Show.anim");

            _choices = new List<DropdownChoice<GameObject>> {
                new("TF Time/20 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation })),
                new("TF Time/15 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime15Animation })),
                new("TF Time/10 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime10Animation })),
                new("TF Time/5 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime5Animation })),
                new("TF Time/2 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime2Animation })),
                new("TF Time/1 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime1Animation })),

                new("TF Time Reverse/20 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse20Animation })),
                new("TF Time Reverse/15 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse15Animation })),
                new("TF Time Reverse/10 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse10Animation })),
                new("TF Time Reverse/5 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse5Animation })),
                new("TF Time Reverse/2 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse2Animation })),
                new("TF Time Reverse/1 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse1Animation })),

                new("CTF/Canine", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation, _knotTfAnimation })),
                new("CTF/Cookie", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation, _cookieTfAnimation })),
                new("CTF/Taper", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation, _tapperTfAnimation })),
                new("CTF/Horse", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation, _horseTfAnimation })),

                new("Pooltoy/Body", (gameObject) => {
                    AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation, _mouthOpenAnimation });
                    AddMouthDoNotOpenComponents(gameObject, new[] { _mouthDoNotOpenAnimation });
                }),
                new("Pooltoy/Accessories", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _accessoriesAnimation })),
                new("Pooltoy/Valve", (gameObject) => {
                    AddMainVrcFurryComponents(gameObject, new[] { _valveShowAnimation });
                    AddMouthDoNotOpenComponents(gameObject, new[] { _valveDoNotShowAnimation });
                }),
                new("Pooltoy/Valve Bulb", (gameObject) => {
                    AddMainVrcFurryComponents(gameObject, new[] { _valveLightbulbAnimation });
                    AddMouthDoNotOpenComponents(gameObject, new[] { _valveDoNotLightbulbAnimation });
                }),


                // new("Seat/Mat Swap on seat toggle", (gameObject) => ),
            };

            _possibleModules = new Dictionary<string, Func<ObjectMeta, bool>> {
                { "Automatic TF", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto" },
                { "Manual TF", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Manual" },
                { "Automatic TF Reversed", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto Reverse" },
                { "Material Swap", component => component.IsVrcFuryToggle && component.VrcFuryExclusiveTag == "TF/Material Swap" },
                { "Position Swap", component => component.IsVrcFuryToggle && component.VrcFuryExclusiveTag == "TF/Start Position" },
                { "Disabled Mouth transform", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Disable Mouth Transform" },
                { "Seat Toggle", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Seat/Toggle" },
            };
        }

        private void CreateGUI() {
            var root = CreateRootUIElement();
            var avatarSelector = CreateAvatarSelectorField(avatar => { MainListView.itemsSource = Avatar?.meshRenderers; });
            var actions = CreateActionsGUI();
            var list = CreateListGUI();


            root.Add(avatarSelector);
            root.Add(actions);
            root.Add(list);
        }

        private MultiColumnListView CreateListGUI() {
            CreateMainListGUI();

            MainListView.columns.Add(new Column {
                title = "Mesh Renderer",
                width = 200,
                makeCell = () => new ObjectField {
                    objectType = typeof(GameObject)
                },
                bindCell = (element, index) => {
                    var field = (ObjectField)element;
                    var meta = MainListView.itemsSource[index] as SkinnedMeshRendererMeta;
                    field.value = meta.GameObject;
                }
            });

            MainListView.columns.Add(new Column {
                title = "Related PhysBones",
                width = 200,
                makeCell = () => new Foldout { text = "PhysBones", value = false },
                bindCell = (element, index) => {
                    var foldout = (Foldout)element;
                    var meta = MainListView.itemsSource[index] as SkinnedMeshRendererMeta;
                    var count = 0;

                    RegisterCallBack<bool>(foldout, (e) => { DoAndRedraw(index, () => meta.Expanded = e.newValue); });

                    foldout.Clear();

                    meta.PhysBones.ForEach(physBone => {
                        count++;

                        foldout.Add(new ObjectField {
                            objectType = typeof(GameObject),
                            value = physBone,
                            style = {
                                flexGrow = 1,
                                flexShrink = 1,
                            }
                        });
                    });
                },
                unbindCell = (element, index) => {
                    var foldout = (Foldout)element;
                    UnregisterCallBack<bool>(foldout);
                }
            });

            MainListView.columns.Add(new Column {
                title = "Add Modules",
                width = 250,
                makeCell = () => new VisualElement(),
                bindCell = (element, index) => {
                    var meta = MainListView.itemsSource[index] as SkinnedMeshRendererMeta;
                    element.Clear();
                    var button = new Button(() => {
                        var menu = new GenericMenu();
                        _choices.ForEach(choice => { menu.AddItem(new GUIContent(choice.Name), false, () => { DoAndRedraw(index, () => choice.Action(meta.GameObject)); }); });

                        menu.ShowAsContext();
                    }) { text = "Add Module" };

                    element.Add(button);

                    if (meta.Expanded) {
                        meta.PhysBones.ForEach(physBone => {
                            element.Add(new Button(() => { DoAndRedraw(index, () => AddSeatVrcFurryComponents(physBone, new[] { _physbonesOffAnimation })); }) { text = "Disable PhysBones when seat on" });
                        });
                    }
                },
            });

            MainListView.columns.Add(new Column {
                title = "Installed Modules",
                width = 500,
                makeCell = () => new VisualElement(),
                bindCell = (element, index) => {
                    var meta = MainListView.itemsSource[index] as SkinnedMeshRendererMeta;
                    element.Clear();

                    var mainModules = new VisualElement() { style = { flexDirection = FlexDirection.Row } };

                    meta.VrcFuryComponents.ForEach(vrcFuryComponent => {
                        foreach (var (key, comparator) in _possibleModules) {
                            if (comparator(vrcFuryComponent)) {
                                mainModules.Add(new Button(() => DoAndRedraw(index, () => DestroyImmediate(vrcFuryComponent.Component))) { text = $"{key} (remove)" });
                            }
                        }
                    });

                    if (mainModules.childCount == 0) {
                        mainModules.Add(new Button() { text = "None" });
                    }

                    element.Add(mainModules);

                    if (meta.Expanded) {
                        meta.PhysBones.ForEach(physBone => {
                            var physBoneModuls = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
                            meta.GetVrcFuryComponents(physBone).ForEach(vrcFuryComponent => {
                                foreach (var (key, comparator) in _possibleModules) {
                                    if (comparator(vrcFuryComponent)) {
                                        physBoneModuls.Add(new Button(() => DoAndRedraw(index, () => DestroyImmediate(vrcFuryComponent.Component))) { text = $"{key} (remove)" });
                                    }
                                }
                            });

                            if (physBoneModuls.childCount == 0) {
                                physBoneModuls.Add(new Button() { text = "None" });
                            }

                            element.Add(physBoneModuls);
                        });
                    }
                }
            });

            MainListView.columns.Add(new Column {
                title = "Warnings",
                width = 400,
                makeCell = () => new Label(),
                bindCell = (element, index) => {
                    var meta = MainListView.itemsSource[index] as SkinnedMeshRendererMeta;
                    var label = (Label)element;

                    label.text = meta.HasSlidersNotPassthrough ? "Some manual sliders have passthrough toggled off. It will result automatic TF not working" : "";
                }
            });

            return MainListView;
        }

        private MultiColumnListView CreateActionsGUI() {
            CreateActionsListGUI();

            ActionsListView.itemsSource = new List<ActionGroup> {
                new() {
                    Name = "Avatar",
                    Actions = new List<Button> {
                        new(() => { DoAndRedraw(() => { Avatar.Recalculate(); }); }) { text = "Reload" },
                    }
                },
            };

            return ActionsListView;
        }

        [MenuItem("Azzmurr/TF Controller")]
        public static void Init() {
            var window = (TransformationControllerHelper)GetWindow(typeof(TransformationControllerHelper));
            window.titleContent = new GUIContent("TF Controller Helper");
            window.Show();
        }

        [MenuItem("GameObject/Azzmurr/TF Controller", false)]
        public static void ShowFromSelection() {
            var window = (TransformationControllerHelper)GetWindow(typeof(TransformationControllerHelper));
            window.titleContent = new GUIContent("Transformation Controller Helper");
            window.SetAvatar(Selection.activeGameObject);
            window.Show();
        }

        private void AddMouthDoNotOpenComponents(GameObject gameObject, AnimationClip[] clips) {
            AddMainVrcFurryComponent("TF/Disable Mouth Transform", gameObject, clips);
        }

        private FuryToggle AddSeatVrcFurryComponents(GameObject gameObject, AnimationClip[] clips) {
            return AddMainVrcFurryComponent("TF/Seat/Toggle", gameObject, clips);
        }

        private void AddMainReversedVrcFuryComponent(GameObject gameObject, AnimationClip[] clips) {
            AddMainVrcFurryComponent("TF/Auto Reverse", gameObject, clips);
        }

        private void AddMainVrcFurryComponents(GameObject gameObject, AnimationClip[] clips) {
            AddMainVrcFurryComponent("TF/Auto", gameObject, clips);
            var manualToggle = AddMainVrcFurryComponent("TF/Manual", gameObject, clips);
            manualToggle.SetSlider();
            new ObjectMeta(manualToggle).Get("c").Set("sliderInactiveAtZero", true);
        }

        private FuryToggle AddMainVrcFurryComponent(string toggleName, GameObject gameObject, AnimationClip[] clips) {
            var toggle = FuryComponents.CreateToggle(gameObject);
            toggle.SetGlobalParameter(toggleName);

            clips.ToList().ForEach(clip => { toggle.GetActions().AddAnimationClip(clip); });

            return toggle;
        }
    }
}

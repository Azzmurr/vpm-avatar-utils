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
                new("TF Time/1/20 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation }, 1)),
                new("TF Time/1/15 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime15Animation }, 1)),
                new("TF Time/1/10 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime10Animation }, 1)),
                new("TF Time/1/5 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime5Animation }, 1)),
                new("TF Time/1/2 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime2Animation }, 1)),
                new("TF Time/1/1 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime1Animation }, 1)),

                new("TF Time/2/20 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation }, 2)),
                new("TF Time/2/15 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime15Animation }, 2)),
                new("TF Time/2/10 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime10Animation }, 2)),
                new("TF Time/2/5 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime5Animation }, 2)),
                new("TF Time/2/2 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime2Animation }, 2)),
                new("TF Time/2/1 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime1Animation }, 2)),

                new("TF Time/3/20 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation }, 3)),
                new("TF Time/3/15 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime15Animation }, 3)),
                new("TF Time/3/10 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime10Animation }, 3)),
                new("TF Time/3/5 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime5Animation }, 3)),
                new("TF Time/3/2 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime2Animation }, 3)),
                new("TF Time/3/1 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime1Animation }, 3)),

                new("TF Time/4/20 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation }, 4)),
                new("TF Time/4/15 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime15Animation }, 4)),
                new("TF Time/4/10 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime10Animation }, 4)),
                new("TF Time/4/5 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime5Animation }, 4)),
                new("TF Time/4/2 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime2Animation }, 4)),
                new("TF Time/4/1 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime1Animation }, 4)),

                new("TF Time/5/20 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation }, 5)),
                new("TF Time/5/15 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime15Animation }, 5)),
                new("TF Time/5/10 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime10Animation }, 5)),
                new("TF Time/5/5 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime5Animation }, 5)),
                new("TF Time/5/2 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime2Animation }, 5)),
                new("TF Time/5/1 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime1Animation }, 5)),

                new("TF Time/6/20 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation }, 6)),
                new("TF Time/6/15 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime15Animation }, 6)),
                new("TF Time/6/10 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime10Animation }, 6)),
                new("TF Time/6/5 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime5Animation }, 6)),
                new("TF Time/6/2 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime2Animation }, 6)),
                new("TF Time/6/1 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime1Animation }, 6)),

                new("TF Time/7/20 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation }, 7)),
                new("TF Time/7/15 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime15Animation }, 7)),
                new("TF Time/7/10 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime10Animation }, 7)),
                new("TF Time/7/5 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime5Animation }, 7)),
                new("TF Time/7/2 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime2Animation }, 7)),
                new("TF Time/7/1 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime1Animation }, 7)),

                new("TF Time/8/20 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation }, 8)),
                new("TF Time/8/15 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime15Animation }, 8)),
                new("TF Time/8/10 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime10Animation }, 8)),
                new("TF Time/8/5 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime5Animation }, 8)),
                new("TF Time/8/2 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime2Animation }, 8)),
                new("TF Time/8/1 min", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime1Animation }, 8)),

                new("TF Time Reverse/1/20 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse20Animation }, 1)),
                new("TF Time Reverse/1/15 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse15Animation }, 1)),
                new("TF Time Reverse/1/10 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse10Animation }, 1)),
                new("TF Time Reverse/1/5 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse5Animation }, 1)),
                new("TF Time Reverse/1/2 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse2Animation }, 1)),
                new("TF Time Reverse/1/1 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse1Animation }, 1)),

                new("TF Time Reverse/2/20 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse20Animation }, 2)),
                new("TF Time Reverse/2/15 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse15Animation }, 2)),
                new("TF Time Reverse/2/10 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse10Animation }, 2)),
                new("TF Time Reverse/2/5 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse5Animation }, 2)),
                new("TF Time Reverse/2/2 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse2Animation }, 2)),
                new("TF Time Reverse/2/1 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse1Animation }, 2)),

                new("TF Time Reverse/3/20 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse20Animation }, 3)),
                new("TF Time Reverse/3/15 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse15Animation }, 3)),
                new("TF Time Reverse/3/10 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse10Animation }, 3)),
                new("TF Time Reverse/3/5 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse5Animation }, 3)),
                new("TF Time Reverse/3/2 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse2Animation }, 3)),
                new("TF Time Reverse/3/1 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse1Animation }, 3)),

                new("TF Time Reverse/4/20 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse20Animation }, 4)),
                new("TF Time Reverse/4/15 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse15Animation }, 4)),
                new("TF Time Reverse/4/10 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse10Animation }, 4)),
                new("TF Time Reverse/4/5 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse5Animation }, 4)),
                new("TF Time Reverse/4/2 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse2Animation }, 4)),
                new("TF Time Reverse/4/1 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse1Animation }, 4)),

                new("TF Time Reverse/5/20 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse20Animation }, 5)),
                new("TF Time Reverse/5/15 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse15Animation }, 5)),
                new("TF Time Reverse/5/10 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse10Animation }, 5)),
                new("TF Time Reverse/5/5 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse5Animation }, 5)),
                new("TF Time Reverse/5/2 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse2Animation }, 5)),
                new("TF Time Reverse/5/1 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse1Animation }, 5)),

                new("TF Time Reverse/6/20 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse20Animation }, 6)),
                new("TF Time Reverse/6/15 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse15Animation }, 6)),
                new("TF Time Reverse/6/10 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse10Animation }, 6)),
                new("TF Time Reverse/6/5 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse5Animation }, 6)),
                new("TF Time Reverse/6/2 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse2Animation }, 6)),
                new("TF Time Reverse/6/1 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse1Animation }, 6)),

                new("TF Time Reverse/7/20 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse20Animation }, 7)),
                new("TF Time Reverse/7/15 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse15Animation }, 7)),
                new("TF Time Reverse/7/10 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse10Animation }, 7)),
                new("TF Time Reverse/7/5 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse5Animation }, 7)),
                new("TF Time Reverse/7/2 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse2Animation }, 7)),
                new("TF Time Reverse/7/1 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse1Animation }, 7)),

                new("TF Time Reverse/8/20 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse20Animation }, 8)),
                new("TF Time Reverse/8/15 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse15Animation }, 8)),
                new("TF Time Reverse/8/10 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse10Animation }, 8)),
                new("TF Time Reverse/8/5 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse5Animation }, 8)),
                new("TF Time Reverse/8/2 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse2Animation }, 8)),
                new("TF Time Reverse/8/1 min", (gameObject) => AddMainReversedVrcFuryComponent(gameObject, new[] { _tfTimeReverse1Animation }, 8)),

                new("CTF/Canine", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation, _knotTfAnimation }, 1)),
                new("CTF/Cookie", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation, _cookieTfAnimation }, 1)),
                new("CTF/Taper", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation, _tapperTfAnimation }, 1)),
                new("CTF/Horse", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation, _horseTfAnimation }, 1)),

                new("Pooltoy/Body", (gameObject) => {
                    AddMainVrcFurryComponents(gameObject, new[] { _tfTime20Animation, _mouthOpenAnimation }, 1);
                    AddMouthDoNotOpenComponents(gameObject, new[] { _mouthDoNotOpenAnimation });
                }),
                new("Pooltoy/Accessories", (gameObject) => AddMainVrcFurryComponents(gameObject, new[] { _accessoriesAnimation }, 1)),
                new("Pooltoy/Valve", (gameObject) => {
                    AddMainVrcFurryComponents(gameObject, new[] { _valveShowAnimation }, 1);
                    AddMouthDoNotOpenComponents(gameObject, new[] { _valveDoNotShowAnimation });
                }),
                new("Pooltoy/Valve Bulb", (gameObject) => {
                    AddMainVrcFurryComponents(gameObject, new[] { _valveLightbulbAnimation }, 1);
                    AddMouthDoNotOpenComponents(gameObject, new[] { _valveDoNotLightbulbAnimation });
                }),


                // new("Seat/Mat Swap on seat toggle", (gameObject) => ),
            };

            _possibleModules = new Dictionary<string, Func<ObjectMeta, bool>> {
                { "Automatic TF", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto" },
                { "Automatic TF 2", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto 2" },
                { "Automatic TF 3", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto 3" },
                { "Automatic TF 4", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto 4" },
                { "Automatic TF 5", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto 5" },
                { "Automatic TF 6", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto 6" },
                { "Automatic TF 7", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto 7" },
                { "Automatic TF 8", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto 8" },
                { "Manual TF", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Manual" },
                { "Manual TF 2", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Manual 2" },
                { "Manual TF 3", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Manual 3" },
                { "Manual TF 4", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Manual 4" },
                { "Manual TF 5", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Manual 5" },
                { "Manual TF 6", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Manual 6" },
                { "Manual TF 7", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Manual 7" },
                { "Manual TF 8", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Manual 8" },
                { "Automatic TF Reversed", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto Reverse" },
                { "Automatic TF Reversed 2", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto Reverse 2" },
                { "Automatic TF Reversed 3", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto Reverse 3" },
                { "Automatic TF Reversed 4", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto Reverse 4" },
                { "Automatic TF Reversed 5", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto Reverse 5" },
                { "Automatic TF Reversed 6", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto Reverse 6" },
                { "Automatic TF Reversed 7", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto Reverse 7" },
                { "Automatic TF Reversed 8", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Auto Reverse 8" },
                { "Disabled Mouth transform", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Disable Mouth Transform" },
                { "Seat Toggle", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter == "TF/Seat/Toggle" },
                { "Unknown TF element", component => component.IsVrcFuryToggle && component.VrcFuryGlobalParameter.StartsWith("TF") },
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
                            if (!comparator(vrcFuryComponent)) continue;
                            mainModules.Add(new Button(() => DoAndRedraw(index, () => DestroyImmediate(vrcFuryComponent.Component))) { text = $"{key} (remove)" });
                            break;
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
                                    if (!comparator(vrcFuryComponent)) continue;
                                    physBoneModuls.Add(new Button(() => DoAndRedraw(index, () => DestroyImmediate(vrcFuryComponent.Component))) { text = $"{key} (remove)" });
                                    break;
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

        private void AddMainReversedVrcFuryComponent(GameObject gameObject, AnimationClip[] clips, int index) {
            AddMainVrcFurryComponent("TF/Auto Reverse" + (index > 1 ? $" {index}" : ""), gameObject, clips);
        }

        private void AddMainVrcFurryComponents(GameObject gameObject, AnimationClip[] clips, int index) {
            AddMainVrcFurryComponent("TF/Auto" + (index > 1 ? $" {index}" : ""), gameObject, clips);
            var manualToggle = AddMainVrcFurryComponent("TF/Manual"  + (index > 1 ? $" {index}" : ""), gameObject, clips);
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

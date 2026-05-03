using System.Linq;
using com.vrcfury.api;
using com.vrcfury.api.Components;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Azzmurr.Utils {
    public class TransformationControllerHelper : EditorWindow {
        private const string VfNamespace = "VF";

        // COMMON
        private static readonly AnimationClip TfTimeAnimationPath = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/com.azzmurr.utils/Controller/Anims/TF Time.anim");
        private static readonly AnimationClip TfTime50AnimationPath = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/com.azzmurr.utils/Controller/Anims/TF Time 50%.anim");
        private static readonly AnimationClip AudioSourceOnAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/com.azzmurr.utils/Controller/Anims/Audio Source On.anim");
        private static readonly AnimationClip PhysbonesOffAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/com.azzmurr.utils/Controller/Anims/Physbones/Physbones Off.anim");

        // CTF
        private static readonly AnimationClip CookieTfAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/com.azzmurr.utils/Controller/Anims/Cookie Transformation.anim");
        private static readonly AnimationClip HorseTfAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/com.azzmurr.utils/Controller/Anims/Horse Transformation.anim");
        private static readonly AnimationClip KnotTfAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/com.azzmurr.utils/Controller/Anims/Knot Trnasformation.anim");
        private static readonly AnimationClip TapperTfAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/com.azzmurr.utils/Controller/Anims/Tapper Transformation.anim");

        // POOL TOY
        private static readonly AnimationClip AccessoriesAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/com.azzmurr.utils/Controller/Anims/Accessories.anim");
        private static readonly AnimationClip MouthDoNotOpenAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/com.azzmurr.utils/Controller/Anims/Mouth Do Not Open.anim");
        private static readonly AnimationClip MouthOpenAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/com.azzmurr.utils/Controller/Anims/Mouth Open.anim");
        private static readonly AnimationClip ValveDoNotLightbulbAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/com.azzmurr.utils/Controller/Anims/Valve Do Not lightbulb.anim");
        private static readonly AnimationClip ValveLightbulbAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/com.azzmurr.utils/Controller/Anims/Valve lightbulb.anim");
        private static readonly AnimationClip ValveDoNotShowAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/com.azzmurr.utils/Controller/Anims/Valve Do Not Show.anim");
        private static readonly AnimationClip ValveShowAnimation = AssetDatabase.LoadAssetAtPath<AnimationClip>("Packages/com.azzmurr.utils/Controller/Anims/Valve Show.anim");

        private GameObject _avatar;

        public GameObject Avatar {
            get => _avatar;
            set {
                _avatar = value;
                AvatarSelector.value = _avatar;
            }
        }

        private ObjectField AvatarSelector =>
            rootVisualElement.Q<ObjectField>("AvatarGameObject");

        private void CreateGUI() {
            var root = rootVisualElement;
            root.style.paddingRight = 8;
            root.style.paddingLeft = 8;

            var avatarSelector = new VisualElement { style = { flexShrink = 0 } };
            var avatarGameObjectField = new ObjectField {
                objectType = typeof(GameObject),
                value = _avatar,
                name = "AvatarGameObject",
                label = "Avatar: ",
                style = {
                    flexShrink = 0,
                    flexGrow = 1,
                }
            };

            var list = CreateListGUI();

            avatarGameObjectField.RegisterValueChangedCallback((e) => {
                var gameObject = e.newValue as GameObject;

                if (gameObject == null) {
                    list.itemsSource = null;
                    return;
                }

                var goList = gameObject
                    .GetComponentsInChildren<MonoBehaviour>(true)
                    .Where(c => c != null && c.GetType().Namespace != null && c.GetType().Namespace.StartsWith(VfNamespace))
                    .ToList()
                    .ConvertAll(c => new VFMeta(c))
                    .Where(meta => meta.IsToggle && meta.Toggle.GlobalParameter is "TF/Auto" or "TF/Manual" or "TF/Seat/Toggle" or "TF/Disable Mouth Transform")
                    .ToList();

                list.itemsSource = goList;
            });

            avatarSelector.Add(avatarGameObjectField);
            root.Add(avatarSelector);
            root.Add(list);
        }

        private MultiColumnListView CreateListGUI() {
            var list = new MultiColumnListView {
                name = "List",
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

            list.columns.Add(new Column {
                title = "GameObject",
                width = 80,
                makeCell = () => new ObjectField {
                    objectType = typeof(GameObject)
                },
                bindCell = (element, index) => {
                    var field = (ObjectField)element;
                    var meta = list.itemsSource[index] as VFMeta;

                    field.value = meta.GameObject;
                }
            });

            list.columns.Add(new Column {
                title = "Global Parameter",
                width = 80,
                makeCell = () => new Label(),
                bindCell = (element, index) => {
                    var field = (Label)element;
                    var meta = list.itemsSource[index] as VFMeta;

                    field.text = meta.Toggle.GlobalParameter;
                }
            });

            return list;
        }

        [MenuItem("Azzmurr/TF Controller/Helper")]
        public static void Init() {
            var window = (TransformationControllerHelper)GetWindow(typeof(TransformationControllerHelper));
            window.titleContent = new GUIContent("TF Controller Helper");
            window.Show();
        }

        [MenuItem("GameObject/Azzmurr/TF Controller/Helper", true, 0)]
        [MenuItem("GameObject/Azzmurr/TF Controller/Common/TF Time", true, 0)]
        [MenuItem("GameObject/Azzmurr/TF Controller/Common/TF Time 50", true, 0)]
        [MenuItem("GameObject/Azzmurr/TF Controller/CTF/Cookie TF", true, 0)]
        [MenuItem("GameObject/Azzmurr/TF Controller/CTF/Horse TF", true, 0)]
        [MenuItem("GameObject/Azzmurr/TF Controller/CTF/Tapper TF", true, 0)]
        [MenuItem("GameObject/Azzmurr/TF Controller/CTF/Knot TF", true, 0)]
        [MenuItem("GameObject/Azzmurr/TF Controller/CTF/Physbones Off when seat toggled", true, 0)]
        [MenuItem("GameObject/Azzmurr/TF Controller/CTF/Add mat swap on seat toggle on", true, 0)]
        [MenuItem("GameObject/Azzmurr/TF Controller/Pool Toy/Body", true, 0)]
        [MenuItem("GameObject/Azzmurr/TF Controller/Pool Toy/Accessories", true, 0)]
        [MenuItem("GameObject/Azzmurr/TF Controller/Pool Toy/Valve", true, 0)]
        [MenuItem("GameObject/Azzmurr/TF Controller/Pool Toy/Valve Bulb", true, 0)]
        public static bool CanShowFromSelection() {
            return Selection.activeGameObject != null;
        }

        [MenuItem("GameObject/Azzmurr/TF Controller/Helper", false, 0)]
        public static void ShowFromSelection() {
            var window = (TransformationControllerHelper)GetWindow(typeof(TransformationControllerHelper));
            window.titleContent = new GUIContent("Transformation Controller Helper");
            window.Avatar = Selection.activeGameObject;
            window.Show();
        }

        [MenuItem("GameObject/Azzmurr/TF Controller/Common/TF Time", false, 0)]
        public static void AddTfTime() {
            AddMainVrcFurryComponents(new[] { TfTimeAnimationPath });
        }

        [MenuItem("GameObject/Azzmurr/TF Controller/Common/TF Time 50", false, 0)]
        public static void AddTfTime50() {
            AddMainVrcFurryComponents(new[] { TfTime50AnimationPath });
        }

        [MenuItem("GameObject/Azzmurr/TF Controller/CTF/Cookie TF", false, 0)]
        public static void AddCookieTf() {
            AddMainVrcFurryComponents(new[] { TfTimeAnimationPath, CookieTfAnimation });
        }

        [MenuItem("GameObject/Azzmurr/TF Controller/CTF/Horse TF", false, 0)]
        public static void AddHorseTf() {
            AddMainVrcFurryComponents(new[] { TfTimeAnimationPath, HorseTfAnimation });
        }

        [MenuItem("GameObject/Azzmurr/TF Controller/CTF/Tapper TF", false, 0)]
        public static void AddTapperTf() {
            AddMainVrcFurryComponents(new[] { TfTimeAnimationPath, TapperTfAnimation });
        }

        [MenuItem("GameObject/Azzmurr/TF Controller/CTF/Knot TF", false, 0)]
        public static void AddKnotTf() {
            AddMainVrcFurryComponents(new[] { TfTimeAnimationPath, KnotTfAnimation });
        }

        [MenuItem("GameObject/Azzmurr/TF Controller/CTF/Physbones Off when seat toggled", false, 0)]
        public static void AddPhysbonesOffOnSeat() {
            AddSeatVrcFurryComponents(new[] { PhysbonesOffAnimation });
        }

        [MenuItem("GameObject/Azzmurr/TF Controller/CTF/Add mat swap on seat toggle on", false, 0)]
        public static void AddMatSwapOnSeat() {
            AddSeatVrcFurryComponents(new AnimationClip[] { }).GetActions();
        }

        [MenuItem("GameObject/Azzmurr/TF Controller/Pool Toy/Body", false, 0)]
        public static void AddBodyPoolToyPreset() {
            AddMainVrcFurryComponents(new[] { TfTimeAnimationPath, MouthOpenAnimation });
            AddMouthDoNotOpenComponents(new[] { MouthDoNotOpenAnimation });
        }

        [MenuItem("GameObject/Azzmurr/TF Controller/Pool Toy/Accessories", false, 0)]
        public static void AddAccessoriesPoolToyPreset() {
            AddMainVrcFurryComponents(new[] { AccessoriesAnimation });
        }

        [MenuItem("GameObject/Azzmurr/TF Controller/Pool Toy/Valve", false, 0)]
        public static void AddValvePoolToyPreset() {
            AddMainVrcFurryComponents(new[] { ValveShowAnimation });
            AddMouthDoNotOpenComponents(new[] { ValveDoNotShowAnimation });
        }

        [MenuItem("GameObject/Azzmurr/TF Controller/Pool Toy/Valve Bulb", false, 0)]
        public static void AddValveBulbPoolToyPreset() {
            AddMainVrcFurryComponents(new[] { ValveLightbulbAnimation });
            AddMouthDoNotOpenComponents(new[] { ValveDoNotLightbulbAnimation });
        }

        private static void AddMouthDoNotOpenComponents(AnimationClip[] clips) {
            AddMainVrcFurryComponent("TF/Disable Mouth Transform", clips);
        }

        private static FuryToggle AddSeatVrcFurryComponents(AnimationClip[] clips) {
            return AddMainVrcFurryComponent("TF/Seat/Toggle", clips);
        }

        private static void AddMainVrcFurryComponents(AnimationClip[] clips) {
            AddMainVrcFurryComponent("TF/Auto", clips);
            AddMainVrcFurryComponent("TF/Manual", clips).SetSlider();
        }

        private static FuryToggle AddMainVrcFurryComponent(string toggleName, AnimationClip[] clips) {
            var toggle = FuryComponents.CreateToggle(Selection.activeGameObject);
            toggle.SetGlobalParameter(toggleName);

            clips.ToList().ForEach(clip => { toggle.GetActions().AddAnimationClip(clip); });

            return toggle;
        }
    }
}
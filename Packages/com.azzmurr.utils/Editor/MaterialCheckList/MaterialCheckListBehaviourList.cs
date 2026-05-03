using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Azzmurr.Utils {
    [CreateAssetMenu(
        fileName = "MaterialCheckListBehaviour",
        menuName = "Azzmurr/MaterialCheckListBehaviour"
    )]
    public class MaterialCheckListBehaviourList : ScriptableObject {
        public MaterialCheckListBehaviour[] behaviours;
    }

    [Serializable]
    public class MaterialCheckListBehaviour {
        public string shaderName;
        public MaterialCheckListItem[] items;
    }

    [Serializable]
    public class MaterialCheckListItem {
        public string propertyName;
        public PropertyCheckType propertyCheckType;
        public UniversalValue desiredValue;

        public string sameAsFieldName;

        public ShowPropertyIf showPropertyIf;
        public string showPropertyIfPropertyName;
        public UniversalValue showPropertyIfPropertyValue;

        public CopyPropertyOnFix copyPropertyOnFix;
        public string copyPropertyValueToPropertyName;
    }

    public enum PropertyCheckType {
        Equals,
        SameAs,
        Exists,
    }

    public enum ShowPropertyIf {
        ShowAlways,
        ShowIfEquals,
        ShowIfNotEquals,
    }

    public enum CopyPropertyOnFix {
        DoNotCopy,
        Copy,
    }

    [CustomEditor(typeof(MaterialCheckListBehaviourList))]
    public class Editor : UnityEditor.Editor {
        private SerializedProperty _behaviours;
        private MaterialCheckListBehaviourList _script;

        private void OnEnable() {
            _script = target as MaterialCheckListBehaviourList;
            if (_script == null) return;

            _behaviours = serializedObject.FindProperty(nameof(MaterialCheckListBehaviourList.behaviours));
        }

        public override VisualElement CreateInspectorGUI() {
            var root = new VisualElement();

            var listView = new ListView {
                focusable = true,
                name = "Behaviours List",
                showAddRemoveFooter = true,
                reorderable = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                showBorder = true,
                headerTitle = "Check Lists",
                showFoldoutHeader = true,
                reorderMode = ListViewReorderMode.Animated,
                showBoundCollectionSize = true,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                style = {
                    marginTop = 8,
                },
            };

            listView.makeItem += () => new MaterialCheckListField();
            listView.bindItem += BindParameterListItem;

            listView.itemsAdded += _ => { serializedObject.Update(); };
            listView.itemsRemoved += _ => { serializedObject.Update(); };

            listView.BindProperty(_behaviours);

            root.Add(listView);

            return root;
        }

        private void BindParameterListItem(VisualElement element, int i) {
            var parameterField = (MaterialCheckListField)element;
            var param = _behaviours.GetArrayElementAtIndex(i);
            parameterField.BindProperty(param);
        }
    }
}
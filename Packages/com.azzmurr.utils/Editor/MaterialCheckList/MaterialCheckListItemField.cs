using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Azzmurr.Utils {
    public class MaterialCheckListItemField : VisualElement {
        private readonly EnumField _copyPropertyOnFixField = new() { style = { width = 100 } };
        private readonly VisualElement _copyPropertySpacer = new() { style = { width = 602 } };

        private readonly TextField _copyPropertyValueToPropertyNameField = new() { label = "Copy to", style = { flexGrow = 1, flexShrink = 0, minWidth = 150 } };
        private readonly EnumField _propertyCheckTypeField = new() { style = { width = 100 } };

        private readonly TextField _propertyNameField = new() { style = { flexGrow = 1, flexShrink = 0, minWidth = 150 } };
        private readonly TextField _sameAsFieldName = new() { style = { width = 400 } };

        private readonly EnumField _showPropertyIfField = new() { style = { width = 100 } };

        private readonly TextField _showPropertyIfPropertyNameField = new() { label = "Show if", style = { flexGrow = 1, flexShrink = 0, minWidth = 150 } };
        private readonly VisualElement _showPropertyIfSpacer = new() { style = { width = 202 } };
        private readonly UniversalValueField _showPropertyIfValueField = new() { style = { width = 400 } };

        private readonly UniversalValueField _valueField = new() { style = { width = 400 } };
        private readonly VisualElement _valueSpacer = new() { style = { width = 400 } };

        private Dictionary<VisualElement, string> _bindings;
        private SerializedProperty _checklistItem; // MaterialCheckListItem

        public MaterialCheckListItemField() {
            CreateDictionaries();
            CreateGUI();
        }

        public void BindProperty(SerializedProperty prop) {
            _checklistItem = prop;

            foreach (var mapping in _bindings) {
                var property = _checklistItem.FindPropertyRelative(mapping.Value);
                switch (mapping.Key) {
                    case UniversalValueField universalValueField:
                        universalValueField.BindProperty(property);
                        break;

                    case IBindable bindable:
                        bindable.BindProperty(property);
                        break;
                }
            }

            ChangeVisibility();
        }

        private void CreateGUI() {
            style.paddingTop = 8;
            style.paddingBottom = 8;

            var main = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            main.Add(_propertyNameField);
            main.Add(_propertyCheckTypeField);
            main.Add(_valueField);
            main.Add(_sameAsFieldName);
            // main.Add(_valueSpacer);
            main.Add(_showPropertyIfField);
            main.Add(_copyPropertyOnFixField);

            Add(main);

            var showIf = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            showIf.Add(_showPropertyIfPropertyNameField);
            showIf.Add(_showPropertyIfValueField);
            showIf.Add(_showPropertyIfSpacer);

            Add(showIf);

            var copyTo = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            copyTo.Add(_copyPropertyValueToPropertyNameField);
            copyTo.Add(_copyPropertySpacer);

            Add(copyTo);

            _propertyCheckTypeField.RegisterValueChangedCallback(e => {
                if (e.newValue == null) return;
                ChangeVisibility();
            });

            _showPropertyIfField.RegisterValueChangedCallback(e => {
                if (e.newValue == null) return;
                ChangeVisibility();
            });

            _copyPropertyOnFixField.RegisterValueChangedCallback(e => {
                if (e.newValue == null) return;
                ChangeVisibility();
            });
        }

        private void CreateDictionaries() {
            _bindings = new Dictionary<VisualElement, string> {
                // Basic properties
                { _propertyNameField, nameof(MaterialCheckListItem.propertyName) },
                { _propertyCheckTypeField, nameof(MaterialCheckListItem.propertyCheckType) },

                { _valueField, nameof(MaterialCheckListItem.desiredValue) },

                { _sameAsFieldName, nameof(MaterialCheckListItem.sameAsFieldName) },

                // Show property conditions
                { _showPropertyIfField, nameof(MaterialCheckListItem.showPropertyIf) },
                { _showPropertyIfPropertyNameField, nameof(MaterialCheckListItem.showPropertyIfPropertyName) },
                { _showPropertyIfValueField, nameof(MaterialCheckListItem.showPropertyIfPropertyValue) },

                // Copy properties
                { _copyPropertyOnFixField, nameof(MaterialCheckListItem.copyPropertyOnFix) },
                { _copyPropertyValueToPropertyNameField, nameof(MaterialCheckListItem.copyPropertyValueToPropertyName) },
            };
        }


        private void ChangeVisibility() {
            var setMode = (PropertyCheckType)_propertyCheckTypeField.value;
            var showMode = (ShowPropertyIf)_showPropertyIfField.value;
            var copyMode = (CopyPropertyOnFix)_copyPropertyOnFixField.value;

            _valueField.style.display = setMode != PropertyCheckType.SameAs
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            _sameAsFieldName.style.display = setMode == PropertyCheckType.SameAs
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            // _valueSpacer.style.display = setMode == PropertyCheckType.Exists
            //     ? DisplayStyle.Flex
            //     : DisplayStyle.None;

            _showPropertyIfPropertyNameField.style.display = showMode == ShowPropertyIf.ShowAlways
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            _showPropertyIfValueField.style.display = showMode == ShowPropertyIf.ShowAlways
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            _showPropertyIfSpacer.style.display = showMode == ShowPropertyIf.ShowAlways
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            _copyPropertySpacer.style.display = copyMode == CopyPropertyOnFix.Copy
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            _copyPropertyValueToPropertyNameField.style.display = copyMode == CopyPropertyOnFix.Copy
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }
}
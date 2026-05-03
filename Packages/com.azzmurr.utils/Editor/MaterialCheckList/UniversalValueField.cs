using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Azzmurr.Utils {
    public class UniversalValueField : BindableElement, INotifyValueChanged<UniversalValue> {
        private readonly EnumField _typeField;
        private readonly VisualElement _valueContainer;
        private UniversalValue _localValue;
        private SerializedProperty _valueProp;

        public UniversalValueField() {
            style.flexDirection = FlexDirection.Row;

            _typeField = new EnumField(UniversalValue.ValueType.Bool) { style = { width = 100 } };
            _typeField.RegisterValueChangedCallback(evt => {
                if (_valueProp != null) {
                    _valueProp.serializedObject.Update();
                    _valueProp.FindPropertyRelative(nameof(UniversalValue.type)).enumValueIndex = (int)(UniversalValue.ValueType)evt.newValue;
                    _valueProp.serializedObject.ApplyModifiedProperties();
                }
                else {
                    _localValue ??= new UniversalValue();
                    _localValue.type = (UniversalValue.ValueType)evt.newValue;
                }

                UpdateField();
            });

            _valueContainer = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Row } };

            Add(_typeField);
            Add(_valueContainer);

            _localValue = new UniversalValue();
            UpdateField();
        }

        public UniversalValue value {
            get => _valueProp != null ? GetValueFromProperty() : _localValue;
            set {
                if (value == null) return;
                SetValueWithoutNotify(value);
                SendValueChangedEvent();
            }
        }

        public void SetValueWithoutNotify(UniversalValue newValue) {
            if (newValue == null) return;

            if (_valueProp != null) {
                _valueProp.serializedObject.Update();
                _valueProp.FindPropertyRelative(nameof(UniversalValue.type)).enumValueIndex = (int)newValue.type;

                switch (newValue.type) {
                    case UniversalValue.ValueType.Bool:
                        _valueProp.FindPropertyRelative(nameof(UniversalValue.boolValue)).boolValue = newValue.boolValue;
                        break;
                    case UniversalValue.ValueType.Int:
                        _valueProp.FindPropertyRelative(nameof(UniversalValue.intValue)).intValue = newValue.intValue;
                        break;
                    case UniversalValue.ValueType.Float:
                        _valueProp.FindPropertyRelative(nameof(UniversalValue.floatValue)).floatValue = newValue.floatValue;
                        break;
                    case UniversalValue.ValueType.Color:
                        _valueProp.FindPropertyRelative(nameof(UniversalValue.colorValue)).colorValue = newValue.colorValue;
                        break;
                    case UniversalValue.ValueType.Vector4:
                        _valueProp.FindPropertyRelative(nameof(UniversalValue.vector4Value)).vector4Value = newValue.vector4Value;
                        break;
                    case UniversalValue.ValueType.Texture:
                        _valueProp.FindPropertyRelative(nameof(UniversalValue.textureValue)).objectReferenceValue = newValue.textureValue;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                _valueProp.serializedObject.ApplyModifiedProperties();
            }
            else {
                _localValue = newValue;
            }

            _typeField.SetValueWithoutNotify(newValue.type);
            UpdateField();
        }

        public void SetTypeSelectorHidden(bool hide = true) {
            _typeField.style.display = hide ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void BindProperty(SerializedProperty property) {
            _valueProp = property;
            _localValue = null;

            var typeRel = property.FindPropertyRelative(nameof(UniversalValue.type));
            _typeField.SetValueWithoutNotify((UniversalValue.ValueType)typeRel.enumValueIndex);

            UpdateField();
        }

        private void UpdateField() {
            _valueContainer.Clear();

            if (_valueProp == null && _localValue == null) return;

            var type = _valueProp != null
                ? (UniversalValue.ValueType)_valueProp.FindPropertyRelative(nameof(UniversalValue.type)).enumValueIndex
                : _localValue.type;

            switch (type) {
                case UniversalValue.ValueType.Bool:
                    var toggle = new Toggle { style = { flexGrow = 1 } };
                    if (_valueProp != null) {
                        toggle.BindProperty(_valueProp.FindPropertyRelative(nameof(UniversalValue.boolValue)));
                    }
                    else {
                        toggle.value = _localValue.boolValue;
                        toggle.RegisterValueChangedCallback(evt => {
                            _localValue.boolValue = evt.newValue;
                            SendValueChangedEvent();
                        });
                    }

                    _valueContainer.Add(toggle);
                    break;

                case UniversalValue.ValueType.Int:
                    var intField = new IntegerField { style = { flexGrow = 1 } };
                    if (_valueProp != null) {
                        intField.BindProperty(_valueProp.FindPropertyRelative(nameof(UniversalValue.intValue)));
                    }
                    else {
                        intField.value = _localValue.intValue;
                        intField.RegisterValueChangedCallback(evt => {
                            _localValue.intValue = evt.newValue;
                            SendValueChangedEvent();
                        });
                    }

                    _valueContainer.Add(intField);
                    break;

                case UniversalValue.ValueType.Float:
                    var floatField = new FloatField { style = { flexGrow = 1 } };
                    if (_valueProp != null) {
                        floatField.BindProperty(_valueProp.FindPropertyRelative(nameof(UniversalValue.floatValue)));
                    }
                    else {
                        floatField.value = _localValue.floatValue;
                        floatField.RegisterValueChangedCallback(evt => {
                            _localValue.floatValue = evt.newValue;
                            SendValueChangedEvent();
                        });
                    }

                    _valueContainer.Add(floatField);
                    break;

                case UniversalValue.ValueType.Color:
                    var colorField = new ColorField { style = { flexGrow = 1 } };
                    var hdrToggle = new Toggle();

                    if (_valueProp != null) {
                        colorField.BindProperty(_valueProp.FindPropertyRelative(nameof(UniversalValue.colorValue)));
                        hdrToggle.BindProperty(_valueProp.FindPropertyRelative(nameof(UniversalValue.isHDR)));
                        hdrToggle.RegisterValueChangedCallback(evt => { colorField.hdr = evt.newValue; });
                    }
                    else {
                        colorField.value = _localValue.colorValue;
                        colorField.hdr = _localValue.isHDR;
                        hdrToggle.value = _localValue.isHDR;
                        colorField.RegisterValueChangedCallback(evt => {
                            _localValue.colorValue = evt.newValue;
                            SendValueChangedEvent();
                        });
                        hdrToggle.RegisterValueChangedCallback(evt => {
                            _localValue.isHDR = evt.newValue;
                            colorField.hdr = evt.newValue;
                            SendValueChangedEvent();
                        });
                    }

                    _valueContainer.Add(new Label("HDR: ") { style = { unityTextAlign = TextAnchor.MiddleLeft } });
                    _valueContainer.Add(hdrToggle);
                    _valueContainer.Add(colorField);
                    break;

                case UniversalValue.ValueType.Vector4:
                    var vector4Field = new Vector4Field { style = { flexGrow = 1 } };
                    if (_valueProp != null) {
                        vector4Field.BindProperty(_valueProp.FindPropertyRelative(nameof(UniversalValue.vector4Value)));
                    }
                    else {
                        vector4Field.value = _localValue.vector4Value;
                        vector4Field.RegisterValueChangedCallback(evt => {
                            _localValue.vector4Value = evt.newValue;
                            SendValueChangedEvent();
                        });
                    }

                    _valueContainer.Add(vector4Field);
                    break;

                case UniversalValue.ValueType.Texture:
                    var textureField = new ObjectField { objectType = typeof(Texture), style = { flexGrow = 1 } };
                    if (_valueProp != null) {
                        textureField.BindProperty(_valueProp.FindPropertyRelative(nameof(UniversalValue.textureValue)));
                    }
                    else {
                        textureField.value = _localValue.textureValue;
                        textureField.RegisterValueChangedCallback(evt => {
                            _localValue.textureValue = (Texture)evt.newValue;
                            SendValueChangedEvent();
                        });
                    }

                    _valueContainer.Add(textureField);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private UniversalValue GetValueFromProperty() {
            if (_valueProp == null) return null;

            var result = new UniversalValue {
                type = (UniversalValue.ValueType)_valueProp.FindPropertyRelative(nameof(UniversalValue.type)).enumValueIndex
            };

            switch (result.type) {
                case UniversalValue.ValueType.Bool:
                    result.boolValue = _valueProp.FindPropertyRelative(nameof(UniversalValue.boolValue)).boolValue;
                    break;
                case UniversalValue.ValueType.Int:
                    result.intValue = _valueProp.FindPropertyRelative(nameof(UniversalValue.intValue)).intValue;
                    break;
                case UniversalValue.ValueType.Float:
                    result.floatValue = _valueProp.FindPropertyRelative(nameof(UniversalValue.floatValue)).floatValue;
                    break;
                case UniversalValue.ValueType.Color:
                    result.colorValue = _valueProp.FindPropertyRelative(nameof(UniversalValue.colorValue)).colorValue;
                    break;
                case UniversalValue.ValueType.Vector4:
                    result.vector4Value = _valueProp.FindPropertyRelative(nameof(UniversalValue.vector4Value)).vector4Value;
                    break;
                case UniversalValue.ValueType.Texture:
                    result.textureValue = _valueProp.FindPropertyRelative(nameof(UniversalValue.textureValue)).objectReferenceValue as Texture;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result;
        }

        private void SendValueChangedEvent() {
            if (_valueProp != null) {
                var newValue = GetValueFromProperty();
                var evt = ChangeEvent<UniversalValue>.GetPooled(value, newValue);
                evt.target = this;
                SendEvent(evt);
            }
            else {
                var oldValue = value;
                var evt = ChangeEvent<UniversalValue>.GetPooled(oldValue, _localValue);
                evt.target = this;
                SendEvent(evt);
            }
        }
    }
}
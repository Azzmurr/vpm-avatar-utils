using System;
using UnityEngine;

namespace Azzmurr.Utils {
    [Serializable]
    public class UniversalValue {
        public enum ValueType {
            Bool,
            Int,
            Float,
            Color,
            Vector4,
            Texture,
        }

        public ValueType type;
        public bool isHDR;

        public bool boolValue;
        public int intValue;
        public float floatValue;
        public Color colorValue;
        public Vector4 vector4Value;
        public Texture textureValue;

        public object GetValue() {
            return type switch {
                ValueType.Bool => boolValue,
                ValueType.Int => intValue,
                ValueType.Float => floatValue,
                ValueType.Color => colorValue,
                ValueType.Vector4 => vector4Value,
                ValueType.Texture => textureValue,
                _ => null,
            };
        }

        public void SetValue(object val) {
            switch (val) {
                case bool b:
                    type = ValueType.Bool;
                    boolValue = b;
                    break;
                case int i:
                    type = ValueType.Int;
                    intValue = i;
                    break;
                case float f:
                    type = ValueType.Float;
                    floatValue = f;
                    break;
                case Color c:
                    type = ValueType.Color;
                    colorValue = c;
                    break;
                case Vector4 v:
                    type = ValueType.Vector4;
                    vector4Value = v;
                    break;
                case Texture t:
                    type = ValueType.Texture;
                    textureValue = t;
                    break;
            }
        }
    }
}
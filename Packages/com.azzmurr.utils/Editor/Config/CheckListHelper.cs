using System;
using System.Collections.Generic;
using UnityEngine;

namespace Azzmurr.Utils {
    internal class CheckListHelper {
        private static readonly Dictionary<UniversalValue.ValueType, Func<MaterialMeta, MaterialCheckListItem, (bool needsUpdate, object actualValue, object desiredValue)>> PropertyCheckers = CreatePropertyCheckers();

        public static (bool needsUpdate, object actualValue, object desiredValue) CheckProperty(MaterialMeta material, MaterialCheckListItem item) {
            if (item == null) throw new ArgumentNullException(nameof(item));

            return !PropertyCheckers.TryGetValue(item.desiredValue.type, out var checker) ? throw new ArgumentOutOfRangeException(nameof(item.desiredValue.type)) : checker(material, item);
        }

        private static Dictionary<UniversalValue.ValueType, Func<MaterialMeta, MaterialCheckListItem, (bool, object, object)>>
            CreatePropertyCheckers() {
            return new Dictionary<UniversalValue.ValueType, Func<MaterialMeta, MaterialCheckListItem, (bool, object, object)>> {
                { UniversalValue.ValueType.Bool, CheckBoolProperty },
                { UniversalValue.ValueType.Int, CheckIntProperty },
                { UniversalValue.ValueType.Float, CheckFloatProperty },
                { UniversalValue.ValueType.Color, CheckColorProperty },
                { UniversalValue.ValueType.Vector4, CheckVector4Property },
                { UniversalValue.ValueType.Texture, CheckTextureProperty }
            };
        }

        private static (bool, object, object) CheckBoolProperty(MaterialMeta material, MaterialCheckListItem item) {
            var actual = material.GetPropertyValue<float>(item.propertyName);
            var desired = GetDesiredValue(item,
                () => (bool)item.desiredValue.GetValue() ? 1f : 0f,
                () => material.GetPropertyValue<float>(item.sameAsFieldName));

            var needUpdate = item.propertyCheckType != PropertyCheckType.Exists && !Mathf.Approximately(actual, desired);

            return (needUpdate, actual, desired);
        }

        private static (bool, object, object) CheckIntProperty(MaterialMeta material, MaterialCheckListItem item) {
            var actual = material.GetPropertyValue<int>(item.propertyName);
            var desired = GetDesiredValue(item,
                () => (int)item.desiredValue.GetValue(),
                () => material.GetPropertyValue<int>(item.sameAsFieldName));

            var needUpdate = item.propertyCheckType != PropertyCheckType.Exists && actual != desired;

            return (needUpdate, actual, desired);
        }

        private static (bool, object, object) CheckFloatProperty(MaterialMeta material, MaterialCheckListItem item) {
            var actual = material.GetPropertyValue<float>(item.propertyName);
            var desired = GetDesiredValue(item,
                () => (float)item.desiredValue.GetValue(),
                () => material.GetPropertyValue<float>(item.sameAsFieldName));

            var needUpdate = item.propertyCheckType != PropertyCheckType.Exists && !Mathf.Approximately(actual, desired);

            return (needUpdate, actual, desired);
        }

        private static (bool, object, object) CheckColorProperty(MaterialMeta material, MaterialCheckListItem item) {
            var actual = material.GetPropertyValue<Color>(item.propertyName);
            var desired = GetDesiredValue(item,
                () => (Color)item.desiredValue.GetValue(),
                () => material.GetPropertyValue<Color>(item.sameAsFieldName));

            var needUpdate = item.propertyCheckType != PropertyCheckType.Exists && actual != desired;

            return (needUpdate, actual, desired);
        }

        private static (bool, object, object) CheckVector4Property(MaterialMeta material, MaterialCheckListItem item) {
            var actual = material.GetPropertyValue<Vector4>(item.propertyName);
            var desired = GetDesiredValue(item,
                () => (Vector4)item.desiredValue.GetValue(),
                () => material.GetPropertyValue<Vector4>(item.sameAsFieldName));
            var needUpdate = item.propertyCheckType != PropertyCheckType.Exists && actual != desired;

            return (needUpdate, actual, desired);
        }

        private static (bool, object, object) CheckTextureProperty(MaterialMeta material, MaterialCheckListItem item) {
            var actual = material.GetPropertyValue<Texture>(item.propertyName);
            var desired = GetDesiredValue(item,
                () => (Texture)item.desiredValue.GetValue(),
                () => material.GetPropertyValue<Texture>(item.sameAsFieldName));

            var needUpdate = item.propertyCheckType == PropertyCheckType.Exists
                ? actual == null
                : actual != desired;

            return (needUpdate, actual, desired);
        }

        private static T GetDesiredValue<T>(MaterialCheckListItem item, Func<T> equalsValue, Func<T> sameAsValue) {
            return item.propertyCheckType switch {
                PropertyCheckType.Equals => equalsValue(),
                PropertyCheckType.SameAs => sameAsValue(),
                PropertyCheckType.Exists => equalsValue(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Azzmurr.Utils {
    public class ObjectMeta {
        public readonly object Object;
        public Type Type => Object?.GetType();
        public string TypeFullName => Type?.FullName;

        public bool IsVrcFury => TypeFullName?.StartsWith("VF") == true;
        public bool IsVrcFuryToggle => IsVrcFury && Get("content").TypeFullName == "VF.Model.Feature.Toggle";
        public bool IsMaterialAction => TypeFullName == "VF.Model.StateAction.MaterialAction";
        public bool IsFlipBookBuilderAction => TypeFullName == "VF.Model.StateAction.FlipBookBuilderAction";
        public bool IsSlider => IsVrcFuryToggle && Get("content").Get("slider").Object is true;
        public bool IsSliderPassthrough => IsSlider && Get("content").Get("sliderInactiveAtZero").Object is true;

        public string VrcFuryGlobalParameter => Get("content").Get("globalParam").Object?.ToString();
        public string VrcFuryExclusiveTag => Get("content").Get("exclusiveTag").Object?.ToString();
        public Component Component => Object as Component;

        public ObjectMeta(object component) {
            Object = component;
        }

        public ObjectMeta Get(string fieldName) {
            var field = Object?.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            return new ObjectMeta(field?.GetValue(Object));
        }

        public ObjectMeta Set<T>(string fieldName, T newValue) {
            var field = Object?.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            field?.SetValue(Object, newValue);

            return new ObjectMeta(field?.GetValue(Object));
        }

        public void ForEach(Action<ObjectMeta> action) {
            if (Object is not IEnumerable list) return;

            foreach (var listObject in list) {
                action(new ObjectMeta(listObject));
            }
        }
    }
}

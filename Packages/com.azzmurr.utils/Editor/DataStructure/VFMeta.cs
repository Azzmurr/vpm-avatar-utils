using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Azzmurr.Utils {
    public class ObjectMeta {
        public readonly object Object;
        public Type Type => Object?.GetType();
        public string TypeFullName => Type?.FullName;

        public bool IsVrcFury => TypeFullName.StartsWith("VF");
        public bool IsVrcFuryToggle => IsVrcFury && Get("content").TypeFullName == "VF.Model.Feature.Toggle";
        public bool IsMaterialAction => TypeFullName == "VF.Model.StateAction.MaterialAction";
        public bool IsFlipBookBuilderAction => TypeFullName == "VF.Model.StateAction.FlipBookBuilderAction";

        public string VrcFuryGlobalParameter => Get("content").Get("globalParam").Object.ToString();
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

        public void ForEach(Action<ObjectMeta> action) {
            if (Object is not IEnumerable list) return;

            foreach (var listObject in list) {
                action(new ObjectMeta(listObject));
            }
        }
    }
}

using System.Reflection;
using UnityEngine;

namespace Azzmurr.Utils {
    public abstract class VFMetaCore {
        protected readonly MonoBehaviour Component;

        protected VFMetaCore(MonoBehaviour behaviour) {
            Component = behaviour;
        }

        public GameObject GameObject => Component.gameObject;

        public abstract FieldInfo[] GetFields();
    }

    public class VFMeta : VFMetaCore {
        public VFMeta(MonoBehaviour behaviour) : base(behaviour) {
        }

        public bool IsToggle => VFToggleMeta.IsToggle(Component);
        public VFToggleMeta Toggle => new(Component);

        public override FieldInfo[] GetFields() {
            return Component.GetType().GetFields();
        }
    }

    public class VFToggleMeta : VFMetaCore {
        private const string Type = "VF.Model.Feature.Toggle";

        public VFToggleMeta(MonoBehaviour behaviour) : base(behaviour) {
        }

        private object Content => Component.GetType().GetField("content").GetValue(Component);
        public string GlobalParameter => Content.GetType().GetField("globalParam").GetValue(Content).ToString();

        public override FieldInfo[] GetFields() {
            return Content.GetType().GetFields();
        }

        public static bool IsToggle(MonoBehaviour component) {
            var content = component.GetType().GetField("content");
            if (content == null) return false;

            var value = content.GetValue(component);
            if (value == null) return false;

            return value.GetType().ToString() == Type;
        }
    }
}
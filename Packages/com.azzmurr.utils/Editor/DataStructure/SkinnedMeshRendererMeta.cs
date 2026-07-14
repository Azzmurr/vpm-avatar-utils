using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.Dynamics;

namespace Azzmurr.Utils {
    public class SkinnedMeshRendererMeta {
        public SkinnedMeshRenderer SkinnedMeshRenderer;
        public Transform RootTransform;
        public GameObject GameObject => SkinnedMeshRenderer.gameObject;
        public Transform RootBone => SkinnedMeshRenderer.rootBone;
        public List<GameObject> PhysBones => GetPhysBonesAffectingMesh(SkinnedMeshRenderer, RootTransform);
        public List<ObjectMeta> VrcFuryComponents => GetVrcFuryComponents(GameObject);
        public bool HasSlidersNotPassthrough => VrcFuryComponents.Any(component => component.IsSlider && !component.IsSliderPassthrough);
        public bool Expanded = false;

        public SkinnedMeshRendererMeta(SkinnedMeshRenderer skinnedMeshRenderer, Transform rootTransform) {
            SkinnedMeshRenderer = skinnedMeshRenderer;
            RootTransform = rootTransform;
        }

        private List<GameObject> GetPhysBonesAffectingMesh(SkinnedMeshRenderer smr, Transform searchRoot)
        {
            var meshBones = new HashSet<Transform>(smr.bones.Where(b => b != null));
            if (RootBone != null) meshBones.Add(RootBone);

            var fullBoneSet = new HashSet<Transform>();
            foreach (var b in meshBones)
                CollectSelfAndChildren(b, fullBoneSet);

            var candidatePhysBones = searchRoot.GetComponentsInChildren<VRCPhysBoneBase>(true);
            var result = new List<VRCPhysBoneBase>();
            foreach (var pb in candidatePhysBones)
            {
                var pbRoot = pb.GetRootTransform();
                if (pbRoot == null) continue;

                var affected = new HashSet<Transform>();
                CollectSelfAndChildren(pbRoot, affected);

                foreach (var ignored in pb.ignoreTransforms)
                {
                    if (ignored == null) continue;
                    var ignoredSet = new HashSet<Transform>();
                    CollectSelfAndChildren(ignored, ignoredSet);
                    affected.ExceptWith(ignoredSet);
                }

                if (affected.Overlaps(fullBoneSet))
                    result.Add(pb);
            }

            return result.ConvertAll((physBone) => physBone.gameObject);
        }

        private static void CollectSelfAndChildren(Transform t, HashSet<Transform> set)
        {
            if (t == null || !set.Add(t)) return;
            for (var i = 0; i < t.childCount; i++)
                CollectSelfAndChildren(t.GetChild(i), set);
        }

        public List<ObjectMeta> GetVrcFuryComponents(GameObject gameObject) {
            var components = gameObject.GetComponents<Component>()
                .Where(component => component != null)
                .Where(component => component
                    .gameObject
                    .GetComponentsInParent<Transform>(true)
                    .All(transform => !transform.CompareTag("EditorOnly"))
                )
                .Where(component => component.GetType().FullName == "VF.Model.VRCFury")
                .ToList()
                .ConvertAll(component => new ObjectMeta(component));

            return components;
        }
    }
}

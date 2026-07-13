using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Azzmurr.Utils {
    public class SkinnedMeshRendererMeta {
        public SkinnedMeshRenderer MeshRenderer;
        public GameObject GameObject => MeshRenderer.gameObject;
        public Transform RootBone => MeshRenderer.rootBone;
        public List<GameObject> PhysBones => GetPhysBones(RootBone.gameObject);
        public List<ObjectMeta> VrcFuryComponents => GetVrcFuryComponents(GameObject);
        public bool HasSlidersNotPassthrough => VrcFuryComponents.Any(component => component.IsSlider && !component.IsSliderPassthrough);
        public bool Expanded = false;

        public SkinnedMeshRendererMeta(SkinnedMeshRenderer meshRenderer) {
            MeshRenderer = meshRenderer;
        }

        private List<GameObject> GetPhysBones(GameObject gameObject) {
            return gameObject.GetComponentsInChildren<VRCPhysBone>(true)
                .Where(vrcPhysBone => vrcPhysBone
                    .gameObject
                    .GetComponentsInParent<Transform>(true)
                    .All(transform => !transform.CompareTag("EditorOnly")))
                .ToHashSet()
                .ToList()
                .ConvertAll((vrcPhysBone) => vrcPhysBone.gameObject);
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

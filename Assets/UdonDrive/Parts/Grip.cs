
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Grip : UdonSharpBehaviour {
        [SerializeField] HandType handType;
        [SerializeField] UpdateCore updateCore;
        [SerializeField] MeshRenderer meshRenderer;
        public override void OnPickup() {
            meshRenderer.enabled = false;
            updateCore.setHold(handType, true);
        }
        public override void OnDrop() {
            meshRenderer.enabled = true;
            updateCore.setHold(handType, false);
        }
    }
}
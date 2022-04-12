
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
        public override void OnPickup() {
            updateCore.setHold(handType, true);
        }
        public override void OnDrop() {
            updateCore.setHold(handType, false);
        }
    }
}
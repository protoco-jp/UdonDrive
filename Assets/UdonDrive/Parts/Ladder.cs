
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Ladder : UdonSharpBehaviour {
        [SerializeField] Transform target;
        public override void Interact() {
            Networking.LocalPlayer.TeleportTo(target.position, target.rotation);
        }
    }
}
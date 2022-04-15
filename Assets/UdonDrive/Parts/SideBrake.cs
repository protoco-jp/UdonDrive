
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SideBrake : UdonSharpBehaviour {
        [SerializeField] UpdateCore updateCore;
        public override void Interact() {
            if (!updateCore.getSideBrake()) {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "sidebrake");
            } else {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "nosidebrake");
            }
        }

        public void sidebrake() {
            updateCore.setSideBrake(true);
        }

        public void nosidebrake() {
            updateCore.setSideBrake(false);
        }
    }
}

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Reverse : UdonSharpBehaviour {
        [SerializeField] UpdateCore updateCore;
        public override void Interact() {
            if (!updateCore.getReverse()) {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "reverse");
            } else {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "unreverse");
            }
        }

        public void reverse() {
            updateCore.setReverse(true);
        }

        public void unreverse() {
            updateCore.setReverse(false);
        }
    }
}
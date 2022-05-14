
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayAnimator : UdonSharpBehaviour {
        [SerializeField] Animator animator;
        private bool onlyOnce = false;
        void Update() {
            if (onlyOnce) { return; }
            float distance = Vector3.Distance(this.transform.position, Networking.LocalPlayer.GetPosition());
            if (distance < 20f) {
                onlyOnce = true;
                activateSandWarm();
            }
        }
        private void activateSandWarm() {
            if (!Networking.LocalPlayer.isMaster) { return; }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "playSand");
        }
        public void playSand() {
            onlyOnce = true;
            animator.Play("Action", 0, 0f);
        }
    }
}
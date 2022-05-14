
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Reverse : UdonSharpBehaviour {
        [SerializeField] UpdateCore updateCore;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip clip;
        [SerializeField] Transform bar; 
        [HideInInspector] public bool lockFlg = false;
        public override void Interact() {
            if (lockFlg) { return; }
            if (!updateCore.getReverse()) {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "reverse");
            } else {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "unreverse");
            }
        }

        public void reverse() {
            bar.localRotation = Quaternion.Euler(-30f, 0, 0);
            audioSource.PlayOneShot(clip);
            updateCore.setReverse(true);
        }

        public void unreverse() {
            bar.localRotation = Quaternion.Euler(0, 0, 0);
            audioSource.PlayOneShot(clip);
            updateCore.setReverse(false);
        }
    }
}

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SideBrake : UdonSharpBehaviour {
        [SerializeField] UpdateCore updateCore;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip clip;
        [SerializeField] Transform bar; 
        public override void Interact() {
            if (!updateCore.getSideBrake()) {
                sidebrake();
            } else {
                nosidebrake();
            }
        }

        public void sidebrake() {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "n_sidebrake");
        }
        public void n_sidebrake() {
            bar.localRotation = Quaternion.Euler(-45,0,0);
            audioSource.PlayOneShot(clip);
            updateCore.setSideBrake(true);
        }
        public void nosidebrake() {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "n_nosidebrake");
        }
        public void n_nosidebrake() {
            bar.localRotation = Quaternion.Euler(0, 0, 0);
            audioSource.PlayOneShot(clip);
            updateCore.setSideBrake(false);
        }
    }
}
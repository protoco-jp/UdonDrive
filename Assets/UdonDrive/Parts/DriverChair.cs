
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DriverChair : UdonSharpBehaviour {
        [SerializeField] UpdateCore updateCore;
        [SerializeField] GameObject physicalBody;
        [SerializeField] GameObject networkEngine;
        [SerializeField] GameObject networkWheelL;
        [SerializeField] GameObject networkWheelR;
        [SerializeField] Animator audioAnim;

        private bool someoneDriving = false;

        private bool ownable = false;

        public override void OnPlayerJoined(VRCPlayerApi player) {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "askOwner");
            if (Networking.LocalPlayer.playerId != player.playerId) { return; }
            if (!Networking.IsMaster) { return; }
            updateCore.setDriver(true);
        }
        public void askOwner() {
           if(someoneDriving == true){
               SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "hasDriver");
           }
        }
        public void hasDriver (){
            audioAnim.SetBool("HasDriver", true);
        }

        public override void Interact() {
            Networking.LocalPlayer.UseAttachedStation();
        }
        public override void OnStationEntered(VRCPlayerApi player) {
            if (Networking.LocalPlayer.playerId != player.playerId) { return; }
            Networking.SetOwner(Networking.LocalPlayer, physicalBody);
            Networking.SetOwner(Networking.LocalPlayer, networkEngine);
            Networking.SetOwner(Networking.LocalPlayer, networkWheelL);
            Networking.SetOwner(Networking.LocalPlayer, networkWheelR);

            ownable = true;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "changeDriver");
        }
        public void changeDriver() {
            audioAnim.Play("StartUp.StartUp", 1, 0f);
            audioAnim.SetBool("HasDriver", true);
            someoneDriving = true;
            updateCore.setSideBrake(false);
            if (ownable) {
                updateCore.setDriver(true);
                ownable = false;
            } else {
                updateCore.setDriver(false);
            }
        }
        public override void OnStationExited(VRCPlayerApi player) {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "quitDriver");
        }
        public void quitDriver() {
            audioAnim.SetBool("HasDriver", false);
            someoneDriving = false;
            updateCore.setSideBrake(true);
        }
    }
}
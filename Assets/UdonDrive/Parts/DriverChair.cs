
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

        public override void Interact(){
            Networking.LocalPlayer.UseAttachedStation();
        }
        public override void OnStationEntered(VRCPlayerApi player) {
            if (Networking.LocalPlayer.playerId != player.playerId) {
                return;
            }
            Networking.SetOwner(Networking.LocalPlayer, physicalBody);
            Networking.SetOwner(Networking.LocalPlayer, networkEngine);
            Networking.SetOwner(Networking.LocalPlayer, networkWheelL);
            Networking.SetOwner(Networking.LocalPlayer, networkWheelR);
            updateCore.setDriver(true);
        }

        public override void OnStationExited(VRCPlayerApi player) {
            if (Networking.LocalPlayer.playerId != player.playerId) {
                return;
            }
            updateCore.setDriver(false);
        }
    }
}

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class PlayerStation : UdonSharpBehaviour {
        private VRCStation vrcStation;
        private VRCPlayerApi[] vrcPlayers = new VRCPlayerApi[16];
        void Start() {
            vrcStation = (VRCStation)GetComponent(typeof(VRCStation));
            SendCustomEventDelayedSeconds("deb", 3f);
        }

        void Update(){
            if(!Networking.IsOwner(this.gameObject)){return;}
            this.transform.position = Networking.LocalPlayer.GetPosition();
            this.transform.rotation = Networking.LocalPlayer.GetRotation();
        }

        public void deb() {
            VRCPlayerApi.GetPlayers(vrcPlayers);
            Debug.Log("!!!");

            SendCustomEventDelayedSeconds("deb", 3f);
            foreach (VRCPlayerApi player in vrcPlayers) {
                if (player == null) { return; }
                if (!player.IsValid() || (player.playerId < 0)) { return; }
                Debug.Log(player.GetPosition());
            }
        }

        public override void Interact() {
            vrcStation.PlayerMobility = VRCStation.Mobility.Mobile;
            Networking.LocalPlayer.UseAttachedStation();
        }

        public override void OnStationEntered(VRCPlayerApi player) {
            if (player.isLocal) {
                Networking.SetOwner(player, this.gameObject);
                return;
            }
            vrcStation.PlayerMobility = VRCStation.Mobility.ImmobilizeForVehicle;
        }
        public override void OnStationExited(VRCPlayerApi player) {
            Debug.Log("Exit");
            vrcStation.PlayerMobility = VRCStation.Mobility.ImmobilizeForVehicle;
        }
    }
}


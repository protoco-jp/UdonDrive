
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Chair : UdonSharpBehaviour {
        [SerializeField] Transform entrypoint;

        [UdonSynced, FieldChangeCallback(nameof(height))]
        private float _height;
        public float height {
            set {
                _height = value;
                entrypoint.localPosition = new Vector3(0, value, 0);
            }
            get => _height;
        }

        private VRCStation vrcStation;

        void Start() {
            vrcStation = (VRCStation)GetComponent(typeof(VRCStation));
        }
        public override void Interact() {
            Networking.LocalPlayer.UseAttachedStation();
        }
        public override void OnStationEntered(VRCPlayerApi player) {
            if (!player.isLocal) { return; }
            SendCustomEventDelayedSeconds("enter", 2f);
        }
        public override void InputJump(bool value, UdonInputEventArgs args) {
            vrcStation.ExitStation(Networking.LocalPlayer);
        }
        public override void OnStationExited(VRCPlayerApi player) {
            if (!player.isLocal) { return; }
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            height = 0f;
            RequestSerialization();
        }
        public void enter() {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            height = this.transform.position.y + 0.05f //5cm浮かせる
                - Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftUpperLeg).y;
            RequestSerialization();
        }
    }
}
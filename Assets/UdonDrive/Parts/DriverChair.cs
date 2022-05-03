
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DriverChair : UdonSharpBehaviour {
        [UdonSynced, FieldChangeCallback(nameof(height))]
        private float _height;
        public float height {
            set {
                _height = value;
                entrypoint.localPosition = new Vector3(0, value, 0);
            }
            get => _height;
        }

        [SerializeField] UpdateCore updateCore;
        [SerializeField] GameObject physicalBody;
        [SerializeField] GameObject networkEngine;
        [SerializeField] GameObject networkWheelL;
        [SerializeField] GameObject networkWheelR;
        [SerializeField] Animator audioAnim;
        [SerializeField] Transform entrypoint;
        [SerializeField] SideBrake sidebrake;

        [SerializeField] GameObject leftGrip;
        [SerializeField] GameObject rightGrip;
        private VRCStation vrcStation;
        void Start() {
            vrcStation = (VRCStation)GetComponent(typeof(VRCStation));
        }
        public override void OnPlayerJoined(VRCPlayerApi player) {
            if (player.isMaster) {
                updateCore.setOwner(true);
            }
        }

        public override void Interact() {
            Networking.LocalPlayer.UseAttachedStation();
        }
        public override void OnStationEntered(VRCPlayerApi player) {
            updateCore.setOwner(player.isLocal);
            updateCore.setDriver(player.isLocal);

            leftGrip.SetActive(player.isLocal);
            rightGrip.SetActive(player.isLocal);

            audioAnim.Play("StartUp.StartUp", 1, 0f);
            audioAnim.SetBool("HasDriver", true);

            if (!player.isLocal) { return; }
            sidebrake.nosidebrake();

            Networking.SetOwner(Networking.LocalPlayer, physicalBody);
            Networking.SetOwner(Networking.LocalPlayer, networkEngine);
            Networking.SetOwner(Networking.LocalPlayer, networkWheelL);
            Networking.SetOwner(Networking.LocalPlayer, networkWheelR);

            SendCustomEventDelayedSeconds("enter", 2f);
        }
        public void enter() {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            height = this.transform.position.y + 0.05f //5cm浮かせる
                - Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftUpperLeg).y;
            RequestSerialization();
        }

        public override void InputJump(bool value, UdonInputEventArgs args) {
            vrcStation.ExitStation(Networking.LocalPlayer);
        }

        public override void OnStationExited(VRCPlayerApi player) {
            audioAnim.SetBool("HasDriver", false);
            
            updateCore.setOwner(player.isLocal);
            updateCore.setDriver(false);

            leftGrip.SetActive(false);
            rightGrip.SetActive(false);

            if (!player.isLocal) { return; }
            sidebrake.sidebrake();

            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            height = 0f;
            RequestSerialization();
        }
    }
}
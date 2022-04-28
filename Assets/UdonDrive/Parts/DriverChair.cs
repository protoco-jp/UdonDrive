
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
                Debug.Log("toggling the object...");
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

        private bool someoneDriving = false;
        private bool ownable = false;
        private VRCStation vrcStation;
        void Start() {
            vrcStation = (VRCStation)GetComponent(typeof(VRCStation));
        }
        public override void OnPlayerJoined(VRCPlayerApi player) {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "askOwner");
            if (!player.isLocal) { return; }
            if (!Networking.IsMaster) { return; }
            updateCore.setDriver(true);
        }
        public void askOwner() {
            if (someoneDriving == true) {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "hasDriver");
            }
        }
        public void hasDriver() {
            audioAnim.SetBool("HasDriver", true);
        }

        public override void Interact() {
            Networking.LocalPlayer.UseAttachedStation();
        }
        public override void OnStationEntered(VRCPlayerApi player) {
            if (!player.isLocal) { return; }
            Networking.SetOwner(Networking.LocalPlayer, physicalBody);
            Networking.SetOwner(Networking.LocalPlayer, networkEngine);
            Networking.SetOwner(Networking.LocalPlayer, networkWheelL);
            Networking.SetOwner(Networking.LocalPlayer, networkWheelR);

            ownable = true;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "changeDriver");
            SendCustomEventDelayedSeconds("enter", 2f);
        }
        public void changeDriver() {
            audioAnim.Play("StartUp.StartUp", 1, 0f);
            audioAnim.SetBool("HasDriver", true);
            someoneDriving = true;

            if (ownable) {
                sidebrake.nosidebrake();
                updateCore.setDriver(true);
                leftGrip.SetActive(true);
                rightGrip.SetActive(true);
                ownable = false;
            } else {
                updateCore.setDriver(false);
                leftGrip.SetActive(false);
                rightGrip.SetActive(false);
            }
        }
        public override void InputJump(bool value, UdonInputEventArgs args) {
            vrcStation.ExitStation(Networking.LocalPlayer);
        }
        public override void OnStationExited(VRCPlayerApi player) {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "quitDriver");
            if (player.isLocal) {
                sidebrake.sidebrake();
                Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
                height = 0f;
            }
        }
        public void quitDriver() {
            audioAnim.SetBool("HasDriver", false);
            someoneDriving = false;
            updateCore.setSideBrake(true);
        }

        public void enter() {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            height = this.transform.position.y + 0.05f //5cm浮かせる
                - Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftUpperLeg).y;
        }
    }
}
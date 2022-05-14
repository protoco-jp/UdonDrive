
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AudioOnOff : UdonSharpBehaviour {
        [SerializeField] GameObject gameObj;
        public override void Interact() {
            gameObj.SetActive(!gameObj.activeSelf);
        }
    }
}
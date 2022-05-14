
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Respawn : UdonSharpBehaviour {
        [SerializeField] float refreshRange = 50f;
        [SerializeField] Transform[] respawnPoints;
        [SerializeField] GameObject physicalBody;
        [SerializeField] Rigidbody rigid;
        private Transform currentRespawn;
        public override void Interact() {
            if (!Networking.IsOwner(physicalBody)) { return; }
            rigid.velocity = Vector3.zero;
            rigid.transform.position = currentRespawn.position;
            rigid.transform.rotation = currentRespawn.rotation;
        }
        void Start() {
            currentRespawn = respawnPoints[0];
        }
    }
}
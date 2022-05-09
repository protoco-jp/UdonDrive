
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Grip : UdonSharpBehaviour {
        [SerializeField] HandType handType;
        [SerializeField] UpdateCore updateCore;
        [SerializeField] VRC_Pickup pickup;
        [SerializeField] MeshRenderer gripRenderer;

        public override void OnPickup() {
            VRC_Pickup.PickupHand pickupHand = handType == HandType.LEFT
                ? VRC_Pickup.PickupHand.Left : VRC_Pickup.PickupHand.Right;
            if(pickupHand != pickup.currentHand){
                pickup.Drop();
                return;
            }
            updateCore.setHold(handType, true);
            gripRenderer.enabled = false;
            pickup.pickupable = false;
            pickup.Drop();
        }
    }
}
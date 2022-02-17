
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TakeOwner : UdonSharpBehaviour {
    [SerializeField] GameObject gameObj;
    public override void Interact() {
        Networking.SetOwner(Networking.LocalPlayer, gameObj);
    }
}

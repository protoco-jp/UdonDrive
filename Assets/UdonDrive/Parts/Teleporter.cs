
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Teleporter : UdonSharpBehaviour {
    [SerializeField] VRCStation[] vrcStations;
    int chairItr = 0;
    public override void Interact() {
        tryUseStation();
        SendCustomEventDelayedSeconds("unlock", 15f);
    }
    public void tryUseStation() {
        float distance = Vector3.Distance(this.transform.position, Networking.LocalPlayer.GetPosition());
        if (distance > 3f) { return; }
        vrcStations[chairItr].UseStation(Networking.LocalPlayer);
        chairItr++;
        chairItr %= vrcStations.Length;
        SendCustomEventDelayedSeconds("tryUseStation", 1f);
    }
    public void unlock() {
        this.DisableInteractive = false;
    }
}

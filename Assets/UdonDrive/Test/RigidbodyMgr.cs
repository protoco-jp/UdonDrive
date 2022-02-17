
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class RigidbodyMgr : UdonSharpBehaviour {
    [SerializeField] VRC.SDK3.Components.VRCObjectSync objSync;
    [SerializeField] Transform trans;

    private Vector3 pos;

    void Start() {
        pos = trans.position;
    }

    public override void Interact() {
        objSync.SetKinematic(false);
        SendCustomEventDelayedSeconds("restore", 5.0f);
    }

    public void restore() {
        objSync.SetKinematic(true);
        trans.position = pos;
    }
}

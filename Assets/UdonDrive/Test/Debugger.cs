
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Debugger : UdonSharpBehaviour
{
    [SerializeField] Rigidbody rigid;

    void Start()
    {
        SendCustomEventDelayedSeconds("loopme",1f);
    }

    public void loopme(){
        Debug.Log(rigid.isKinematic);
        SendCustomEventDelayedSeconds("loopme",1f);
    }
}

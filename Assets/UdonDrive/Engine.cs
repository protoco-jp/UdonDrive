
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace UdonDrive {
    public class Engine : UdonSharpBehaviour {
        [SerializeField] Transform handle;
        [SerializeField] Transform leftGrip;
        [SerializeField] Transform rightGrip;

        [SerializeField] Transform testObjLeft;
        [SerializeField] Transform testObjRight;
        void Update() {
            Vector3 leftVec = Vector3.ProjectOnPlane(
                leftGrip.position - handle.position,
                handle.forward
            );
            Vector3 rightVec = Vector3.ProjectOnPlane(
                rightGrip.position - handle.position,
                handle.forward
            );
            testObjLeft.position = leftVec;
            testObjRight.position = rightVec;
        }
    }
}

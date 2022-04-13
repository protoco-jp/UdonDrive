
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UpdateCore : UdonSharpBehaviour {
        [SerializeField] float torqueAmp = 700f;
        [SerializeField] float brakeAmp = 1000f;
        [SerializeField] float steeringAmp = 0.3f;
        [Range(0, 1)][SerializeField] float steeringRewindSpeed = 0.02f;
        [Range(0, 1)][SerializeField] float footBrakeRatio = 0.8f;
        [Range(1, 10)][SerializeField] float networkBodySpeedSlope = 6;
        [SerializeField] WheelCollider[] drivenWheel;
        [SerializeField] WheelCollider[] drivingWheel;
        [SerializeField] Transform[] drivenShaft;
        [SerializeField] Transform[] drivingShaft;
        [SerializeField] Transform[] visualDrivenWheel;
        [SerializeField] Transform[] visualDrivingWheel;

        [SerializeField] float velocityAmp = 0.01f;
        [SerializeField] Transform velocityOffset;
        [SerializeField] Transform steeringWheel;
        [SerializeField] Transform gripLeft;
        [SerializeField] Transform gripLocalLeft;
        [SerializeField] Transform gripDefaultLeft;
        [SerializeField] Transform gripRight;
        [SerializeField] Transform gripLocalRight;
        [SerializeField] Transform gripDefaultRight;

        [SerializeField] Transform physicsTransform;
        [SerializeField] Transform followerTransform;

        [SerializeField] Transform networkEngine; //x:speed,y:angle,z:

        private bool isDriver = true;
        public void setDriver(bool _isDriver) {
            isDriver = _isDriver;
        }

        private bool holdLeft = false;
        private bool holdRight = false;
        public void setHold(HandType handType, bool stats) {
            if (handType == HandType.LEFT) {
                holdLeft = stats;
            } else {
                holdRight = stats;
            }
        }

        private float leftValue = 0f;
        private float rightValue = 0f;
        private float wheelAngle = 0f;
        private Vector3 velocity = Vector3.zero;
        private Vector3 oldPos = Vector3.zero;

        void Update() {
            if (isDriver) {
                //leftValue = Input.GetAxis("Oculus_CrossPlatform_PrimaryIndexTrigger");
                //rightValue = Input.GetAxis("Oculus_CrossPlatform_SecondaryIndexTrigger");
                followBody4Driver();
                setSteeringAngle();
                driveVisualWheel();
                setNetworkValue();
            } else {
                followBody4Passenger();
                driveNetworkVisualWheel();
            }
            checkGripHold();
        }

        void FixedUpdate() {
            if (!isDriver) { return; }
            angleWheel();
            driveWheel();
        }
        private void checkGripHold() {
            if (!holdLeft) {
                gripLeft.SetPositionAndRotation(
                    gripDefaultLeft.position,
                    gripDefaultLeft.rotation
                );
            }
            if (!holdRight) {
                gripRight.SetPositionAndRotation(
                    gripDefaultRight.position,
                    gripDefaultRight.rotation
                );
            }
        }
        private void setSteeringAngle() {
            if (!(holdLeft || holdRight)) {
                leftValue = 1; //brake
                wheelAngle = Mathf.Lerp(wheelAngle, 0f, steeringRewindSpeed * Time.deltaTime);
                steeringWheel.localRotation = Quaternion.Euler(
                    steeringWheel.localRotation.eulerAngles.x,
                    wheelAngle,
                    steeringWheel.localRotation.eulerAngles.z
                );
                return;
            }

            velocity = (velocityOffset.position - oldPos) / Time.deltaTime;
            velocity *= velocityAmp;
            oldPos = velocityOffset.position;
            if (holdLeft) {
                gripLocalLeft.SetPositionAndRotation(
                    Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftHand) + velocity,
                    gripLeft.rotation
                );
            } else {
                gripLocalLeft.SetPositionAndRotation(
                    gripDefaultLeft.position,
                    gripLeft.rotation
                );
            }
            if (holdRight) {
                gripLocalRight.SetPositionAndRotation(
                    Networking.LocalPlayer.GetBonePosition(HumanBodyBones.RightHand) + velocity,
                    gripRight.rotation
                );
            } else {
                gripLocalRight.SetPositionAndRotation(
                    gripDefaultRight.position,
                    gripRight.rotation
                );
            }

            float leftAngle = Vector2.SignedAngle(
                new Vector2(gripLocalLeft.localPosition.x, gripLocalLeft.localPosition.z),
                new Vector2(gripDefaultLeft.localPosition.x, gripDefaultLeft.localPosition.z)
            );
            float rightAngle = Vector2.SignedAngle(
                new Vector2(gripLocalRight.localPosition.x, gripLocalRight.localPosition.z),
                new Vector2(gripDefaultRight.localPosition.x, gripDefaultRight.localPosition.z)
            );
            float steeringAngle = (leftAngle + rightAngle) / 2;
            steeringWheel.localRotation = Quaternion.Euler(
                steeringWheel.localRotation.eulerAngles.x,
                wheelAngle + steeringAngle,
                steeringWheel.localRotation.eulerAngles.z
            );
            wheelAngle = wheelAngle + steeringAngle;
        }
        private void driveVisualWheel() {
            Vector3 pos;
            Quaternion rot;
            for (int i = 0; i < drivenWheel.Length; i++) {
                drivenWheel[i].GetWorldPose(out pos, out rot);
                visualDrivenWheel[i].localRotation = Quaternion.Inverse(drivenShaft[i].rotation) * rot;
                visualDrivenWheel[i].localPosition = pos - drivenShaft[i].position;
            }
            for (int i = 0; i < drivingWheel.Length; i++) {
                drivingWheel[i].GetWorldPose(out pos, out rot);
                visualDrivingWheel[i].localRotation = Quaternion.Inverse(drivingShaft[i].rotation) * rot;
                visualDrivingWheel[i].localPosition = pos - drivingShaft[i].position;
            }
        }
        private void driveNetworkVisualWheel() {

        }

        private void followBody4Driver() {
            followerTransform.SetPositionAndRotation(
                physicsTransform.position,
                physicsTransform.rotation
            );
        }
        private void setNetworkValue() {
            networkEngine.position = new Vector3(
                velocity.magnitude,
                wheelAngle * steeringAmp,
                0
            );
        }

        private void followBody4Passenger() {
            followerTransform.position = Vector3.MoveTowards(
                followerTransform.position,
                physicsTransform.position,
                networkBodySpeedSlope * Time.deltaTime * Vector3.Distance(
                    followerTransform.position,
                    physicsTransform.position
                )
            );
            followerTransform.rotation = Quaternion.RotateTowards(
                followerTransform.rotation,
                physicsTransform.rotation,
                networkBodySpeedSlope * Time.deltaTime * Quaternion.Angle(
                    followerTransform.rotation,
                    physicsTransform.rotation
                )
            );
        }
        private void angleWheel() {
            wheelAngle = 30;
            float angleW = wheelAngle * steeringAmp;
            foreach (WheelCollider wheel in drivenWheel) {
                wheel.steerAngle = angleW;
            }
        }
        private void driveWheel() {
            rightValue = 1;
            float brakeTorque = leftValue * brakeAmp;
            float inputTorque = rightValue * torqueAmp;

            foreach (WheelCollider wheel in drivenWheel) {
                wheel.brakeTorque = brakeTorque * footBrakeRatio;
            }
            foreach (WheelCollider wheel in drivingWheel) {
                wheel.brakeTorque = brakeTorque * (1 - footBrakeRatio);
                wheel.motorTorque = inputTorque;
            }
        }
    }
}
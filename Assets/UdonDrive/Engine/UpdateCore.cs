
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
        [Range(1, 720)][SerializeField] float steeringRewindSpeed = 180f;
        [Range(0, 1)][SerializeField] float footBrakeRatio = 0.8f;
        [Range(1, 10)][SerializeField] float networkBodySpeedSlope = 6;
        [Range(1f, 5)][SerializeField] float networkWheelRotationAmp = 1f;

        [SerializeField] Transform[] drivenShaft;
        [SerializeField] Transform[] drivingShaft;
        [SerializeField] WheelCollider[] drivenWheel;
        [SerializeField] WheelCollider[] drivingWheel;

        [SerializeField] Transform[] visualDrivenShaft;
        [SerializeField] Transform[] visualDrivingShaft;
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

        [SerializeField] Transform networkEngine; //x:speed,y:steering,z:
        [SerializeField] Transform networkWheel1; //x:speed,y:steering,z:
        [SerializeField] Transform networkWheel2; //x:speed,y:steering,z:

        private bool isDriver = false;
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
            velocity = (velocityOffset.position - oldPos) / Time.deltaTime;
            oldPos = velocityOffset.position;
            if (isDriver) {
                leftValue = Input.GetAxis("Oculus_CrossPlatform_PrimaryIndexTrigger");
                rightValue = Input.GetAxis("Oculus_CrossPlatform_SecondaryIndexTrigger");
                followBody4Driver();
                setSteeringAngle();
                driveVisualWheel();
                setNetworkValue();
            } else {
                followBody4Passenger();
                getNetworkValue();
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
            steeringWheel.localRotation = Quaternion.Euler(
                0f,
                wheelAngle,
                0f
            );

            if (!(holdLeft || holdRight)) {
                leftValue = 1; //brake
                wheelAngle = Mathf.MoveTowards(wheelAngle, 0f, steeringRewindSpeed * Time.deltaTime);
                return;
            }

            if (holdLeft) {
                gripLocalLeft.SetPositionAndRotation(
                    Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftHand) + velocity * velocityAmp,
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
                    Networking.LocalPlayer.GetBonePosition(HumanBodyBones.RightHand) + velocity * velocityAmp,
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
            Debug.Log(steeringAngle);
            wheelAngle = wheelAngle + steeringAngle;
        }
        private void driveVisualWheel() {
            Vector3 pos;
            Quaternion rot;

            for (int i = 0; i < drivenWheel.Length; i++) {
                visualDrivenShaft[i].localPosition = Vector3.zero;
                visualDrivenShaft[i].localRotation = Quaternion.identity;
                drivenWheel[i].GetWorldPose(out pos, out rot);
                visualDrivenWheel[i].localRotation = Quaternion.Inverse(drivenShaft[i].rotation) * rot;
                visualDrivenWheel[i].localPosition = pos - drivenShaft[i].position;
            }
            for (int i = 0; i < drivingWheel.Length; i++) {
                visualDrivingShaft[i].localPosition = Vector3.zero;
                visualDrivingShaft[i].localRotation = Quaternion.identity;
                drivingWheel[i].GetWorldPose(out pos, out rot);
                visualDrivingWheel[i].localRotation = Quaternion.Inverse(drivingShaft[i].rotation) * rot;
                visualDrivingWheel[i].localPosition = pos - drivingShaft[i].position;
            }
        }

        private void setNetworkValue() {
            networkEngine.position = new Vector3(
                0,
                wheelAngle,
                0
            );
            networkWheel1.position = new Vector3(
                visualDrivenWheel[0].localPosition.y,
                visualDrivenWheel[1].localPosition.y,
                visualDrivingWheel[0].localPosition.y
            );
            networkWheel2.position = new Vector3(
                visualDrivingWheel[1].localPosition.y,
                visualDrivingWheel[2].localPosition.y,
                visualDrivingWheel[3].localPosition.y
            );
        }

        private void getNetworkValue() {
            float sAngle = Vector3.Angle(velocityOffset.forward, velocityOffset.forward);
            float rotAngle = -(sAngle - 90) / 90;
            float rotationAngle = rotAngle * velocity.magnitude / networkWheelRotationAmp;
            for (int i = 0; i < drivenWheel.Length; i++) {
                visualDrivenWheel[i].Rotate(rotationAngle, 0, 0);
            }
            for (int i = 0; i < drivingWheel.Length; i++) {
                visualDrivingWheel[i].Rotate(rotationAngle, 0, 0);
            }

            for (int i = 0; i < visualDrivenShaft.Length; i++) {
                visualDrivenShaft[i].localRotation = Quaternion.Euler(0, networkEngine.position.y * steeringAmp, 0);
            }

            visualDrivenShaft[0].localPosition = new Vector3(0, networkWheel1.position.x, 0);
            visualDrivenShaft[1].localPosition = new Vector3(0, networkWheel1.position.y, 0);

            visualDrivingShaft[0].localPosition = new Vector3(0, networkWheel1.position.z, 0);

            visualDrivingShaft[1].localPosition = new Vector3(0, networkWheel2.position.x, 0);
            visualDrivingShaft[2].localPosition = new Vector3(0, networkWheel2.position.y, 0);
            visualDrivingShaft[3].localPosition = new Vector3(0, networkWheel2.position.z, 0);

            steeringWheel.localRotation = Quaternion.Euler(
                steeringWheel.localRotation.eulerAngles.x,
                networkEngine.position.y,
                steeringWheel.localRotation.eulerAngles.z
            );
        }

        private void followBody4Driver() {
            followerTransform.SetPositionAndRotation(
                physicsTransform.position,
                physicsTransform.rotation
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
            float angleW = wheelAngle * steeringAmp;
            foreach (WheelCollider wheel in drivenWheel) {
                wheel.steerAngle = angleW;
            }
        }
        private void driveWheel() {
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
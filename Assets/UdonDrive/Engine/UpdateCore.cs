
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UpdateCore : UdonSharpBehaviour {
        #region parameter
        [SerializeField] float _torqueAmp = 700f;
        [SerializeField] float _brakeAmp = 1000f;
        [SerializeField] float _steeringMax = 250f;
        [SerializeField] float _steeringAmp = 0.3f;
        [Range(1, 720)][SerializeField] float _steeringRewindSpeed = 180f;
        [Range(0, 1)][SerializeField] float _footBrakeRatio = 0.8f;
        [Range(1, 10)][SerializeField] float _networkBodySpeedSlope = 6;
        [Range(1f, 5)][SerializeField] float _networkWheelRotationAmp = 1f;
        #endregion

        #region wheel transform
        [SerializeField] Transform[] _drivenShaft;
        [SerializeField] Transform[] _drivingShaft;
        [SerializeField] WheelCollider[] _drivenWheel;
        [SerializeField] WheelCollider[] _drivingWheel;

        [SerializeField] Transform[] _visualDrivenShaft;
        [SerializeField] Transform[] _visualDrivingShaft;
        [SerializeField] Transform[] _visualDrivenWheel;
        [SerializeField] Transform[] _visualDrivingWheel;
        #endregion

        #region steering wheel
        [SerializeField] float _velocityAmp = 0.01f;
        [SerializeField] Transform _velocityReference;
        [SerializeField] Transform _steeringWheel;
        [SerializeField] Transform _gripLeft;
        [SerializeField] Transform _gripDefaultLeft;
        [SerializeField] Transform _gripLocalLeft;
        [SerializeField] Transform _gripRight;
        [SerializeField] Transform _gripDefaultRight;
        [SerializeField] Transform _gripLocalRight;

        #endregion

        #region body
        [SerializeField] Transform _physicsTransform;
        [SerializeField] Transform _followerTransform;
        #endregion

        #region networking
        [SerializeField] Transform _networkEngine; //x:speed y:steering z:
        [SerializeField] Transform _networkWheel1; //x:speed y:steering z:
        [SerializeField] Transform _networkWheel2; //x:speed y:steering z:
        #endregion

        #region api
        private bool _isDriver = false;
        public void setDriver(bool isDriver) {
            _isDriver = isDriver;
        }

        private bool _holdLeft = false;
        private bool _holdRight = false;
        public void setHold(HandType handType, bool stats) {
            if (handType == HandType.LEFT) {
                _holdLeft = stats;
            } else {
                _holdRight = stats;
            }
        }

        private bool _reverse = false;
        public void setReverse(bool stats) {
            _reverse = stats;
            Debug.Log(_reverse);
        }
        public bool getReverse() {
            return _reverse;
        }

        private bool _sideBrake = true;
        public void setSideBrake(bool stats) {
            _sideBrake = stats;
            Debug.Log(_sideBrake);
        }
        public bool getSideBrake() {
            return _sideBrake;
        }
        #endregion

        #region value keeper
        private float _leftValue = 0f;
        private float _rightValue = 0f;
        private float _wheelAngle = 0f;
        private Vector3 _velocity = Vector3.zero;
        private Vector3 _oldPos = Vector3.zero;
        #endregion

        #region embedded func
        void Update() {
            rotateSteeringWheel();
            getVelocity();
            if (_isDriver) {
                getInput();
                followBody4Driver();
                checkGripHold();
                setSteeringAngle();
                driveVisualWheel();
                setNetworkValue();
            } else {
                followBody4Passenger();
                getNetworkValue();
                driveNetworkWheel();
            }
        }

        void FixedUpdate() {
            if (!_isDriver) { return; }
            angleWheel();
            driveWheel();
        }
        #endregion

        #region common func
        private void rotateSteeringWheel() {
            _steeringWheel.localRotation = Quaternion.Euler(
                0f,
                _wheelAngle,
                0f
            );
        }
        private void getVelocity() {
            _velocity = (_velocityReference.position - _oldPos) / Time.deltaTime;
            _oldPos = _velocityReference.position;
        }
        #endregion

        #region driver
        private void getInput() {
            _leftValue = Input.GetAxis("Oculus_CrossPlatform_PrimaryIndexTrigger");
            _rightValue = Input.GetAxis("Oculus_CrossPlatform_SecondaryIndexTrigger");
        }
        private void followBody4Driver() {
            _followerTransform.SetPositionAndRotation(
                _physicsTransform.position,
                _physicsTransform.rotation
            );
        }
        private void checkGripHold() {
            if (!_holdLeft) {
                _gripLeft.SetPositionAndRotation(
                    _gripDefaultLeft.position,
                    _gripDefaultLeft.rotation
                );
            }
            if (!_holdRight) {
                _gripRight.SetPositionAndRotation(
                    _gripDefaultRight.position,
                    _gripDefaultRight.rotation
                );
            }
        }
        private void setSteeringAngle() {
            if (!(_holdLeft || _holdRight)) {
                _wheelAngle = Mathf.MoveTowards(
                    _wheelAngle,
                    0f,
                    _steeringRewindSpeed * Time.deltaTime
                );
                return;
            }

            if (_holdLeft) {
                _gripLocalLeft.position =
                    Networking.LocalPlayer.GetBonePosition(HumanBodyBones.LeftHand)
                    + _velocity * _velocityAmp;
            } else {
                _gripLocalLeft.position = _gripDefaultLeft.position;
            }
            if (_holdRight) {
                _gripLocalRight.position =
                    Networking.LocalPlayer.GetBonePosition(HumanBodyBones.RightHand)
                    + _velocity * _velocityAmp;
            } else {
                _gripLocalRight.position = _gripDefaultRight.position;
            }

            float leftAngle = Vector2.SignedAngle(
                new Vector2(_gripLocalLeft.localPosition.x, _gripLocalLeft.localPosition.z),
                new Vector2(_gripDefaultLeft.localPosition.x, _gripDefaultLeft.localPosition.z)
            );
            float rightAngle = Vector2.SignedAngle(
                new Vector2(_gripLocalRight.localPosition.x, _gripLocalRight.localPosition.z),
                new Vector2(_gripDefaultRight.localPosition.x, _gripDefaultRight.localPosition.z)
            );
            float steeringAngle = (leftAngle + rightAngle) / 2;

            _wheelAngle = _wheelAngle + steeringAngle;
            if(_wheelAngle > _steeringMax){
                _wheelAngle = _steeringMax;
            }else if(_wheelAngle < -_steeringMax){
                _wheelAngle = -_steeringMax;
            }
            
        }
        private void driveVisualWheel() {
            Vector3 pos;
            Quaternion rot;

            for (int i = 0; i < _drivenWheel.Length; i++) {
                _visualDrivenShaft[i].localPosition = Vector3.zero;
                _visualDrivenShaft[i].localRotation = Quaternion.identity;
                _drivenWheel[i].GetWorldPose(out pos, out rot);
                _visualDrivenWheel[i].localRotation = Quaternion.Inverse(_drivenShaft[i].rotation) * rot;
                _visualDrivenWheel[i].localPosition = pos - _drivenShaft[i].position;
            }
            for (int i = 0; i < _drivingWheel.Length; i++) {
                _visualDrivingShaft[i].localPosition = Vector3.zero;
                _visualDrivingShaft[i].localRotation = Quaternion.identity;
                _drivingWheel[i].GetWorldPose(out pos, out rot);
                _visualDrivingWheel[i].localRotation = Quaternion.Inverse(_drivingShaft[i].rotation) * rot;
                _visualDrivingWheel[i].localPosition = pos - _drivingShaft[i].position;
            }
        }
        private void setNetworkValue() {
            _networkEngine.position = new Vector3(
                0,
                _wheelAngle,
                0
            );
            _networkWheel1.position = new Vector3(
                _visualDrivenWheel[0].localPosition.y,
                _visualDrivenWheel[1].localPosition.y,
                _visualDrivingWheel[0].localPosition.y
            );
            _networkWheel2.position = new Vector3(
                _visualDrivingWheel[1].localPosition.y,
                _visualDrivingWheel[2].localPosition.y,
                _visualDrivingWheel[3].localPosition.y
            );
        }
        private void angleWheel() { //FixedUpdate
            float angleW = _wheelAngle * _steeringAmp;
            foreach (WheelCollider wheel in _drivenWheel) {
                wheel.steerAngle = angleW;
            }
        }
        private void driveWheel() { //FixedUpdate
            float brakeTorque = _leftValue * _brakeAmp;
            float inputTorque = _rightValue * _torqueAmp * (_reverse ? -1f : 1f);
            
            foreach (WheelCollider wheel in _drivenWheel) {
                wheel.brakeTorque = brakeTorque * _footBrakeRatio;
            }
            foreach (WheelCollider wheel in _drivingWheel) {
                if (_sideBrake) {
                    wheel.brakeTorque = 9999f;
                    wheel.motorTorque = 0;
                }else{
                    wheel.brakeTorque = brakeTorque * (1 - _footBrakeRatio);
                    wheel.motorTorque = inputTorque;
                }
            }
        }
        #endregion

        #region passenger
        private void followBody4Passenger() {
            _followerTransform.position = Vector3.MoveTowards(
                _followerTransform.position,
                _physicsTransform.position,
                _networkBodySpeedSlope * Time.deltaTime * Vector3.Distance(
                    _followerTransform.position,
                    _physicsTransform.position
                )
            );
            _followerTransform.rotation = Quaternion.RotateTowards(
                _followerTransform.rotation,
                _physicsTransform.rotation,
                _networkBodySpeedSlope * Time.deltaTime * Quaternion.Angle(
                    _followerTransform.rotation,
                    _physicsTransform.rotation
                )
            );
        }
        private void getNetworkValue() {
            _wheelAngle = _networkEngine.position.y;

            for (int i = 0; i < _drivenWheel.Length; i++) {
                _visualDrivenWheel[i].localPosition = Vector3.zero;
            }
            for (int i = 0; i < _drivingWheel.Length; i++) {
                _visualDrivingWheel[i].localPosition = Vector3.zero;
            }

            _visualDrivenShaft[0].localPosition = new Vector3(0, _networkWheel1.position.x, 0);
            _visualDrivenShaft[1].localPosition = new Vector3(0, _networkWheel1.position.y, 0);

            _visualDrivingShaft[0].localPosition = new Vector3(0, _networkWheel1.position.z, 0);

            _visualDrivingShaft[1].localPosition = new Vector3(0, _networkWheel2.position.x, 0);
            _visualDrivingShaft[2].localPosition = new Vector3(0, _networkWheel2.position.y, 0);
            _visualDrivingShaft[3].localPosition = new Vector3(0, _networkWheel2.position.z, 0);

            _steeringWheel.localRotation = Quaternion.Euler(
                _steeringWheel.localRotation.eulerAngles.x,
                _networkEngine.position.y,
                _steeringWheel.localRotation.eulerAngles.z
            );
        }
        private void driveNetworkWheel() {
            float sAngle = Vector3.Angle(_velocityReference.forward, _velocityReference.forward);
            float rotAngle = -(sAngle - 90) / 90;
            float rotationAngle = rotAngle * _velocity.magnitude / _networkWheelRotationAmp;
            for (int i = 0; i < _drivenWheel.Length; i++) {
                _visualDrivenWheel[i].Rotate(rotationAngle, 0, 0);
            }
            for (int i = 0; i < _drivingWheel.Length; i++) {
                _visualDrivingWheel[i].Rotate(rotationAngle, 0, 0);
            }

            for (int i = 0; i < _visualDrivenShaft.Length; i++) {
                _visualDrivenShaft[i].localRotation = Quaternion.Euler(0, _wheelAngle * _steeringAmp, 0);
            }
        }
        #endregion
    }
}
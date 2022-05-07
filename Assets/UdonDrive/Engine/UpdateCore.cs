
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace UdonDrive {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class UpdateCore : UdonSharpBehaviour {
        #region parameter
        [SerializeField] float _torqueAmp = 1200f;
        [SerializeField] float _brakeAmp = 10000f;
        [SerializeField] float _steeringMax = 270f;
        [SerializeField] float _steeringAmp = 0.16f;
        [SerializeField] float _speedMax = 130f; // km/s
        [SerializeField] float _backSpeedMax = 40f; // km/s
        [SerializeField] float _meterMax = 180f;
        [SerializeField] float _dustThreshold = 20f;
        [Range(1, 720)][SerializeField] float _steeringRewindSpeed = 180f;
        [Range(0, 1)][SerializeField] float _footBrakeRatio = 0.8f;
        [Range(1, 10)][SerializeField] float _networkBodySpeedSlope = 6;
        [Range(1f, 5)][SerializeField] float _networkWheelRotationAmp = 2f;
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
        [SerializeField] Animator _wheelDust;
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
        [SerializeField] Rigidbody _rigidBody;
        [SerializeField] Transform _followerTransform;
        [SerializeField] Animator _driveAudioAnim;
        [SerializeField] AudioSource _driveSound;
        private bool _airbrakeflag = false;
        #endregion

        #region meter
        [SerializeField] Transform _speedMeter;
        [SerializeField] Transform _tacoMeter;
        private float _speedMeterRotation = 0f;
        private float _tacoMeterRotation = 0f;
        #endregion

        #region networking
        [SerializeField] Transform _networkEngine; //x:speed y:steering z:
        [SerializeField] Transform _networkWheel1; //x:speed y:steering z:
        [SerializeField] Transform _networkWheel2; //x:speed y:steering z:
        #endregion

        #region api
        private bool _isOwner = false;
        public void setOwner(bool isOwner) {
            _isOwner = isOwner;
        }

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
        }
        public bool getReverse() {
            return _reverse;
        }

        private bool _sideBrake = true;
        public void setSideBrake(bool stats) {
            _sideBrake = stats;
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
        void Start() {
            _rigidBody.centerOfMass = new Vector3(0, 0.5f, 0);
        }
        void Update() {
            rotateSteeringWheel();
            getVelocity();
            setMeterAndSound();
            if (_isOwner) {
                if (_isDriver) {
                    getInput();
                    setAirbrake();
                } else {
                    clearInput();
                }
                followBody4Owner();
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

        private void setMeterAndSound() {
            float v = _velocity.magnitude * 3.6f; // m/s->km/h
            if (v > _meterMax) {
                v = _meterMax;
            }
            float sv = v * (270 / _meterMax); //km/h->rot
            _speedMeterRotation = Mathf.MoveTowards(_speedMeterRotation, sv, 60f * Time.deltaTime);
            _speedMeter.localRotation = Quaternion.Euler(0, _speedMeterRotation, 0);
            if ((sv + _rightValue * 3f) > 2f) {
                _driveAudioAnim.SetBool("IsDriving", true);
            } else {
                _driveAudioAnim.SetBool("IsDriving", false);
            }

            float tv = (sv % (_meterMax / 5f)) + sv * (3f / 5f);
            if (tv > _meterMax) {
                tv = _meterMax;
            }
            _tacoMeterRotation = Mathf.MoveTowards(_tacoMeterRotation, tv, 60f * Time.deltaTime);
            _tacoMeter.localRotation = Quaternion.Euler(0, _tacoMeterRotation, 0);

            _driveSound.pitch = 0.85f + (_tacoMeterRotation / (_meterMax)) + _rightValue * 0.3f;

            if (v > _dustThreshold) {
                _wheelDust.SetFloat("Blend", v / _meterMax);
            } else {
                _wheelDust.SetFloat("Blend", 0);
            }

        }
        #endregion

        #region driver
        private void getInput() {
            _leftValue = Input.GetAxis("Oculus_CrossPlatform_PrimaryIndexTrigger");
            _rightValue = Input.GetAxis("Oculus_CrossPlatform_SecondaryIndexTrigger");
        }
        private void clearInput() {
            _leftValue = 0;
            _rightValue = 0;
        }
        private void setAirbrake() {
            if (_sideBrake) { return; }
            if (_airbrakeflag && _leftValue < 0.2f) {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "airBrake");
                _airbrakeflag = false;
                return;
            }
            if (_leftValue > 0.8f) { _airbrakeflag = true; }
        }
        private void followBody4Owner() {
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
            if (_wheelAngle > _steeringMax) {
                _wheelAngle = _steeringMax;
            } else if (_wheelAngle < -_steeringMax) {
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
                _rightValue,
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

            float sAngle = Vector3.Angle(_velocityReference.forward, _velocity);
            bool movingForward = (-(sAngle - 90)) > 0 ? true : false;
            if (movingForward) {
                inputTorque *= 1 - Mathf.Min(_velocity.magnitude * 3.6f, _speedMax) / _speedMax;
            } else {
                inputTorque *= 1 - Mathf.Min(_velocity.magnitude * 3.6f, _backSpeedMax) / _backSpeedMax;
            }

            foreach (WheelCollider wheel in _drivenWheel) {
                wheel.brakeTorque = brakeTorque * _footBrakeRatio;
            }
            foreach (WheelCollider wheel in _drivingWheel) {
                if (_sideBrake) {
                    wheel.brakeTorque = 99999f;
                    wheel.motorTorque = 0;
                } else {
                    wheel.brakeTorque = brakeTorque * (1 - _footBrakeRatio);
                    wheel.motorTorque = inputTorque;
                }
            }
        }
        public void airBrake() {
            _driveAudioAnim.Play("Brake.Brake", 2, 0f);
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
            _rightValue = _networkEngine.position.x;
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
            float sAngle = Vector3.Angle(_velocityReference.forward, _velocity);
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
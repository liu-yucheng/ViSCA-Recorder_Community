// Copyright (C) 2024 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using UnityEngine;


namespace ViSCARecorder
{
    public class Wheel1 : MonoBehaviour
    {
        public GameObject WheelBodyObject;
        public bool Steerable = false;
        public bool SteerInverted = false;
        public bool Motorized = true;
        public bool MotorInverted = false;
        public bool BrakeEnabled = true;
        public bool BrakeInverted = false;

        public float SteerAngle
        {
            get
            {
                return WheelCollider.steerAngle;
            }

            set
            {
                if (Steerable)
                {
                    float value_ = value;
                    value_ *= SteerInverted ? -1f : 1f;
                    WheelCollider.steerAngle = value_;
                }
            }
        } // end property

        public float MotorTorque
        {
            get
            {
                return WheelCollider.motorTorque;
            }

            set
            {
                if (Motorized)
                {
                    float value_ = value;
                    value_ *= MotorInverted ? -1f : 1f;
                    WheelCollider.motorTorque = value_;
                }
            }
        } // end property

        public float BrakeTorque
        {
            get
            {
                return WheelCollider.brakeTorque;
            }

            set
            {
                if (BrakeEnabled)
                {
                    float value_ = value;
                    value_ *= BrakeInverted ? -1f : 1f;
                    WheelCollider.brakeTorque = value_;
                }
            }
        }

        public float SteerAngle_;
        public float MotorTorque_;
        public float BrakeTorque_;

        private WheelCollider WheelCollider;

        void Start()
        {
            WheelCollider = GetComponent<WheelCollider>();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            WheelCollider.GetWorldPose(
                out Vector3 Current_ColliderTranslation,
                out Quaternion Current_ColliderRotation
            );

            WheelBodyObject.transform.rotation = Current_ColliderRotation;
            WheelBodyObject.transform.position = Current_ColliderTranslation;

            SteerAngle_ = SteerAngle;
            MotorTorque_ = MotorTorque;
            BrakeTorque_ = BrakeTorque;
        }
    }
} // end namespace

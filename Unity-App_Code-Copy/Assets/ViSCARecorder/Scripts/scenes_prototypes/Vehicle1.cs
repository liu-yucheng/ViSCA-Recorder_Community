// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ViSCARecorder
{
    public class Vehicle1 : MonoBehaviour
    {
        public GameObject Vehicle_RigidBodyObject;

        public InputActionReference Action_Drive_PC;
        public InputActionReference Action_Drive_XR;
        public InputActionReference Action_Sickness_PC;
        public InputActionReference Action_Sickness_XR_Left1;
        public InputActionReference Action_Sickness_XR_Left2;
        public InputActionReference Action_Sickness_XR_Right1;
        public InputActionReference Action_Sickness_XR_Right2;

        public Vector2 Input_AutoPilot = Vector2.zero;
        public bool Input_AutoPilot_Enabled = false;

        public List<GameObject> Wheels_ColliderObjects;

        public GameObject Dashboard_TextObject;


        private Rigidbody Vehicle_RigidBody;
        private Vector2 Vehicle_MotorTorqueRange_NM = new Vector2(2400f, 3600f);
        private Vector2 Vehicle_BrakeTorqueRange_NM = new Vector2(2400f, 3600f);
        private Vector2 Vehicle_SpeedRange_MPerS = new Vector2(-55, 55);
        private Vector2 Vehicle_SteerRange_Degrees = new Vector2(2, 20);

        private Vector2 Input_Drive_PC = Vector2.zero;
        private Vector2 Input_Drive_XR = Vector2.zero;
        private Vector2 Input_Drive = Vector2.zero;

        private List<Wheel1> Wheels_Wheel1Instances;
        private float Wheels_ForwardSpeed_MPerS = 0f;
        private float Wheels_Speed_MPerS = 0f;
        private float Wheels_MotorFactor = 1f;
        private float Wheels_BrakeFactor = 1f;
        private float Wheels_SteerFactor = 1f;
        private float Wheels_MotorTorqueMax;
        private float Wheels_BrakeTorqueMax;
        private float Wheels_SteerAngleMax;
        private bool Wheels_MotorsEnabled = true;

        private float Input_Sickness_WeakThreshold = 0.5f;
        private float Input_Sickness_Threshold = 1f;
        private float Input_Sickness_PC = 0f;
        private float Input_Sickness_XR = 0f;
        private float Input_Sickness = 0f;

        private Text Dashboard_Text;
        private float Dashboard_KMPerH_Per_MPerS = 3.6f;
        private float Dashboard_Percent_Per_Unit = 100f;
        private Color Dashboard_NoSicknessColor = new(0f, 1f, 0f, 1f);
        private Color Dashboard_WeakSicknessColor = new(1f, 1f, 0f, 1f);
        private Color Dashboard_SicknessColor = new(1f, 0f, 0f, 1f);
        private float Dashboard_RefreshInterval_Seconds = 0.1f; // 10 Hz.
        private float Dashboard_RefreshCountdown_Seconds = 0f;
        private float Dashboard_WeakSicknessThreshold = 0.5f;
        private float Dashboard_SicknessThreshold = 1f;
        private float Dashboard_Speed_KMPerH = 0f;
        private float Dashboard_Power_Percent = 0f;
        private float Dashboard_Heading_Degrees = 0f;
        private string Dashboard_Time_Custom = "0 days, 00:00:00.000";
        private float Dashboard_Sickness = 0f;

        void Start()
        {
            Vehicle_RigidBodyObject = Vehicle_RigidBodyObject == null ? gameObject : Vehicle_RigidBodyObject;
            Vehicle_RigidBody = Vehicle_RigidBodyObject.GetComponent<Rigidbody>();
            Wheels_Wheel1Instances = new();

            foreach (GameObject WheelColliderObject in Wheels_ColliderObjects)
            {
                Wheel1 Wheel = WheelColliderObject.GetComponent<Wheel1>();
                Wheels_Wheel1Instances.Add(Wheel);
            }

            Wheels_MotorTorqueMax = Vehicle_MotorTorqueRange_NM.y;
            Wheels_BrakeTorqueMax = Vehicle_BrakeTorqueRange_NM.y;
            Wheels_SteerAngleMax = Vehicle_SteerRange_Degrees.y;
            Dashboard_Text = Dashboard_TextObject.GetComponent<Text>();
        }

        void FixedUpdate()
        {
            FindDriveInput();
            RefreshWheels();
            FindSicknessInput();

            Dashboard_RefreshCountdown_Seconds -= Time.fixedDeltaTime;

            if (Dashboard_RefreshCountdown_Seconds < Time.fixedDeltaTime)
            {
                Dashboard_RefreshCountdown_Seconds = Dashboard_RefreshInterval_Seconds;
                RefreshDashboard();
            }
        }

        private void FindDriveInput()
        {
            if (Input_AutoPilot_Enabled)
            {
                Input_Drive = Input_AutoPilot;
            }
            else
            {
                Input_Drive_PC = Action_Drive_PC.action.ReadValue<Vector2>();
                Input_Drive_XR = Action_Drive_XR.action.ReadValue<Vector2>();
                Input_Drive = Input_Drive_PC + Input_Drive_XR;
            }

            Input_Drive.x = Math.Clamp(Input_Drive.x, -1f, 1f);
            Input_Drive.y = Math.Clamp(Input_Drive.y, -1f, 1f);
        }

        private void RefreshWheels()
        {
            float ThrottleBrake = Input_Drive.y;
            float SteeringWheel = Input_Drive.x;

            Wheels_ForwardSpeed_MPerS = Vector3.Dot(transform.forward, Vehicle_RigidBody.velocity);
            Wheels_Speed_MPerS = Mathf.Abs(Wheels_ForwardSpeed_MPerS);
            Wheels_MotorFactor = Mathf.InverseLerp(Vehicle_SpeedRange_MPerS.y, 0, Wheels_Speed_MPerS);
            Wheels_BrakeFactor = Mathf.InverseLerp(Vehicle_SpeedRange_MPerS.y, 0, Wheels_Speed_MPerS);
            Wheels_SteerFactor = Mathf.InverseLerp(Vehicle_SpeedRange_MPerS.y, 0, Wheels_Speed_MPerS);
            Wheels_MotorTorqueMax = Mathf.Lerp(Vehicle_MotorTorqueRange_NM.x, Vehicle_MotorTorqueRange_NM.y, Wheels_MotorFactor);
            Wheels_BrakeTorqueMax = Mathf.Lerp(Vehicle_BrakeTorqueRange_NM.x, Vehicle_BrakeTorqueRange_NM.y, Wheels_BrakeFactor);
            Wheels_SteerAngleMax = Mathf.Lerp(Vehicle_SteerRange_Degrees.x, Vehicle_SteerRange_Degrees.y, Wheels_SteerFactor);
            Wheels_MotorsEnabled = Mathf.Sign(ThrottleBrake) == Mathf.Sign(Wheels_ForwardSpeed_MPerS) || Wheels_Speed_MPerS < 0.5f;

            foreach (Wheel1 Wheel in Wheels_Wheel1Instances)
            {
                Wheel.SteerAngle = SteeringWheel * Wheels_SteerAngleMax;

                if (Wheels_MotorsEnabled)
                {
                    if (Wheels_ForwardSpeed_MPerS > Vehicle_SpeedRange_MPerS.x && Wheels_ForwardSpeed_MPerS < Vehicle_SpeedRange_MPerS.y)
                    {
                        Wheel.MotorTorque = ThrottleBrake * Wheels_MotorTorqueMax;
                    }
                    else
                    {
                        Wheel.MotorTorque = 0;
                    }

                    Wheel.BrakeTorque = 0;
                }
                else
                {
                    Wheel.MotorTorque = 0;
                    Wheel.BrakeTorque = Mathf.Abs(ThrottleBrake) * Wheels_BrakeTorqueMax;
                }
            }
        } // end method

        private void FindSicknessInput()
        {
            Input_Sickness_PC = Action_Sickness_PC.action.ReadValue<float>();
            float Input_Sickness_XR_Left1 = Action_Sickness_XR_Left1.action.ReadValue<float>();
            float Input_Sickness_XR_Left2 = Action_Sickness_XR_Left2.action.ReadValue<float>();
            float Input_Sickness_XR_Right1 = Action_Sickness_XR_Right1.action.ReadValue<float>();
            float Input_Sickness_XR_Right2 = Action_Sickness_XR_Right2.action.ReadValue<float>();
            Input_Sickness_XR = 0f;

            if (
                Input_Sickness_XR_Left1 >= Input_Sickness_WeakThreshold
                || Input_Sickness_XR_Left2 >= Input_Sickness_WeakThreshold
                || Input_Sickness_XR_Right1 >= Input_Sickness_WeakThreshold
                || Input_Sickness_XR_Right2 >= Input_Sickness_WeakThreshold
            )
            {
                Input_Sickness_XR = 0.5f;
            }
            
            if (
                Input_Sickness_XR_Left1 >= Input_Sickness_Threshold
                || Input_Sickness_XR_Left2 >= Input_Sickness_Threshold
                || Input_Sickness_XR_Right1 >= Input_Sickness_Threshold
                || Input_Sickness_XR_Right2 >= Input_Sickness_Threshold
            )
            {
                Input_Sickness_XR = 1f;
            }

            if (Input_Sickness_PC > 0f)
            {
                Input_Sickness = Input_Sickness_PC;
            }
            else if (Input_Sickness_XR > 0f)
            {
                Input_Sickness = Input_Sickness_XR;
            }
            else
            {
                Input_Sickness = 0f;
            }
        }

        private void RefreshDashboard()
        {
            float ForwardSpeed_MPerS = Vector3.Dot(transform.forward, Vehicle_RigidBody.velocity);
            Dashboard_Speed_KMPerH = ForwardSpeed_MPerS * Dashboard_KMPerH_Per_MPerS;
            Dashboard_Power_Percent = Input_Drive.y * Dashboard_Percent_Per_Unit;
            Vector3 Rotation = transform.rotation.eulerAngles;
            NormalizeEulerAngles(ref Rotation);
            Dashboard_Heading_Degrees = Rotation.y;
            TimeSpan TimeSpan_ = TimeSpan.FromSeconds(Time.time);
            Dashboard_Time_Custom = $"{TimeSpan_:dd} days, {TimeSpan_:hh}:{TimeSpan_:mm}:{TimeSpan_:ss}.{TimeSpan_:fff}";
            Dashboard_Sickness = Input_Sickness;

            string Text_ =
                $"begin Dashboard | 仪表板\n"
                + $"\n"
                + $"Speed     | 速度： {Dashboard_Speed_KMPerH:000.0} kM/H\n"
                + $"Power     | 功率： {Dashboard_Power_Percent:000.0} %\n"
                + $"Heading   | 航向： {Dashboard_Heading_Degrees:000.0} Degrees\n"
                + $"Time      | 时间： {Dashboard_Time_Custom}\n"
                + $"Sickness  | 眩晕： {Dashboard_Sickness:0.0} / 1.0\n"
                + $"\n"
                + $"end Dashboard | 仪表板\n"
            ;

            Color Color_ = Dashboard_NoSicknessColor;

            if (Input_Sickness >= Dashboard_WeakSicknessThreshold)
            {
                Color_ = Dashboard_WeakSicknessColor;
            }
            
            if (Input_Sickness >= Dashboard_SicknessThreshold) {
                Color_ = Dashboard_SicknessColor;
            }

            Dashboard_Text.text = Text_;
            Dashboard_Text.color = Color_;
        }

        private void NormalizeEulerAngles(ref Vector3 angles)
        {
            angles.x %= 360f;
            angles.y %= 360f;
            angles.z %= 360f;

            if (angles.x > 180f)
            {
                angles.x -= 360f;
            }
            else if (angles.x < -180f)
            {
                angles.x += 360f;
            }

            if (angles.y > 180f)
            {
                angles.y -= 360f;
            }
            else if (angles.y < -180f)
            {
                angles.y += 360f;
            }

            if (angles.z > 180f)
            {
                angles.z -= 360f;
            }
            else if (angles.z < -180f)
            {
                angles.z += 360f;
            }
        }
    }
} // end namespace

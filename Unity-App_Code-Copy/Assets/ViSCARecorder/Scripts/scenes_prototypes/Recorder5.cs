// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System;
using System.Collections;
using System.Collections.Generic;

using TMPro;

using Unity.XR.PXR;

using UnityEngine;
using UnityEngine.XR;

namespace ViSCARecorder
{
    public class Recorder5 : MonoBehaviour
    {
        public GameObject xrOriginObject;
        public GameObject debugOutObject;
        public GameObject eyeGazeObject;
        public GameObject leftEyeOpennessObject;
        public GameObject rightEyeOpennessObject;

        private InputDevice? headDevice;
        private InputDevice? leftHandDevice;
        private InputDevice? rightHandDevice;
        private Vector3 eyeGazePosition;
        private Vector3 eyeGazeRotation;
        private float leftEyeOpenness;
        private float rightEyeOpenness;
        private Vector3 headPosition;
        private Vector3 headRotation;
        private Vector3 leftHandPosition;
        private Vector3 leftHandRotation;
        private Vector3 rightHandPosition;
        private Vector3 rightHandRotation;
        private TextMeshProUGUI debugOutTMP;
        private Vector3 leftEyeOpennessOriginalScale;
        private Vector3 rightEyeOpennessOriginalScale;
        private float leftEyeOpennessScaleYMin = 0.2f;
        private float leftEyeOpennessScaleYMax = 1.0f;
        private float rightEyeOpennessScaleYMin = 0.2f;
        private float rightyeOpennessScaleYMax = 1.0f;

        void Start()
        {
            Application.runInBackground = true;
            StartTrackedDevices();
            StartDebugOut();
            StartEyeGazeObjects();
        }

        void Update()
        {
            UpdateEyeGazeLegacy();
            UpdateTrackedDevices();
            UpdateControllers();
            UpdateDebugOut();
            UpdateEyeGazeObjects();
        }

        private void StartDebugOut()
        {
            debugOutTMP = debugOutObject.GetComponent<TextMeshProUGUI>();

            string debugOutString =
                "- begin ViSCARecorder.Recorder5.Start\n"
                + "\n"
                + "date_time_custom: 00000101-000000-000000-utc+0000\n"
                + "date_time_standard: 0000-01-01T00:00:00.000000+00:00\n"
                + "date_time_unix_ms: 9223372036854775807\n"
                + "eye_gaze_position: (000.000, 000.000, 000.000)\n"
                + "eye_gaze_rotation: (000.000, 000.000, 000.000)\n"
                + "left_eye_openness: 0.000\n"
                + "right_eye_openness: 0.000\n"
                + "head_position: (000.000, 000.000, 000.000)\n"
                + "head_rotation: (000.000, 000.000, 000.000)\n"
                + "left_hand_position: (000.000, 000.000, 000.000)\n"
                + "left_hand_rotation: (000.000, 000.000, 000.000)\n"
                + "right_hand_position: (000.000, 000.000, 000.000)\n"
                + "right_hand_rotation: (000.000, 000.000, 000.000)\n"
                + "\n"
                + "- end ViSCARecorder.Recorder5.Start\n"
            ;

            debugOutTMP.text = debugOutString;
        }

        private void UpdateDebugOut()
        {
            DateTime now = DateTime.Now;

            string dateTimeCustomString =
                $"{now:yyyyMMdd}-{now:HHmmss}-{now:ffffff}-UTC-{TimeZoneInfo.Local.BaseUtcOffset:hhmm}"
            ;

            string dateTimeStandardString = $"{now:o}";
            long unixMS = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            string dateTimeUnixMSString = $"{unixMS}";
            string eyeGazePositionString = eyeGazePosition.ToString("000.000");
            string eyeGazeRotationString = eyeGazeRotation.ToString("000.000");
            string leftEyeOpennessString = $"{leftEyeOpenness:f3}";
            string rightEyeOpennessString = $"{rightEyeOpenness:f3}";
            string headPositionString = headPosition.ToString("000.000");
            string headRotationString = headRotation.ToString("000.000");
            string leftHandPositionString = leftHandPosition.ToString("000.000");
            string leftHandRotationString = leftHandRotation.ToString("000.000");
            string rightHandPositionString = rightHandPosition.ToString("000.000");
            string rightHandRotationString = rightHandRotation.ToString("000.000");

            string debugOutString =
                $"- begin ViSCARecorder.Recorder5.Update\n"
                + $"\n"
                + $"date_time_custom: {dateTimeCustomString}\n"
                + $"date_time_standard: {dateTimeStandardString}\n"
                + $"date_time_unix_ms: {dateTimeUnixMSString}\n"
                + $"eye_gaze_position: {eyeGazePositionString}\n"
                + $"eye_gaze_rotation: {eyeGazeRotationString}\n"
                + $"left_eye_openness: {leftEyeOpennessString}\n"
                + $"right_eye_openness: {rightEyeOpennessString}\n"
                + $"head_position: {headPositionString}\n"
                + $"head_rotation: {headRotationString}\n"
                + $"left_hand_position: {leftHandPositionString}\n"
                + $"left_hand_rotation: {leftHandRotationString}\n"
                + $"right_hand_position: {rightHandPositionString}\n"
                + $"right_hand_rotation: {rightHandRotationString}\n"
                + $"\n"
                + $"- end ViSCARecorder.Recorder5.Update\n"
            ;

            debugOutTMP.text = debugOutString;
        }

        private void UpdateEyeGazeLegacy()
        {
            Matrix4x4 xrOriginMatrix = xrOriginObject.transform.localToWorldMatrix;
            PXR_EyeTracking.GetHeadPosMatrix(out Matrix4x4 headMatrix);
            PXR_EyeTracking.GetCombineEyeGazePoint(out Vector3 eyeGazePointLocal);
            PXR_EyeTracking.GetCombineEyeGazeVector(out Vector3 eyeGazeVectorLocal);
            Vector3 eyeGazePointWorld = eyeGazePointLocal;
            eyeGazePointWorld = headMatrix.MultiplyPoint(eyeGazePointWorld);
            eyeGazePointWorld = xrOriginMatrix.MultiplyPoint(eyeGazePointWorld);
            Vector3 eyeGazeVectorWorld = eyeGazeVectorLocal;
            eyeGazeVectorWorld = headMatrix.MultiplyVector(eyeGazeVectorWorld);
            eyeGazeVectorWorld = xrOriginMatrix.MultiplyVector(eyeGazeVectorWorld);
            eyeGazePosition = eyeGazePointWorld;
            eyeGazeRotation = Quaternion.LookRotation(eyeGazeVectorWorld, Vector3.up).eulerAngles;
            NormalizeEulerAngles(ref eyeGazeRotation);
            PXR_EyeTracking.GetLeftEyeGazeOpenness(out leftEyeOpenness);
            PXR_EyeTracking.GetRightEyeGazeOpenness(out rightEyeOpenness);
        }

        private void StartTrackedDevices()
        {
            UpdateDevice(
                InputDeviceCharacteristics.HeadMounted 
                    | InputDeviceCharacteristics.TrackedDevice,
                
                out headDevice
            );

            UpdateDevice(
                InputDeviceCharacteristics.Left
                    | InputDeviceCharacteristics.HeldInHand
                    | InputDeviceCharacteristics.TrackedDevice,

                out leftHandDevice
            );

            UpdateDevice(
                InputDeviceCharacteristics.Right
                    | InputDeviceCharacteristics.HeldInHand
                    | InputDeviceCharacteristics.TrackedDevice,

                out rightHandDevice
            );
        }

        private void UpdateTrackedDevices()
        {
            UpdateDevicePosition(headDevice, out headPosition);
            UpdateDeviceRotation(headDevice, out headRotation);
            UpdateDevicePosition(leftHandDevice, out leftHandPosition);
            UpdateDeviceRotation(leftHandDevice, out leftHandRotation);
            UpdateDevicePosition(rightHandDevice, out rightHandPosition);
            UpdateDeviceRotation(rightHandDevice, out rightHandRotation);
        }

        private void UpdateControllers()
        {
            UpdateController(PXR_Input.Controller.LeftController, out leftHandPosition, out leftHandRotation);
            UpdateController(PXR_Input.Controller.RightController, out rightHandPosition, out rightHandRotation);
        }

        private void StartEyeGazeObjects()
        {
            leftEyeOpennessOriginalScale = leftEyeOpennessObject.transform.localScale;
            rightEyeOpennessOriginalScale = rightEyeOpennessObject.transform.localScale;
            leftEyeOpennessScaleYMin *= leftEyeOpennessOriginalScale.y;
            leftEyeOpennessScaleYMax *= leftEyeOpennessOriginalScale.y;
            rightEyeOpennessScaleYMin *= rightEyeOpennessOriginalScale.y;
            rightyeOpennessScaleYMax *= rightEyeOpennessOriginalScale.y;
        }

        private void UpdateEyeGazeObjects()
        {
            eyeGazeObject.transform.position = eyeGazePosition;
            eyeGazeObject.transform.rotation = Quaternion.Euler(eyeGazeRotation);
            Vector3 leftEyeOpennessScale = leftEyeOpennessObject.transform.localScale;
            leftEyeOpennessScale.y = leftEyeOpennessScaleYMin + leftEyeOpenness * (leftEyeOpennessScaleYMax - leftEyeOpennessScaleYMin);
            leftEyeOpennessObject.transform.localScale = leftEyeOpennessScale;
            Vector3 rightEyeOpennessScale = rightEyeOpennessObject.transform.localScale;
            rightEyeOpennessScale.y = rightEyeOpennessScaleYMin + rightEyeOpenness * (rightyeOpennessScaleYMax - rightEyeOpennessScaleYMin);
            rightEyeOpennessObject.transform.localScale = rightEyeOpennessScale;
        }

        private void UpdateDevice(InputDeviceCharacteristics characteristics, out InputDevice? device)
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);

            if (devices.Count > 0)
            {
                device = devices[0];
            }
            else
            {
                device = null;
            }
        }

        private void UpdateDevicePosition(InputDevice? device, out Vector3 position)
        {
            Vector3 result = Vector3.zero;
            
            if (device != null)
            {
                InputDevice device_ = (InputDevice) device;
                device_.TryGetFeatureValue(CommonUsages.devicePosition, out result);
            }

            position = result;
        }

        private void UpdateDeviceRotation(InputDevice? device, out Vector3 rotation)
        {
            Quaternion resultQuaternion = Quaternion.identity;
            
            if (device != null)
            {
                InputDevice device_ = (InputDevice)device;
                device_.TryGetFeatureValue(CommonUsages.deviceRotation, out resultQuaternion);
            }

            Vector3 result = resultQuaternion.eulerAngles;
            NormalizeEulerAngles(ref result);
            rotation = result;
        }

        private void UpdateController(PXR_Input.Controller controller, out Vector3 position, out Vector3 rotation)
        {
            position = PXR_Input.GetControllerPredictPosition(controller, 0.0);
            Quaternion rotationQuaternion = PXR_Input.GetControllerPredictRotation(controller, 0.0);
            rotation = rotationQuaternion.eulerAngles;
            NormalizeEulerAngles(ref rotation);
        }

        private void NormalizeEulerAngles(ref Vector3 eulerAngles)
        {
            Vector3 result = new Vector3();

            if (eulerAngles.x > 180f)
            {
                result.x = eulerAngles.x - 360f;
            }
            else
            {
                result.x = eulerAngles.x;
            }

            if (eulerAngles.y > 180f)
            {
                result.y = eulerAngles.y - 360f;
            }
            else
            {
                result.y = eulerAngles.y;
            }

            if (eulerAngles.z > 180f)
            {
                result.z = eulerAngles.z - 360f;
            }
            else
            {
                result.z = eulerAngles.z;
            }

            eulerAngles = result;
        }
    } // end class
} // end namespace

// Copyright (C) 2024 Yucheng Liu. Under the GNU AGPL 3.0 License.
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
    public class Recorder4 : MonoBehaviour
    {
        public GameObject xrOriginObject;
        public GameObject hudOutObject;
        public GameObject eyeGazeObject;

        private InputDevice? headDevice;
        private InputDevice? leftHandDevice;
        private InputDevice? rightHandDevice;
        private Vector3 eyeGazePosition;
        private Vector3 eyeGazeRotation;
        private Vector3 headPosition;
        private Vector3 headRotation;
        private Vector3 leftHandPosition;
        private Vector3 leftHandRotation;
        private Vector3 rightHandPosition;
        private Vector3 rightHandRotation;
        private TextMeshProUGUI hudOutTMP;

        private string hudOutStringFormat =
            "ViSCARecorder.Recorder4.{0}\n"
            + "  date-time: {1}\n"
            + "  eye-gaze:\n"
            + "    position: {2}\n"
            + "    rotation: {3}\n"
            + "  head:\n"
            + "    position: {4}\n"
            + "    rotation: {5}\n"
            + "  left-hand:\n"
            + "    position: {6}\n"
            + "    rotation: {7}\n"
            + "  right-hand:\n"
            + "    position: {8}\n"
            + "    rotation: {9}\n"
        ;

        void Start()
        {
            Application.runInBackground = true;
            // StartEyeGaze();
            StartTrackedDevices();
            StartHUDOut();
        }

        void Update()
        {
            // UpdateEyeGaze();
            UpdateEyeGazeLegacy();
            UpdateTrackedDevices();
            UpdateControllers();
            UpdateHUDOut();
            UpdateEyeGazeObject();
        }

        private void OnDestroy()
        {
            // DestoryEyeGaze();
        }

        private void StartHUDOut()
        {
            hudOutTMP = hudOutObject.GetComponent<TextMeshProUGUI>();

            string hudOutString = string.Format(
                hudOutStringFormat,
                "Start",
                "",
                "", "",
                "", "",
                "", "",
                "", ""
            );

            hudOutTMP.text = hudOutString;
        }

        private void UpdateHUDOut()
        {
            DateTime now = DateTime.Now;

            string dateTimeString = string.Format(
                "{0:yyyyMMdd}-{1:HHmmss}-{2:ffffff}-UTC-{3:hhmm}",
                now, now, now, TimeZoneInfo.Local.BaseUtcOffset
            );

            string eyeGazePositionString = eyeGazePosition.ToString("000.000");
            string eyeGazeRotationString = eyeGazeRotation.ToString("000.000");
            string headPositionString = headPosition.ToString("000.000");
            string headRotationString = headRotation.ToString("000.000");
            string leftHandPositionString = leftHandPosition.ToString("000.000");
            string leftHandRotationString = leftHandRotation.ToString("000.000");
            string rightHandPositionString = rightHandPosition.ToString("000.000");
            string rightHandRotationString = rightHandRotation.ToString("000.000");

            string hudOutString = string.Format(
                hudOutStringFormat,
                "Update",
                dateTimeString,
                eyeGazePositionString, eyeGazeRotationString,
                headPositionString, headRotationString,
                leftHandPositionString, leftHandRotationString,
                rightHandPositionString, rightHandRotationString
            );

            hudOutTMP.text = hudOutString;
        }

        private void StartEyeGaze()
        {
            PXR_MotionTracking.WantEyeTrackingService();
            EyeTrackingStartInfo info = new EyeTrackingStartInfo();
            info.mode = EyeTrackingMode.PXR_ETM_BOTH;
            info.needCalibration = 1;
            PXR_MotionTracking.StartEyeTracking(ref info);
        }

        private void UpdateEyeGaze()
        {
            bool isSupported = false;
            int modeCount = 0;
            EyeTrackingMode[] supportedModes = { EyeTrackingMode.PXR_ETM_NONE };
            PXR_MotionTracking.GetEyeTrackingSupported(ref isSupported, ref modeCount, ref supportedModes);

            bool isTracking = false;
            EyeTrackingState state = new EyeTrackingState();
            PXR_MotionTracking.GetEyeTrackingState(ref isTracking, ref state);

            EyeTrackingDataGetInfo info = new EyeTrackingDataGetInfo();
            info.displayTime = 0;

            info.flags =
                EyeTrackingDataGetFlags.PXR_EYE_DEFAULT |
                EyeTrackingDataGetFlags.PXR_EYE_POSITION |
                EyeTrackingDataGetFlags.PXR_EYE_ORIENTATION
            ;

            EyeTrackingData data = new EyeTrackingData();
            PXR_MotionTracking.GetEyeTrackingData(ref info, ref data);
            PerEyeData combinedEyeData = data.eyeDatas[(int)PerEyeUsage.Combined];
            PxrPose pose = combinedEyeData.pose;
            PxrVector3f position = pose.position;
            PxrVector4f rotation = pose.orientation;
            eyeGazePosition = new Vector3(position.x, position.y, position.z);
            eyeGazeRotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w).eulerAngles;
            NormalizeEulerAngles(ref eyeGazeRotation);
        }

        private void DestoryEyeGaze()
        {
            EyeTrackingStopInfo info = new EyeTrackingStopInfo();
            PXR_MotionTracking.StopEyeTracking(ref info);
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

        private void UpdateEyeGazeObject()
        {
            eyeGazeObject.transform.position = eyeGazePosition;
            eyeGazeObject.transform.rotation = Quaternion.Euler(eyeGazeRotation);
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

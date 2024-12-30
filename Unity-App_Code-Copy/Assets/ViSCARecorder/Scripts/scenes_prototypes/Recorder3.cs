// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System;
using System.Collections;
using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace ViSCARecorder
{
    public class Recorder3 : MonoBehaviour
    {
        public GameObject hudOutObject;

        public InputActionReference eyeGazePositionInput;
        public InputActionReference eyeGazeRotationInput;
        public InputActionReference headPositionInput;
        public InputActionReference headRotationInput;
        public InputActionReference leftHandPositionInput;
        public InputActionReference leftHandRotationInput;
        public InputActionReference rightHandPositionInput;
        public InputActionReference rightHandRotationInput;

        private TextMeshProUGUI hudOutTMP;

        private string hudOutStringFormat =
            "ViSCARecorder.Recorder3.{0}\n"
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
            + "  android:\n"
            + "    activity-available: {10}\n"
            + "    bridge-available: {11}\n"
        ;

        private Vector3 eyeGazePosition;
        private Vector3 eyeGazeRotation;
        private Vector3 headPosition;
        private Vector3 headRotation;
        private Vector3 leftHandPosition;
        private Vector3 leftHandRotation;
        private Vector3 rightHandPosition;
        private Vector3 rightHandRotation;
        private bool androidActivityAvailable;
        private bool androidBridgeAvailable;
        private AndroidJavaObject androidActivity;
        private AndroidJavaClass androidBridge;

        void Start()
        {
            hudOutTMP = hudOutObject.GetComponent<TextMeshProUGUI>();

            string hudOutString = string.Format(
                hudOutStringFormat,
                "Start",
                "",
                "", "",
                "", "",
                "", "",
                "", "",
                "", ""
            );

            hudOutTMP.text = hudOutString;

            eyeGazePositionInput.action.performed -= onPerformedEyeGazePosition;
            eyeGazeRotationInput.action.performed -= onPerformedEyeGazeRotation;
            headPositionInput.action.performed -= onPerformedHeadPosition;
            headRotationInput.action.performed -= onPerformedHeadRotation;
            leftHandPositionInput.action.performed -= onPerformedLeftHandPosition;
            leftHandRotationInput.action.performed -= onPerformedLeftHandRotation;
            rightHandPositionInput.action.performed -= onPerformedRightHandPosition;
            rightHandRotationInput.action.performed -= onPerformedRightHandRotation;

            eyeGazePositionInput.action.performed += onPerformedEyeGazePosition;
            eyeGazeRotationInput.action.performed += onPerformedEyeGazeRotation;
            headPositionInput.action.performed += onPerformedHeadPosition;
            headRotationInput.action.performed += onPerformedHeadRotation;
            leftHandPositionInput.action.performed += onPerformedLeftHandPosition;
            leftHandRotationInput.action.performed += onPerformedLeftHandRotation;
            rightHandPositionInput.action.performed += onPerformedRightHandPosition;
            rightHandRotationInput.action.performed += onPerformedRightHandRotation;

            Application.runInBackground = true;
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            androidActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            androidBridge = new AndroidJavaClass("liu_yucheng.visca_recorder.Recorder3Bridge");
            
            if (androidBridge != null)
            {
                androidBridge.CallStatic("startService");
            }
        }

        void Update()
        {
            DateTime now = DateTime.Now;
            androidActivityAvailable = androidActivity != null;
            androidBridgeAvailable = androidBridge != null;

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
                rightHandPositionString, rightHandRotationString,
                androidActivityAvailable, androidBridgeAvailable
            );

            hudOutTMP.text = hudOutString;
        }

        void OnDestroy()
        {
            eyeGazePositionInput.action.performed -= onPerformedEyeGazePosition;
            eyeGazeRotationInput.action.performed -= onPerformedEyeGazeRotation;
            headPositionInput.action.performed -= onPerformedHeadPosition;
            headRotationInput.action.performed -= onPerformedHeadRotation;
            leftHandPositionInput.action.performed -= onPerformedLeftHandPosition;
            leftHandRotationInput.action.performed -= onPerformedLeftHandRotation;
            rightHandPositionInput.action.performed -= onPerformedRightHandPosition;
            rightHandRotationInput.action.performed -= onPerformedRightHandRotation;

            if (androidBridge != null)
            {
                androidBridge.CallStatic("stopService");
            }
        }

        private void onPerformedEyeGazePosition(InputAction.CallbackContext context)
        {
            eyeGazePosition = context.ReadValue<Vector3>();
        }

        private void onPerformedEyeGazeRotation(InputAction.CallbackContext context)
        {
            eyeGazeRotation = context.ReadValue<Quaternion>().eulerAngles;
            normalizeEulerAngles(ref eyeGazeRotation);
        }

        private void onPerformedHeadPosition(InputAction.CallbackContext context)
        {
            headPosition = context.ReadValue<Vector3>();
        }

        private void onPerformedHeadRotation(InputAction.CallbackContext context)
        {
            headRotation = context.ReadValue<Quaternion>().eulerAngles;
            normalizeEulerAngles(ref headRotation);
        }

        private void onPerformedLeftHandPosition(InputAction.CallbackContext context)
        {
            leftHandPosition = context.ReadValue<Vector3>();
        }

        private void onPerformedLeftHandRotation(InputAction.CallbackContext context)
        {
            leftHandRotation = context.ReadValue<Quaternion>().eulerAngles;
            normalizeEulerAngles(ref leftHandRotation);
        }

        private void onPerformedRightHandPosition(InputAction.CallbackContext context)
        {
            rightHandPosition = context.ReadValue<Vector3>();
        }

        private void onPerformedRightHandRotation(InputAction.CallbackContext context)
        {
            rightHandRotation = context.ReadValue<Quaternion>().eulerAngles;
            normalizeEulerAngles(ref rightHandRotation);
        }

        private void normalizeEulerAngles(ref Vector3 eulerAngles)
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
    }
}

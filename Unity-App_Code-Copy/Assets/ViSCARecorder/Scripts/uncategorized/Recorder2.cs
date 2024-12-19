// Copyright (C) 2024 Yucheng Liu. Under the GNU AGPL 3.0 License.
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
    public class Recorder2 : MonoBehaviour
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
            "ViSCARecorder.Recorder2.{0}\n"
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

        private Vector3 eyeGazePosition;
        private Vector3 eyeGazeRotation;
        private Vector3 headPosition;
        private Vector3 headRotation;
        private Vector3 leftHandPosition;
        private Vector3 leftHandRotation;
        private Vector3 rightHandPosition;
        private Vector3 rightHandRotation;

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
        }

        void Update()
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
        }

        private void onPerformedEyeGazePosition(InputAction.CallbackContext context)
        {
            eyeGazePosition = context.ReadValue<Vector3>();
        }

        private void onPerformedEyeGazeRotation(InputAction.CallbackContext context)
        {
            eyeGazeRotation = context.ReadValue<Quaternion>().eulerAngles;
        }

        private void onPerformedHeadPosition(InputAction.CallbackContext context)
        {
            headPosition = context.ReadValue<Vector3>();
        }

        private void onPerformedHeadRotation(InputAction.CallbackContext context)
        {
            headRotation = context.ReadValue<Quaternion>().eulerAngles;
        }

        private void onPerformedLeftHandPosition(InputAction.CallbackContext context)
        {
            leftHandPosition = context.ReadValue<Vector3>();
        }

        private void onPerformedLeftHandRotation(InputAction.CallbackContext context)
        {
            leftHandRotation = context.ReadValue<Quaternion>().eulerAngles;
        }

        private void onPerformedRightHandPosition(InputAction.CallbackContext context)
        {
            rightHandPosition = context.ReadValue<Vector3>();
        }

        private void onPerformedRightHandRotation(InputAction.CallbackContext context)
        {
            rightHandRotation = context.ReadValue<Quaternion>().eulerAngles;
        }
    }
}

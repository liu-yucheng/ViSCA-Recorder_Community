// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System;
using System.Collections;
using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace ViSCARecorder
{
    public class Recorder1 : MonoBehaviour
    {
        public GameObject hudOutObject;
        public GameObject eyeGazeObject;
        public GameObject headObject;
        public GameObject leftHandObject;
        public GameObject rightHandObject;

        private TextMeshProUGUI hudOutTMP;
        
        private string hudOutStringFormat =
            "ViSCARecorder.Recorder1.{0}\n"
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
        
        private Transform eyeGazeTransform;
        private Transform headTransform;
        private Transform leftHandTransform;
        private Transform rightHandTransform;

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

            eyeGazeTransform = eyeGazeObject.transform;
            headTransform = headObject.transform;
            leftHandTransform = leftHandObject.transform;
            rightHandTransform = rightHandObject.transform;
        }

        void Update()
        {
            DateTime now = DateTime.Now;

            string dateTimeString = string.Format(
                "{0:yyyyMMdd}-{1:HHmmss}-{2:ffffff}-UTC-{3:hhmm}",
                now, now, now, TimeZoneInfo.Local.BaseUtcOffset
            );

            string eyeGazePositionString = eyeGazeTransform.position.ToString("000.000");
            string eyeGazeRotationString = eyeGazeTransform.rotation.eulerAngles.ToString("000.000");
            string headPositionString = headTransform.position.ToString("000.000");
            string headRotationString = headTransform.rotation.eulerAngles.ToString("000.000");
            string leftHandPositionString = leftHandTransform.position.ToString("000.000");
            string leftHandRotationString = leftHandTransform.rotation.eulerAngles.ToString("000.000");
            string rightHandPositionString = rightHandTransform.position.ToString("000.000");
            string rightHandRotationString = rightHandTransform.rotation.eulerAngles.ToString("000.000");

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
    }
}

// Copyright (C) 2024 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System;
using System.Collections;
using System.Collections.Generic;

using TMPro;

using Unity.XR.PXR;

using UnityEngine;
using UnityEngine.XR;

using ViSCARecorder;
using ViSCARecorder.Recorder7NameSpace;

namespace ViSCARecorder
{
    public class Recorder7 : MonoBehaviour
    {
        public GameObject debugOutObject;
        public GameObject xrOriginObject;
        public GameObject eyeGazeObject;
        public GameObject leftEyeOpennessObject;
        public GameObject rightEyeOpennessObject;
        public GameObject faceLeftEyeObject;
        public GameObject faceRightEyeObject;
        public GameObject faceHeadObject;
        public GameObject faceTeethObject;

        private Vector3 eyeGazePosition;
        private Vector3 eyeGazeRotation;
        private float leftEyeOpenness;
        private float rightEyeOpenness;
        private Dictionary<string, float?> faceBlendShapeDict;
        private Dictionary<string, float?> faceBlendShapeDictMirrored;
        private float faceLaughing;
        private Vector3 headPosition;
        private Vector3 headRotation;
        private Vector3 leftHandPosition;
        private Vector3 leftHandRotation;
        private Vector3 rightHandPosition;
        private Vector3 rightHandRotation;

        private InputDevice? headDevice;
        private InputDevice? leftHandDevice;
        private InputDevice? rightHandDevice;
        private TextMeshProUGUI debugOutTMP;
        private Vector3 leftEyeOpennessOriginalScale;
        private Vector3 rightEyeOpennessOriginalScale;
        private float leftEyeOpennessScaleYMin = 0.2f;
        private float leftEyeOpennessScaleYMax = 1.0f;
        private float rightEyeOpennessScaleYMin = 0.2f;
        private float rightyeOpennessScaleYMax = 1.0f;
        private SkinnedMeshRenderer faceLeftEyeRenderer;
        private SkinnedMeshRenderer faceRightEyeRenderer;
        private SkinnedMeshRenderer faceHeadRenderer;
        private SkinnedMeshRenderer faceTeethRenderer;
        private List<int> faceLeftEyeShapeIndexes;
        private List<int> faceRightEyeShapeIndexes;
        private List<int> faceHeadShapeIndexes;
        private List<int> faceTeethShapeIndexes;
        private bool faceMirrorEnabled = true;
        private float faceBlendShapeMultiplier = 1.0f;

        void Start()
        {
            Application.runInBackground = true;
            StartFaceLegacy();
            StartTrackedDevices();
            StartDebugOut();
            StartEyeGazeObjects();
            StartFaceObjects();
        }

        void Update()
        {
            UpdateEyeGazeLegacy();
            UpdateFaceLegacy();
            UpdateTrackedDevices();
            UpdateControllers();
            UpdateDebugOut();
            UpdateEyeGazeObjects();
            UpdateFaceObjects();
        }

        void OnDestroy()
        {
            DestoryFaceLegacy();
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
            __.NormalizeEulerAngles(ref eyeGazeRotation);
            PXR_EyeTracking.GetLeftEyeGazeOpenness(out leftEyeOpenness);
            PXR_EyeTracking.GetRightEyeGazeOpenness(out rightEyeOpenness);
        }

        private void StartFaceLegacy()
        {
            PXR_System.EnableLipSync(true);
            PXR_System.EnableFaceTracking(true);
        }

        private unsafe void UpdateFaceLegacy()
        {
            PxrFaceTrackingInfo info = new PxrFaceTrackingInfo();
            PXR_System.GetFaceTrackingData(0, GetDataType.PXR_GET_FACELIP_DATA, ref info);
            __.FindShapeArray(info, out float[] blendShapeArray);
            __.ShapeArrayToDict(blendShapeArray, out faceBlendShapeDict, false);
            __.ShapeArrayToDict(blendShapeArray, out faceBlendShapeDictMirrored, true);
            faceLaughing = info.laughingProb;
        }

        private void DestoryFaceLegacy()
        {
            PXR_System.EnableLipSync(false);
            PXR_System.EnableFaceTracking(false);
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

        private void StartDebugOut()
        {
            debugOutTMP = debugOutObject.GetComponent<TextMeshProUGUI>();

            string debugOutString =
                "- begin ViSCARecorder.Recorder7.Start\n"
                + "\n"
                + "date_time_custom: 00000101-000000-000000-utc+0000\n"
                + "date_time_standard: 0000-01-01T00:00:00.000000+00:00\n"
                + "date_time_unix_ms: 9223372036854775807\n"
                + "eye_gaze_position: (000.000, 000.000, 000.000)\n"
                + "eye_gaze_rotation: (000.000, 000.000, 000.000)\n"
                + "left_eye_openness: 0.000\n"
                + "right_eye_openness: 0.000\n"
                + "face_blend_shape_dict: {...}\n"
                + "face_laughing: 0.000\n"
                + "head_position: (000.000, 000.000, 000.000)\n"
                + "head_rotation: (000.000, 000.000, 000.000)\n"
                + "left_hand_position: (000.000, 000.000, 000.000)\n"
                + "left_hand_rotation: (000.000, 000.000, 000.000)\n"
                + "right_hand_position: (000.000, 000.000, 000.000)\n"
                + "right_hand_rotation: (000.000, 000.000, 000.000)\n"
                + "\n"
                + "- end ViSCARecorder.Recorder7.Start\n"
            ;

            debugOutTMP.text = debugOutString;
        }

        private void UpdateDebugOut()
        {
            DateTime now = DateTime.Now;
            string dateTimeCustomString = $"{now:yyyyMMdd}-{now:HHmmss}-{now:ffffff}";
            TimeSpan baseUTCOffset = TimeZoneInfo.Local.BaseUtcOffset;

            if (baseUTCOffset > TimeSpan.Zero)
            {
                dateTimeCustomString += $"-utc+{baseUTCOffset:hhmm}";
            }
            else
            {
                dateTimeCustomString += $"-utc{baseUTCOffset:hhmm}";
            }

            string dateTimeStandardString = $"{now:o}";
            long unixMS = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            string dateTimeUnixMSString = $"{unixMS}";
            string eyeGazePositionString = eyeGazePosition.ToString("000.000");
            string eyeGazeRotationString = eyeGazeRotation.ToString("000.000");
            string leftEyeOpennessString = $"{leftEyeOpenness:f3}";
            string rightEyeOpennessString = $"{rightEyeOpenness:f3}";
            __.ShapeDictToString(faceBlendShapeDict, out string faceBlendShapeDictString);
            string faceLaughingString = $"{faceLaughing:f3}";
            string headPositionString = headPosition.ToString("000.000");
            string headRotationString = headRotation.ToString("000.000");
            string leftHandPositionString = leftHandPosition.ToString("000.000");
            string leftHandRotationString = leftHandRotation.ToString("000.000");
            string rightHandPositionString = rightHandPosition.ToString("000.000");
            string rightHandRotationString = rightHandRotation.ToString("000.000");

            string debugOutString =
                $"- begin ViSCARecorder.Recorder7.Update\n"
                + $"\n"
                + $"date_time_custom: {dateTimeCustomString}\n"
                + $"date_time_standard: {dateTimeStandardString}\n"
                + $"date_time_unix_ms: {dateTimeUnixMSString}\n"
                + $"eye_gaze_position: {eyeGazePositionString}\n"
                + $"eye_gaze_rotation: {eyeGazeRotationString}\n"
                + $"left_eye_openness: {leftEyeOpennessString}\n"
                + $"right_eye_openness: {rightEyeOpennessString}\n"
                + $"face_blend_shape_dict: {faceBlendShapeDictString}\n"
                + $"face_laughing: {faceLaughingString}\n"
                + $"head_position: {headPositionString}\n"
                + $"head_rotation: {headRotationString}\n"
                + $"left_hand_position: {leftHandPositionString}\n"
                + $"left_hand_rotation: {leftHandRotationString}\n"
                + $"right_hand_position: {rightHandPositionString}\n"
                + $"right_hand_rotation: {rightHandRotationString}\n"
                + $"\n"
                + $"- end ViSCARecorder.Recorder7.Update\n"
            ;

            debugOutTMP.text = debugOutString;
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

        private void StartFaceObjects()
        {
            faceLeftEyeRenderer = faceLeftEyeObject.GetComponent<SkinnedMeshRenderer>();
            faceRightEyeRenderer = faceRightEyeObject.GetComponent<SkinnedMeshRenderer>();
            faceHeadRenderer = faceHeadObject.GetComponent<SkinnedMeshRenderer>();
            faceTeethRenderer = faceTeethObject.GetComponent<SkinnedMeshRenderer>();
            Mesh leftEyeMesh = faceLeftEyeRenderer.sharedMesh;
            Mesh rightEyeMesh = faceRightEyeRenderer.sharedMesh;
            Mesh headMesh = faceHeadRenderer.sharedMesh;
            Mesh teethMesh = faceTeethRenderer.sharedMesh;
            __.FindShapeIndexes(__.faceLeftEyeShapeNames, leftEyeMesh, out faceLeftEyeShapeIndexes);
            __.FindShapeIndexes(__.faceRightEyeShapeNames, rightEyeMesh, out faceRightEyeShapeIndexes);
            __.FindShapeIndexes(__.faceShapeNames, headMesh, out faceHeadShapeIndexes);
            __.FindShapeIndexes(__.faceTeethShapeNames, teethMesh, out faceTeethShapeIndexes);
        }

        private void UpdateFaceObjects()
        {
            UpdateRendererShapeWeights(faceLeftEyeShapeIndexes, __.faceLeftEyeShapeNames, faceLeftEyeRenderer);
            UpdateRendererShapeWeights(faceRightEyeShapeIndexes, __.faceRightEyeShapeNames, faceRightEyeRenderer);
            UpdateRendererShapeWeights(faceHeadShapeIndexes, __.faceShapeNames, faceHeadRenderer);
            UpdateRendererShapeWeights(faceTeethShapeIndexes, __.faceTeethShapeNames, faceTeethRenderer);
        }

        private void UpdateDevice(in InputDeviceCharacteristics characteristics, out InputDevice? device)
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

        private void UpdateDevicePosition(in InputDevice? device, out Vector3 position)
        {
            Vector3 result = Vector3.zero;
            
            if (device is InputDevice device_)
            {
                device_.TryGetFeatureValue(CommonUsages.devicePosition, out result);
            }

            position = result;
        }

        private void UpdateDeviceRotation(in InputDevice? device, out Vector3 rotation)
        {
            Quaternion resultQuaternion = Quaternion.identity;
            
            if (device is InputDevice device_)
            {
                device_.TryGetFeatureValue(CommonUsages.deviceRotation, out resultQuaternion);
            }

            Vector3 result = resultQuaternion.eulerAngles;
            __.NormalizeEulerAngles(ref result);
            rotation = result;
        }

        private void UpdateController(in PXR_Input.Controller controller, out Vector3 position, out Vector3 rotation)
        {
            position = PXR_Input.GetControllerPredictPosition(controller, 0.0);
            Quaternion rotationQuaternion = PXR_Input.GetControllerPredictRotation(controller, 0.0);
            rotation = rotationQuaternion.eulerAngles;
            __.NormalizeEulerAngles(ref rotation);
        }

        private void UpdateRendererShapeWeights(
            in List<int> shapeIndexes,
            in List<string> shapeNames,
            SkinnedMeshRenderer renderer
        ) {
            for (int index = 0; index < shapeIndexes.Count; index += 1)
            {
                int shapeIndex = shapeIndexes[index];
                string shapeName = shapeNames[index];
                Dictionary<string, float?> shapeDict;

                if (faceMirrorEnabled)
                {
                    shapeDict = faceBlendShapeDictMirrored;
                }
                else
                {
                    shapeDict = faceBlendShapeDict;
                }

                float shapeWeight = shapeDict[shapeName] ?? 0.0f;
                
                if (shapeIndex >= 0)
                {
                    float factoredShapeWeight = faceBlendShapeMultiplier * shapeWeight;
                    renderer.SetBlendShapeWeight(shapeIndex, factoredShapeWeight);
                }
            }
        }
    } // end class

    namespace Recorder7NameSpace
    {
        public enum BlendShapes
        {
            eyeLookDownLeft = 0,
            noseSneerLeft = 1,
            eyeLookInLeft = 2,
            browInnerUp = 3,
            browDownRight = 4,
            mouthClose = 5,
            mouthLowerDownRight = 6,
            jawOpen = 7,
            mouthUpperUpRight = 8,
            mouthShrugUpper = 9,
            mouthFunnel = 10,
            eyeLookInRight = 11,
            eyeLookDownRight = 12,
            noseSneerRight = 13,
            mouthRollUpper = 14,
            jawRight = 15,
            browDownLeft = 16,
            mouthShrugLower = 17,
            mouthRollLower = 18,
            mouthSmileLeft = 19,
            mouthPressLeft = 20,
            mouthSmileRight = 21,
            mouthPressRight = 22,
            mouthDimpleRight = 23,
            mouthLeft = 24,
            jawForward = 25,
            eyeSquintLeft = 26,
            mouthFrownLeft = 27,
            eyeBlinkLeft = 28,
            cheekSquintLeft = 29,
            browOuterUpLeft = 30,
            eyeLookUpLeft = 31,
            jawLeft = 32,
            mouthStretchLeft = 33,
            mouthPucker = 34,
            eyeLookUpRight = 35,
            browOuterUpRight = 36,
            cheekSquintRight = 37,
            eyeBlinkRight = 38,
            mouthUpperUpLeft = 39,
            mouthFrownRight = 40,
            eyeSquintRight = 41,
            mouthStretchRight = 42,
            cheekPuff = 43,
            eyeLookOutLeft = 44,
            eyeLookOutRight = 45,
            eyeWideRight = 46,
            eyeWideLeft = 47,
            mouthRight = 48,
            mouthDimpleLeft = 49,
            mouthLowerDownLeft = 50,
            tongueOut = 51,
            viseme_PP = 52,
            viseme_CH = 53,
            viseme_o = 54,
            viseme_O = 55,
            viseme_i = 56,
            viseme_I = 57,
            viseme_RR = 58,
            viseme_XX = 59,
            viseme_aa = 60,
            viseme_FF = 61,
            viseme_u = 62,
            viseme_U = 63,
            viseme_TH = 64,
            viseme_kk = 65,
            viseme_SS = 66,
            viseme_e = 67,
            viseme_DD = 68,
            viseme_E = 69,
            viseme_nn = 70,
            viseme_sil = 71
        }

        public enum AvatarLeftEyeShapes
        {
            eyeLookDownLeft = 0,
            eyeLookInLeft = 2,
            eyeLookUpLeft = 31,
            eyeLookOutLeft = 44
        }

        public enum AvatarRightEyeShapes
        {
            eyeLookInRight = 11,
            eyeLookDownRight = 12,
            eyeLookUpRight = 35,
            eyeLookOutRight = 45
        }

        public enum AvatarTeethShapes
        {
            tongueOut = 51
        }

        public enum BlendShapesMirrored
        {
            eyeLookDownRight = 0,
            noseSneerRight = 1,
            eyeLookInRight = 2,
            browInnerUp = 3,
            browDownLeft = 4,
            mouthClose = 5,
            mouthLowerDownLeft = 6,
            jawOpen = 7,
            mouthUpperUpLeft = 8,
            mouthShrugUpper = 9,
            mouthFunnel = 10,
            eyeLookInLeft = 11,
            eyeLookDownLeft = 12,
            noseSneerLeft = 13,
            mouthRollUpper = 14,
            jawLeft = 15,
            browDownRight = 16,
            mouthShrugLower = 17,
            mouthRollLower = 18,
            mouthSmileRight = 19,
            mouthPressRight = 20,
            mouthSmileLeft = 21,
            mouthPressLeft = 22,
            mouthDimpleLeft = 23,
            mouthRight = 24,
            jawForward = 25,
            eyeSquintRight = 26,
            mouthFrownRight = 27,
            eyeBlinkRight = 28,
            cheekSquintRight = 29,
            browOuterUpRight = 30,
            eyeLookUpRight = 31,
            jawRight = 32,
            mouthStretchRight = 33,
            mouthPucker = 34,
            eyeLookUpLeft = 35,
            browOuterUpLeft = 36,
            cheekSquintLeft = 37,
            eyeBlinkLeft = 38,
            mouthUpperUpRight = 39,
            mouthFrownLeft = 40,
            eyeSquintLeft = 41,
            mouthStretchLeft = 42,
            cheekPuff = 43,
            eyeLookOutRight = 44,
            eyeLookOutLeft = 45,
            eyeWideLeft = 46,
            eyeWideRight = 47,
            mouthLeft = 48,
            mouthDimpleRight = 49,
            mouthLowerDownRight = 50,
            tongueOut = 51,
            viseme_PP = 52,
            viseme_CH = 53,
            viseme_o = 54,
            viseme_O = 55,
            viseme_i = 56,
            viseme_I = 57,
            viseme_RR = 58,
            viseme_XX = 59,
            viseme_aa = 60,
            viseme_FF = 61,
            viseme_u = 62,
            viseme_U = 63,
            viseme_TH = 64,
            viseme_kk = 65,
            viseme_SS = 66,
            viseme_e = 67,
            viseme_DD = 68,
            viseme_E = 69,
            viseme_nn = 70,
            viseme_sil = 71
        }

        public enum AvatarLeftEyeShapesMirrored
        {
            eyeLookInLeft = 11,
            eyeLookDownLeft = 12,
            eyeLookUpLeft = 35,
            eyeLookOutLeft = 45
        }

        public enum AvatarRightEyeShapesMirrored
        {
            eyeLookDownRight = 0,
            eyeLookInRight = 2,
            eyeLookUpRight = 31,
            eyeLookOutRight = 44
        }

        public enum AvatarTeethShapesMirrored
        {
            tongueOut = 51
        }

        public class __
        {
            public static int faceShapeArrayLength = 72;

            public static List<string> faceShapeNames = new List<string>
            {
                "eyeLookDownLeft",
                "noseSneerLeft",
                "eyeLookInLeft",
                "browInnerUp",
                "browDownRight",
                "mouthClose",
                "mouthLowerDownRight",
                "jawOpen",
                "mouthUpperUpRight",
                "mouthShrugUpper",
                "mouthFunnel",
                "eyeLookInRight",
                "eyeLookDownRight",
                "noseSneerRight",
                "mouthRollUpper",
                "jawRight",
                "browDownLeft",
                "mouthShrugLower",
                "mouthRollLower",
                "mouthSmileLeft",
                "mouthPressLeft",
                "mouthSmileRight",
                "mouthPressRight",
                "mouthDimpleRight",
                "mouthLeft",
                "jawForward",
                "eyeSquintLeft",
                "mouthFrownLeft",
                "eyeBlinkLeft",
                "cheekSquintLeft",
                "browOuterUpLeft",
                "eyeLookUpLeft",
                "jawLeft",
                "mouthStretchLeft",
                "mouthPucker",
                "eyeLookUpRight",
                "browOuterUpRight",
                "cheekSquintRight",
                "eyeBlinkRight",
                "mouthUpperUpLeft",
                "mouthFrownRight",
                "eyeSquintRight",
                "mouthStretchRight",
                "cheekPuff",
                "eyeLookOutLeft",
                "eyeLookOutRight",
                "eyeWideRight",
                "eyeWideLeft",
                "mouthRight",
                "mouthDimpleLeft",
                "mouthLowerDownLeft",
                "tongueOut",
                "viseme_PP",
                "viseme_CH",
                "viseme_o",
                "viseme_O",
                "viseme_i",
                "viseme_I",
                "viseme_RR",
                "viseme_XX",
                "viseme_aa",
                "viseme_FF",
                "viseme_u",
                "viseme_U",
                "viseme_TH",
                "viseme_kk",
                "viseme_SS",
                "viseme_e",
                "viseme_DD",
                "viseme_E",
                "viseme_nn",
                "viseme_sil"
            };

            public static List<string> faceLeftEyeShapeNames = new List<string>
            {
                "eyeLookDownLeft",
                "eyeLookInLeft",
                "eyeLookUpLeft",
                "eyeLookOutLeft"
            };

            public static List<string> faceRightEyeShapeNames = new List<string>
            {
                "eyeLookInRight",
                "eyeLookDownRight",
                "eyeLookUpRight",
                "eyeLookOutRight"
            };

            public static List<string> faceTeethShapeNames = new List<string>
            {
                "tongueOut"
            };

            public static List<string> faceShapeNamesMirrored = new List<string>
            {
                "eyeLookDownRight",
                "noseSneerRight",
                "eyeLookInRight",
                "browInnerUp",
                "browDownLeft",
                "mouthClose",
                "mouthLowerDownLeft",
                "jawOpen",
                "mouthUpperUpLeft",
                "mouthShrugUpper",
                "mouthFunnel",
                "eyeLookInLeft",
                "eyeLookDownLeft",
                "noseSneerLeft",
                "mouthRollUpper",
                "jawLeft",
                "browDownRight",
                "mouthShrugLower",
                "mouthRollLower",
                "mouthSmileRight",
                "mouthPressRight",
                "mouthSmileLeft",
                "mouthPressLeft",
                "mouthDimpleLeft",
                "mouthRight",
                "jawForward",
                "eyeSquintRight",
                "mouthFrownRight",
                "eyeBlinkRight",
                "cheekSquintRight",
                "browOuterUpRight",
                "eyeLookUpRight",
                "jawRight",
                "mouthStretchRight",
                "mouthPucker",
                "eyeLookUpLeft",
                "browOuterUpLeft",
                "cheekSquintLeft",
                "eyeBlinkLeft",
                "mouthUpperUpRight",
                "mouthFrownLeft",
                "eyeSquintLeft",
                "mouthStretchLeft",
                "cheekPuff",
                "eyeLookOutRight",
                "eyeLookOutLeft",
                "eyeWideLeft",
                "eyeWideRight",
                "mouthLeft",
                "mouthDimpleRight",
                "mouthLowerDownRight",
                "tongueOut",
                "viseme_PP",
                "viseme_CH",
                "viseme_o",
                "viseme_O",
                "viseme_i",
                "viseme_I",
                "viseme_RR",
                "viseme_XX",
                "viseme_aa",
                "viseme_FF",
                "viseme_u",
                "viseme_U",
                "viseme_TH",
                "viseme_kk",
                "viseme_SS",
                "viseme_e",
                "viseme_DD",
                "viseme_E",
                "viseme_nn",
                "viseme_sil"
            };

            public static List<string> faceLeftEyeShapeNamesMirrored = new List<string>
            {
                "eyeLookInLeft",
                "eyeLookDownLeft",
                "eyeLookUpLeft",
                "eyeLookOutLeft"
            };

            public static List<string> faceRightEyeShapeNamesMirrored = new List<string>
            {
                "eyeLookDownRight",
                "eyeLookInRight",
                "eyeLookUpRight",
                "eyeLookOutRight"
            };

            public static List<string> faceTeethShapeNamesMirrored = new List<string>
            {
                "tongueOut"
            };

            public static void NormalizeEulerAngles(ref Vector3 eulerAngles)
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

            public static unsafe void FindShapeArray(in PxrFaceTrackingInfo info, out float[] array)
            {
                array = new float[faceShapeArrayLength];

                fixed (float* pointer = info.blendShapeWeight)
                {
                    for (int index = 0; index < array.Length; index += 1)
                    {
                        array[index] = *(pointer + index);
                    }
                }
            }

            public static void ShapeArrayToDict(
                in float[] array,
                out Dictionary<string, float?> dict,
                bool mirrorEnabled
            ) {
                dict = new Dictionary<string, float?>();
                List<string> shapeNames;

                if (mirrorEnabled)
                {
                    shapeNames = faceShapeNamesMirrored;
                }
                else
                {
                    shapeNames = faceShapeNames;
                }

                for (int index = 0; index < shapeNames.Count; index += 1)
                {
                    if (index < array.Length)
                    {
                        dict.Add(shapeNames[index], array[index]);
                    }
                    else
                    {
                        dict.Add(shapeNames[index], null);
                    }
                }
            }

            public static void ShapeDictToString(in Dictionary<string, float?> dict, out string string_)
            {
                List<string> elementStringList = new List<string>();

                foreach (string key in dict.Keys)
                {
                    float? value = dict[key];
                    string elementString;

                    if (value is float value_)
                    {
                        elementString = $"{key}: {value_:f3}";
                    }
                    else
                    {
                        elementString = $"{key}: null";
                    }

                    elementStringList.Add(elementString);
                }

                string_ = $"{{{string.Join(", ", elementStringList)}}}";
            }

            public static void FindShapeIndexes(
                in List<string> shapeNames,
                in Mesh sharedMesh,
                out List<int> shapeIndexes
            ) {
                shapeIndexes = new List<int>();

                for (int index = 0; index < shapeNames.Count; index += 1)
                {
                    string shapeName = shapeNames[index];
                    int shapeIndex = sharedMesh.GetBlendShapeIndex(shapeName);
                    shapeIndexes.Add(shapeIndex);
                }
            }

            public static void SwapValues<T>(ref T value1, ref T value2)
            {
                T temp = value1;
                value1 = value2;
                value2 = temp;
            }
        } // end class
    } // end namespace
} // end namespace

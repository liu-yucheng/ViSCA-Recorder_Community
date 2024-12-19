// Copyright (C) 2024 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System;
using System.Collections;
using System.Collections.Generic;

using TMPro;

using Unity.XR.CoreUtils.Collections;
using Unity.XR.PXR;

using UnityEngine;
using UnityEngine.XR;

using ViSCARecorder;
using ViSCARecorder.Recorder8NameSpace;

namespace ViSCARecorder
{
    public class Recorder8 : MonoBehaviour
    {
        public GameObject debugOutObject;
        public GameObject xrOriginObject;
        public GameObject mainCameraObject;
        public GameObject eyeGazeObject;
        public GameObject leftEyeOpennessObject;
        public GameObject rightEyeOpennessObject;
        public GameObject faceLeftEyeObject;
        public GameObject faceRightEyeObject;
        public GameObject faceHeadObject;
        public GameObject faceTeethObject;
        public Record record;

        private Vector3 eyeGazePosition;
        private Vector3 eyeGazeRotation;
        private Dictionary<string, float> faceBlendShapeDict;
        private Dictionary<string, float> faceBlendShapeDictMirrored;
        private Record previousRecord;

        private float sceneStartTimeSeconds;
        private float previousTimeSeconds;
        private Camera mainCamera;
        private InputDevice? headsetDevice;
        private InputDevice? leftControllerDevice;
        private InputDevice? rightControllerDevice;
        private TextMeshProUGUI debugOutTMP;
        private Vector3 leftEyeOpennessOriginalScale;
        private Vector3 rightEyeOpennessOriginalScale;
        private float leftEyeOpennessScaleYMin = 0.25f;
        private float leftEyeOpennessScaleYMax = 1.0f;
        private float rightEyeOpennessScaleYMin = 0.25f;
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
        private float faceBlendShapeMultiplier = 1.5f;

        void Start()
        {
            Application.runInBackground = true;
            StartTiming();
            StartEyeGazeLegacy();
            StartFaceLegacy();
            StartTrackedDevices();
            StartControllers();
            StartDebugOut();
            StartEyeGazeObjects();
            StartFaceObjects();
            StartRecording();
        }

        void FixedUpdate()
        {
            FixedUpdateTiming();
            FixedUpdateEyeGazeLegacy();
            FixedUpdateFaceLegacy();
            FixedUpdateTrackedDevices();
            FixedUpdateControllers();
            FixedUpdateDebugOut();
            FixedUpdateEyeGazeObjects();
            FixedUpdateFaceObjects();
            FixedUpdateRecording();
        }

        void OnDestroy()
        {
            DestoryFaceLegacy();
            DestoryRecording();
        }

        private void StartTiming()
        {
            float timeSeconds = Time.time;
            sceneStartTimeSeconds = timeSeconds;
            previousTimeSeconds = timeSeconds;
        }

        private void FixedUpdateTiming()
        {
            float timeSeconds = Time.time;
            DateTime now  = DateTime.Now;
            string dateTimeCustom = $"{now:yyyyMMdd}-{now:HHmmss}-{now:ffffff}";
            TimeSpan baseUTCOffset = TimeZoneInfo.Local.BaseUtcOffset;

            if (baseUTCOffset > TimeSpan.Zero)
            {
                dateTimeCustom += $"-utc+{baseUTCOffset:hhmm}";
            }
            else
            {
                dateTimeCustom += $"-utc{baseUTCOffset:hhmm}";
            }

            string dateTime = $"{now:o}";
            long unixMS = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            float gameTimeSeconds = timeSeconds - sceneStartTimeSeconds;
            float deltaTimeSeconds = timeSeconds - previousTimeSeconds;
            previousTimeSeconds = timeSeconds;

            record.time_stamp.date_time_custom = dateTimeCustom;
            record.time_stamp.date_time = dateTime;
            record.time_stamp.unix_ms = unixMS;
            record.time_stamp.game_time_seconds = gameTimeSeconds;
            record.time_stamp.delta_time_seconds = deltaTimeSeconds;
        }

        private void StartEyeGazeLegacy()
        {
            mainCamera = mainCameraObject.GetComponent<Camera>();
        }

        private void FixedUpdateEyeGazeLegacy()
        {
            Matrix4x4 xrOriginWorldMatrix = xrOriginObject.transform.localToWorldMatrix;
            PXR_EyeTracking.GetHeadPosMatrix(out Matrix4x4 headMatrix);
            PXR_EyeTracking.GetCombineEyeGazePoint(out Vector3 eyeGazePointLocal);
            PXR_EyeTracking.GetCombineEyeGazeVector(out Vector3 eyeGazeVectorLocal);
            Vector3 eyeGazePointWorld = eyeGazePointLocal;
            eyeGazePointWorld = headMatrix.MultiplyPoint(eyeGazePointWorld);
            eyeGazePointWorld = xrOriginWorldMatrix.MultiplyPoint(eyeGazePointWorld);
            Vector3 eyeGazeVectorWorld = eyeGazeVectorLocal;
            eyeGazeVectorWorld = headMatrix.MultiplyVector(eyeGazeVectorWorld);
            eyeGazeVectorWorld = xrOriginWorldMatrix.MultiplyVector(eyeGazeVectorWorld);
            Vector3 eyeGazePosition = eyeGazePointWorld;
            Vector3 eyeGazeRotation = Quaternion.LookRotation(eyeGazeVectorWorld, Vector3.up).eulerAngles;
            __.NormalizeEulerAngles(eyeGazeRotation, out eyeGazeRotation);
            Vector3 eyeGazeViewportPosition = mainCamera.WorldToViewportPoint(eyeGazePosition);
            PXR_EyeTracking.GetLeftEyeGazeOpenness(out float leftEyeOpenness);
            PXR_EyeTracking.GetRightEyeGazeOpenness(out float rightEyeOpenness);

            this.eyeGazePosition = eyeGazePosition;
            this.eyeGazeRotation = eyeGazeRotation;

            Matrix4x4 xrOriginLocalMatrix = xrOriginObject.transform.worldToLocalMatrix;
            Vector3 eyeGazePointHeadset = xrOriginLocalMatrix.MultiplyPoint(eyeGazePointWorld);
            Vector3 eyeGazeVectorHeadset = xrOriginLocalMatrix.MultiplyVector(eyeGazeVectorWorld);
            eyeGazePosition = eyeGazePointHeadset;
            eyeGazeRotation = Quaternion.LookRotation(eyeGazeVectorHeadset, Vector3.up).eulerAngles;
            __.NormalizeEulerAngles(eyeGazeRotation, out eyeGazeRotation);

            record.eye_gaze.spatial_pose.position = eyeGazePosition;
            record.eye_gaze.spatial_pose.rotation = eyeGazeRotation;
            record.eye_gaze.viewport_pose.position = eyeGazeViewportPosition;
            record.eye_gaze.left_eye_openness = leftEyeOpenness;
            record.eye_gaze.right_eye_openness = rightEyeOpenness;
        }

        private void StartFaceLegacy()
        {
            PXR_System.EnableLipSync(true);
            PXR_System.EnableFaceTracking(true);
        }

        private unsafe void FixedUpdateFaceLegacy()
        {
            PxrFaceTrackingInfo info = new();
            PXR_System.GetFaceTrackingData(0, GetDataType.PXR_GET_FACELIP_DATA, ref info);
            __.FindShapeArray(info, out float[] blendShapeArray);
            __.ShapeArrayToDict(blendShapeArray, false, out Dictionary<string, float> faceBlendShapeDict);
            __.ShapeArrayToDict(blendShapeArray, true, out Dictionary<string, float> faceBlendShapeDictMirrored);
            __.ShapeArrayToList(blendShapeArray, out List<float> faceBlendShapeList);
            float faceLaughing = info.laughingProb;

            this.faceBlendShapeDict = faceBlendShapeDict;
            this.faceBlendShapeDictMirrored = faceBlendShapeDictMirrored;

            record.face.blend_shape_dict = new SerializableDict<string, float>(faceBlendShapeDict);
            record.face.blend_shape_list = faceBlendShapeList;
            record.face.laughing = faceLaughing;
        }

        private void DestoryFaceLegacy()
        {
            PXR_System.EnableLipSync(false);
            PXR_System.EnableFaceTracking(false);
        }

        private void StartTrackedDevices()
        {
            FixedUpdateDevice(
                InputDeviceCharacteristics.HeadMounted
                    | InputDeviceCharacteristics.TrackedDevice,

                out headsetDevice
            );

            FixedUpdateDevice(
                InputDeviceCharacteristics.Left
                    | InputDeviceCharacteristics.HeldInHand
                    | InputDeviceCharacteristics.TrackedDevice,

                out leftControllerDevice
            );

            FixedUpdateDevice(
                InputDeviceCharacteristics.Right
                    | InputDeviceCharacteristics.HeldInHand
                    | InputDeviceCharacteristics.TrackedDevice,

                out rightControllerDevice
            );
        }

        private void FixedUpdateTrackedDevices()
        {
            FixedUpdateDevicePosition(headsetDevice, out Vector3 headsetPosition);
            FixedUpdateDeviceRotation(headsetDevice, out Vector3 headsetRotation);
            FixedUpdateDevicePosition(leftControllerDevice, out Vector3 leftControllerPosition);
            FixedUpdateDeviceRotation(leftControllerDevice, out Vector3 leftControllerRotation);
            FixedUpdateDevicePosition(rightControllerDevice, out Vector3 rightControllerPosition);
            FixedUpdateDeviceRotation(rightControllerDevice, out Vector3 rightControllerRotation);

            Matrix4x4 xrOriginLocalMatrix = xrOriginObject.transform.worldToLocalMatrix;
            Quaternion xrOriginLocalQuaternion = xrOriginLocalMatrix.rotation;
            
            headsetPosition = xrOriginLocalMatrix.MultiplyPoint(headsetPosition);
            Quaternion headsetQuaternion = Quaternion.Euler(headsetRotation);
            headsetQuaternion *= xrOriginLocalQuaternion;
            headsetRotation = headsetQuaternion.eulerAngles;
            __.NormalizeEulerAngles(headsetRotation, out headsetRotation);

            leftControllerPosition = xrOriginLocalMatrix.MultiplyPoint(leftControllerPosition);
            Quaternion leftControllerQuaternion = Quaternion.Euler(leftControllerRotation);
            leftControllerQuaternion *= xrOriginLocalQuaternion;
            leftControllerRotation = leftControllerQuaternion.eulerAngles;
            __.NormalizeEulerAngles(leftControllerRotation, out leftControllerRotation);

            rightControllerPosition = xrOriginLocalMatrix.MultiplyPoint(rightControllerPosition);
            Quaternion rightControllerQuaternion = Quaternion.Euler(rightControllerRotation);
            rightControllerQuaternion *= xrOriginLocalQuaternion;
            rightControllerRotation = rightControllerQuaternion.eulerAngles;
            __.NormalizeEulerAngles(rightControllerRotation, out rightControllerRotation);

            record.headset_spatial_pose.position = headsetPosition;
            record.headset_spatial_pose.rotation = headsetRotation;
            record.left_controller_spatial_pose.position = leftControllerPosition;
            record.left_controller_spatial_pose.rotation = leftControllerRotation;
            record.right_controller_spatial_pose.position = rightControllerPosition;
            record.right_controller_spatial_pose.rotation = rightControllerRotation;
        }

        private void StartControllers()
        {
            // Do nothing.
        }

        private void FixedUpdateControllers()
        {
            FixedUpdateController(
                PXR_Input.Controller.LeftController,
                out Vector3 leftControllerPosition,
                out Vector3 leftControllerRotation
            );

            FixedUpdateController(
                PXR_Input.Controller.RightController,
                out Vector3 rightControllerPosition,
                out Vector3 rightControllerRotation
            );

            Matrix4x4 xrOriginLocalMatrix = xrOriginObject.transform.worldToLocalMatrix;
            Quaternion xrOriginLocalQuaternion = xrOriginLocalMatrix.rotation;

            leftControllerPosition = xrOriginLocalMatrix.MultiplyPoint(leftControllerPosition);
            Quaternion leftControllerQuaternion = Quaternion.Euler(leftControllerRotation);
            leftControllerQuaternion *= xrOriginLocalQuaternion;
            leftControllerRotation = leftControllerQuaternion.eulerAngles;
            __.NormalizeEulerAngles(leftControllerRotation, out leftControllerRotation);

            rightControllerPosition = xrOriginLocalMatrix.MultiplyPoint(rightControllerPosition);
            Quaternion rightControllerQuaternion = Quaternion.Euler(rightControllerRotation);
            rightControllerQuaternion *= xrOriginLocalQuaternion;
            rightControllerRotation = rightControllerQuaternion.eulerAngles;
            __.NormalizeEulerAngles(rightControllerRotation, out rightControllerRotation);

            record.left_controller_spatial_pose.position = leftControllerPosition;
            record.left_controller_spatial_pose.rotation = leftControllerRotation;
            record.right_controller_spatial_pose.position = rightControllerPosition;
            record.right_controller_spatial_pose.rotation = rightControllerRotation;
        }

        private void StartDebugOut()
        {
            debugOutTMP = debugOutObject.GetComponent<TextMeshProUGUI>();

            string debugOutString =
                "- begin ViSCARecorder.Recorder8.Start\n"
                + "\n"
                + "record: {...}\n"
                + "\n"
                + "- end ViSCARecorder.Recorder8.Start\n"
            ;

            debugOutTMP.text = debugOutString;
        }

        private void FixedUpdateDebugOut()
        {
            string recordString = JsonUtility.ToJson(record, true);

            string debugOutString =
                $"- begin ViSCARecorder.Recorder8.FixedUpdate\n"
                + $"\n"
                + $"{recordString}\n"
                + $"\n"
                + $"- end ViSCARecorder.Recorder8.FixedUpdate\n"
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

        private void FixedUpdateEyeGazeObjects()
        {
            float leftEyeOpenness = record.eye_gaze.left_eye_openness;
            float rightEyeOpenness = record.eye_gaze.right_eye_openness;

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

        private void FixedUpdateFaceObjects()
        {
            FixedUpdateRendererShapeWeights(faceLeftEyeShapeIndexes, __.faceLeftEyeShapeNames, faceLeftEyeRenderer);
            FixedUpdateRendererShapeWeights(faceRightEyeShapeIndexes, __.faceRightEyeShapeNames, faceRightEyeRenderer);
            FixedUpdateRendererShapeWeights(faceHeadShapeIndexes, __.faceShapeNames, faceHeadRenderer);
            FixedUpdateRendererShapeWeights(faceTeethShapeIndexes, __.faceTeethShapeNames, faceTeethRenderer);
        }

        private void StartRecording()
        {
            record = new();
            previousRecord = new();
        }

        private void FixedUpdateRecording()
        {
            // Do nothing.
        }

        private void DestoryRecording()
        {
            // Do nothing.
        }

        private void FixedUpdateDevice(in InputDeviceCharacteristics characteristics, out InputDevice? device)
        {
            List<InputDevice> devices = new();
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

        private void FixedUpdateDevicePosition(in InputDevice? device, out Vector3 position)
        {
            Vector3 result = Vector3.zero;

            if (device is InputDevice device_)
            {
                device_.TryGetFeatureValue(CommonUsages.devicePosition, out result);
            }

            position = result;
        }

        private void FixedUpdateDeviceRotation(in InputDevice? device, out Vector3 rotation)
        {
            Quaternion resultQuaternion = Quaternion.identity;

            if (device is InputDevice device_)
            {
                device_.TryGetFeatureValue(CommonUsages.deviceRotation, out resultQuaternion);
            }

            Vector3 result = resultQuaternion.eulerAngles;
            __.NormalizeEulerAngles(result, out result);
            rotation = result;
        }

        private void FixedUpdateController(in PXR_Input.Controller controller, out Vector3 position, out Vector3 rotation)
        {
            position = PXR_Input.GetControllerPredictPosition(controller, 0.0);
            Quaternion rotationQuaternion = PXR_Input.GetControllerPredictRotation(controller, 0.0);
            rotation = rotationQuaternion.eulerAngles;
            __.NormalizeEulerAngles(rotation, out rotation);
        }

        private void FixedUpdateRendererShapeWeights(
            in List<int> shapeIndexes,
            in List<string> shapeNames,
            SkinnedMeshRenderer renderer
        )
        {
            for (int index = 0; index < shapeIndexes.Count; index += 1)
            {
                int shapeIndex = shapeIndexes[index];
                string shapeName = shapeNames[index];
                Dictionary<string, float> shapeDict;

                if (faceMirrorEnabled)
                {
                    shapeDict = faceBlendShapeDictMirrored;
                }
                else
                {
                    shapeDict = faceBlendShapeDict;
                }

                float shapeWeight = shapeDict[shapeName];

                if (shapeIndex >= 0)
                {
                    float factoredShapeWeight = faceBlendShapeMultiplier * shapeWeight;
                    renderer.SetBlendShapeWeight(shapeIndex, factoredShapeWeight);
                }
            }
        }
    } // end class
} // end namespace

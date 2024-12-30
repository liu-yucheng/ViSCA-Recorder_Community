// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using TMPro;

using Unity.Jobs;
using Unity.XR.CoreUtils.Collections;
using Unity.XR.PXR;

using UnityEngine;
using UnityEngine.XR;

using ViSCARecorder;
using ViSCARecorder.Recorder11NameSpace;

namespace ViSCARecorder
{
    public class Recorder11 : MonoBehaviour
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

        // Timing.
        private DateTime now;
        private TimeSpan utcOffset;
        private float sceneStartTimeSeconds;
        private float previousTimeSeconds;
        private float deltaTimeSeconds;

        // Eye gaze legacy.
        private Camera mainCamera;
        private Vector3 eyeGazePosition;
        private Vector3 eyeGazeRotation;

        // Face legacy.
        private Dictionary<string, float> faceBlendShapeDict;
        private Dictionary<string, float> faceBlendShapeDictMirrored;

        // Tracked devices.
        private InputDevice? headsetDevice;
        private InputDevice? leftControllerDevice;
        private InputDevice? rightControllerDevice;

        // Debug out.
        private TextMeshProUGUI debugOutTMP;

        // Eye gaze objects.
        private Vector3 leftEyeOpennessOriginalScale;
        private Vector3 rightEyeOpennessOriginalScale;
        private float leftEyeOpennessScaleYMin = 0.25f;
        private float leftEyeOpennessScaleYMax = 1.0f;
        private float rightEyeOpennessScaleYMin = 0.25f;
        private float rightyeOpennessScaleYMax = 1.0f;

        // Face objects.
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

        // Recording.
        private Record previousRecord;
        private Records records;
        private float recordsSavingIntervalSeconds = 1f;
        private float recordsSavingCountdown;
        private float recordsFileSwitchingIntervalSeconds = 60f;
        private float recordsFileSwitchingCountdown;
        private string downloadFolderName = "/storage/emulated/0/Download";
        private string recordsFolderName;
        private string recordsFileName;
        private List<Thread> threads;

        void Start()
        {
            Application.runInBackground = true;
            Start_Timing();
            Start_EyeGazeLegacy();
            Start_FaceLegacy();
            Start_TrackedDevices();
            Start_Controllers();
            Start_Actions();
            Start_DebugOut();
            Start_EyeGazeObjects();
            Start_FaceObjects();
            Start_Recording();
        }

        void FixedUpdate()
        {
            FixedUpdate_Timing();
            FixedUpdate_EyeGazeLegacy();
            FixedUpdate_FaceLegacy();
            FixedUpdate_TrackedDevices();
            FixedUpdate_Controllers();
            FixedUpdate_Actions();
            FixedUpdate_Recording();
            FixedUpdate_DebugOut();
            FixedUpdate_EyeGazeObjects();
            FixedUpdate_FaceObjects();
            FixedUpdate_Recording2();
        }

        private void LateUpdate()
        {
            
        }

        void OnDestroy()
        {
            Destory_FaceLegacy();
            Destory_Recording();
        }

        private void Start_Timing()
        {
            float timeSeconds = Time.time;

            now = DateTime.Now;
            utcOffset = TimeZoneInfo.Local.BaseUtcOffset;
            sceneStartTimeSeconds = timeSeconds;
            previousTimeSeconds = timeSeconds;
            this.deltaTimeSeconds = Time.fixedDeltaTime;
        }

        private void FixedUpdate_Timing()
        {
            now = DateTime.Now;
            utcOffset = TimeZoneInfo.Local.BaseUtcOffset;
            float timeSeconds = Time.time;
            string dateTimeCustom = $"{now:yyyyMMdd}-{now:HHmmss}-{now:ffffff}-utc{utcOffset:hhmm}";
            string dateTime = $"{now:o}";
            long unixMS = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            float gameTimeSeconds = timeSeconds - sceneStartTimeSeconds;
            deltaTimeSeconds = timeSeconds - previousTimeSeconds;
            previousTimeSeconds = timeSeconds;

            record.timestamp.date_time_custom = dateTimeCustom;
            record.timestamp.date_time = dateTime;
            record.timestamp.unix_ms = unixMS;
            record.timestamp.game_time_seconds = gameTimeSeconds;
            record.timestamp.delta_time_seconds = deltaTimeSeconds;
        }

        private void Start_EyeGazeLegacy()
        {
            mainCamera = mainCameraObject.GetComponent<Camera>();
        }

        private void FixedUpdate_EyeGazeLegacy()
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
            __.NormalizeEulerAngles(ref eyeGazeRotation);
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
            __.NormalizeEulerAngles(ref eyeGazeRotation);

            record.eye_gaze.spatial_pose.position = eyeGazePosition;
            record.eye_gaze.spatial_pose.rotation = eyeGazeRotation;
            record.eye_gaze.viewport_pose.position = eyeGazeViewportPosition;
            record.eye_gaze.left_eye_openness = leftEyeOpenness;
            record.eye_gaze.right_eye_openness = rightEyeOpenness;
        }

        private void Start_FaceLegacy()
        {
            PXR_System.EnableLipSync(true);
            PXR_System.EnableFaceTracking(true);
        }

        private unsafe void FixedUpdate_FaceLegacy()
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

        private void Destory_FaceLegacy()
        {
            PXR_System.EnableLipSync(false);
            PXR_System.EnableFaceTracking(false);
        }

        private void Start_TrackedDevices()
        {
            FixedUpdate_TrackedDevices_Device(
                InputDeviceCharacteristics.HeadMounted
                    | InputDeviceCharacteristics.TrackedDevice,

                out headsetDevice
            );

            FixedUpdate_TrackedDevices_Device(
                InputDeviceCharacteristics.Left
                    | InputDeviceCharacteristics.HeldInHand
                    | InputDeviceCharacteristics.TrackedDevice,

                out leftControllerDevice
            );

            FixedUpdate_TrackedDevices_Device(
                InputDeviceCharacteristics.Right
                    | InputDeviceCharacteristics.HeldInHand
                    | InputDeviceCharacteristics.TrackedDevice,

                out rightControllerDevice
            );
        }

        private void FixedUpdate_TrackedDevices()
        {
            FixedUpdate_TrackedDevices_Position(headsetDevice, out Vector3 headsetPosition);
            FixedUpdate_TrackedDevices_Rotation(headsetDevice, out Vector3 headsetRotation);
            FixedUpdate_TrackedDevices_Position(leftControllerDevice, out Vector3 leftControllerPosition);
            FixedUpdate_TrackedDevices_Rotation(leftControllerDevice, out Vector3 leftControllerRotation);
            FixedUpdate_TrackedDevices_Position(rightControllerDevice, out Vector3 rightControllerPosition);
            FixedUpdate_TrackedDevices_Rotation(rightControllerDevice, out Vector3 rightControllerRotation);

            Matrix4x4 xrOriginLocalMatrix = xrOriginObject.transform.worldToLocalMatrix;
            Quaternion xrOriginLocalQuat = xrOriginLocalMatrix.rotation;
            
            headsetPosition = xrOriginLocalMatrix.MultiplyPoint(headsetPosition);
            Quaternion headsetQuat = Quaternion.Euler(headsetRotation);
            headsetQuat *= xrOriginLocalQuat;
            headsetRotation = headsetQuat.eulerAngles;
            __.NormalizeEulerAngles(ref headsetRotation);

            leftControllerPosition = xrOriginLocalMatrix.MultiplyPoint(leftControllerPosition);
            Quaternion leftControllerQuat = Quaternion.Euler(leftControllerRotation);
            leftControllerQuat *= xrOriginLocalQuat;
            leftControllerRotation = leftControllerQuat.eulerAngles;
            __.NormalizeEulerAngles(ref leftControllerRotation);

            rightControllerPosition = xrOriginLocalMatrix.MultiplyPoint(rightControllerPosition);
            Quaternion rightControllerQuat = Quaternion.Euler(rightControllerRotation);
            rightControllerQuat *= xrOriginLocalQuat;
            rightControllerRotation = rightControllerQuat.eulerAngles;
            __.NormalizeEulerAngles(ref rightControllerRotation);

            record.headset_spatial_pose.position = headsetPosition;
            record.headset_spatial_pose.rotation = headsetRotation;
            record.left_controller_spatial_pose.position = leftControllerPosition;
            record.left_controller_spatial_pose.rotation = leftControllerRotation;
            record.right_controller_spatial_pose.position = rightControllerPosition;
            record.right_controller_spatial_pose.rotation = rightControllerRotation;
        }

        private void Start_Controllers()
        {
            // Do nothing.
        }

        private void FixedUpdate_Controllers()
        {
            FixedUpdate_Controllers_Controller(
                PXR_Input.Controller.LeftController,
                out Vector3 leftControllerPosition,
                out Vector3 leftControllerRotation
            );

            FixedUpdate_Controllers_Controller(
                PXR_Input.Controller.RightController,
                out Vector3 rightControllerPosition,
                out Vector3 rightControllerRotation
            );

            Matrix4x4 xrOriginLocalMatrix = xrOriginObject.transform.worldToLocalMatrix;
            Quaternion xrOriginLocalQuat = xrOriginLocalMatrix.rotation;

            leftControllerPosition = xrOriginLocalMatrix.MultiplyPoint(leftControllerPosition);
            Quaternion leftControllerQuat = Quaternion.Euler(leftControllerRotation);
            leftControllerQuat *= xrOriginLocalQuat;
            leftControllerRotation = leftControllerQuat.eulerAngles;
            __.NormalizeEulerAngles(ref leftControllerRotation);

            rightControllerPosition = xrOriginLocalMatrix.MultiplyPoint(rightControllerPosition);
            Quaternion rightControllerQuat = Quaternion.Euler(rightControllerRotation);
            rightControllerQuat *= xrOriginLocalQuat;
            rightControllerRotation = rightControllerQuat.eulerAngles;
            __.NormalizeEulerAngles(ref rightControllerRotation);

            record.left_controller_spatial_pose.position = leftControllerPosition;
            record.left_controller_spatial_pose.rotation = leftControllerRotation;
            record.right_controller_spatial_pose.position = rightControllerPosition;
            record.right_controller_spatial_pose.rotation = rightControllerRotation;
        }

        private void Start_Actions()
        {
            // Do nothing.
        }

        private void FixedUpdate_Actions()
        {
            FixedUpdate_Actions_Action(leftControllerDevice, CommonUsages.primary2DAxis, out Vector2 leftJoystickInput);
            FixedUpdate_Actions_Action(leftControllerDevice, CommonUsages.primaryTouch, out bool xTouched);
            FixedUpdate_Actions_Action(leftControllerDevice, CommonUsages.primaryButton, out bool xPressed);
            FixedUpdate_Actions_Action(leftControllerDevice, CommonUsages.secondaryTouch, out bool yTouched);
            FixedUpdate_Actions_Action(leftControllerDevice, CommonUsages.secondaryButton, out bool yPressed);

            FixedUpdate_Actions_Action(rightControllerDevice, CommonUsages.primary2DAxis, out Vector2 rightJoystickInput);
            FixedUpdate_Actions_Action(rightControllerDevice, CommonUsages.primaryTouch, out bool aTouched);
            FixedUpdate_Actions_Action(rightControllerDevice, CommonUsages.primaryButton, out bool aPressed);
            FixedUpdate_Actions_Action(rightControllerDevice, CommonUsages.secondaryTouch, out bool bTouched);
            FixedUpdate_Actions_Action(rightControllerDevice, CommonUsages.secondaryButton, out bool bPressed);

            FixedUpdate_Actions_Joysticks(leftJoystickInput, rightJoystickInput, out Vector2 activeInput);
            FixedUpdate_Actions_Button(xTouched, xPressed, out float xValue);
            FixedUpdate_Actions_Button(yTouched, yPressed, out float yValue);
            FixedUpdate_Actions_Button(aTouched, aPressed, out float aValue);
            FixedUpdate_Actions_Button(bTouched, bPressed, out float bValue);
            FixedUpdate_Actions_Sickness(aValue, bValue, xValue, yValue, out float sickness);
            
            record.game_play.locomotion.left_joystick_input = leftJoystickInput;
            record.game_play.locomotion.right_joystick_input= rightJoystickInput;
            record.game_play.locomotion.active_input.input_value = activeInput;
            record.sickness.button_input.a = aValue;
            record.sickness.button_input.b = bValue;
            record.sickness.button_input.x = xValue;
            record.sickness.button_input.y = yValue;
            record.sickness.reported = sickness;
            record.sickness.deduced = sickness;
        }

        private void Start_Recording()
        {
            record = new();
            previousRecord = new();
            records = new();
            recordsSavingCountdown = recordsSavingIntervalSeconds;
            recordsFileSwitchingCountdown = recordsFileSwitchingIntervalSeconds;
            recordsFolderName = Path.Combine(downloadFolderName, "liu_yucheng.visca_recorder");
            recordsFileName = $"visca-records-{now:yyyyMMdd}-{now:HHmmss}-{now:ffffff}-utc{utcOffset:hhmm}.json";
            __.ReplaceInvalidCharsWith("_", ref recordsFileName);
            threads = new();

            Directory.CreateDirectory(recordsFolderName);
            string recordsString = JsonUtility.ToJson(records);
            string recordsFilePath = Path.Combine(recordsFolderName, recordsFileName);
            File.WriteAllText(recordsFilePath, recordsString);
        }

        private void FixedUpdate_Recording()
        {
            record.ProcessWith(previousRecord);
            records.items.Add(record);
        }

        private void FixedUpdate_Recording2()
        {
            previousRecord = record;
            record = new(previousRecord);

            recordsSavingCountdown -= deltaTimeSeconds;

            if (recordsSavingCountdown < 0)
            {
                ThreadStart threadStart = new ThreadStart(FixedUpdate_Recording2_SaveRecording);
                Thread thread = new Thread(threadStart);
                thread.Start();
                threads.Add(thread);
                recordsSavingCountdown = recordsSavingIntervalSeconds;
            }

            recordsFileSwitchingCountdown -= deltaTimeSeconds;

            if (recordsFileSwitchingCountdown < 0)
            {
                ThreadStart threadStart = new ThreadStart(FixedUpdate_Recording2_SaveRecording);
                Thread thread = new Thread(threadStart);
                thread.Start();
                threads.Add(thread);

                records = new Records();
                recordsFileName = $"visca-records-{now:yyyyMMdd}-{now:HHmmss}-{now:ffffff}-utc{utcOffset:hhmm}.json";
                __.ReplaceInvalidCharsWith("_", ref recordsFileName);
                recordsFileSwitchingCountdown = recordsFileSwitchingIntervalSeconds;
            }

            List<Thread> oldThreads = threads;
            threads = new();

            foreach (Thread thread in oldThreads)
            {
                if (thread.ThreadState == ThreadState.Stopped)
                {
                    thread.Join();
                }
                else
                {
                    threads.Add(thread);
                }
            }
        }

        private void Destory_Recording()
        {
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
        }

        private void Start_DebugOut()
        {
            debugOutTMP = debugOutObject.GetComponent<TextMeshProUGUI>();

            string debugOutString =
                "- begin ViSCARecorder.Recorder11.Start\n"
                + "\n"
                + "record: {...}\n"
                + "\n"
                + "- end ViSCARecorder.Recorder11.Start\n"
            ;

            debugOutTMP.text = debugOutString;
        }

        private void FixedUpdate_DebugOut()
        {
            string recordString = JsonUtility.ToJson(record, true);

            string debugOutString =
                $"- begin ViSCARecorder.Recorder11.FixedUpdate\n"
                + $"\n"
                + $"{recordString}\n"
                + $"\n"
                + $"- end ViSCARecorder.Recorder11.FixedUpdate\n"
            ;

            debugOutTMP.text = debugOutString;
        }

        private void Start_EyeGazeObjects()
        {
            leftEyeOpennessOriginalScale = leftEyeOpennessObject.transform.localScale;
            rightEyeOpennessOriginalScale = rightEyeOpennessObject.transform.localScale;
            leftEyeOpennessScaleYMin *= leftEyeOpennessOriginalScale.y;
            leftEyeOpennessScaleYMax *= leftEyeOpennessOriginalScale.y;
            rightEyeOpennessScaleYMin *= rightEyeOpennessOriginalScale.y;
            rightyeOpennessScaleYMax *= rightEyeOpennessOriginalScale.y;
        }

        private void FixedUpdate_EyeGazeObjects()
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

        private void Start_FaceObjects()
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

        private void FixedUpdate_FaceObjects()
        {
            FixedUpdate_FaceObjects_ShapeWeights(faceLeftEyeShapeIndexes, __.faceLeftEyeShapeNames, faceLeftEyeRenderer);
            FixedUpdate_FaceObjects_ShapeWeights(faceRightEyeShapeIndexes, __.faceRightEyeShapeNames, faceRightEyeRenderer);
            FixedUpdate_FaceObjects_ShapeWeights(faceHeadShapeIndexes, __.faceShapeNames, faceHeadRenderer);
            FixedUpdate_FaceObjects_ShapeWeights(faceTeethShapeIndexes, __.faceTeethShapeNames, faceTeethRenderer);
        }

        private void FixedUpdate_TrackedDevices_Device(
            in InputDeviceCharacteristics characteristics,
            out InputDevice? device
        )
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

        private void FixedUpdate_TrackedDevices_Position(
            in InputDevice? device,
            out Vector3 position
        )
        {
            Vector3 result = Vector3.zero;

            if (device is InputDevice device_)
            {
                device_.TryGetFeatureValue(CommonUsages.devicePosition, out result);
            }

            position = result;
        }

        private void FixedUpdate_TrackedDevices_Rotation(
            in InputDevice? device,
            out Vector3 rotation
        )
        {
            Quaternion resultQuat = Quaternion.identity;

            if (device is InputDevice device_)
            {
                device_.TryGetFeatureValue(CommonUsages.deviceRotation, out resultQuat);
            }

            Vector3 result = resultQuat.eulerAngles;
            __.NormalizeEulerAngles(ref result);
            rotation = result;
        }

        private void FixedUpdate_Controllers_Controller(
            in PXR_Input.Controller controller,
            out Vector3 position,
            out Vector3 rotation
        )
        {
            position = PXR_Input.GetControllerPredictPosition(controller, 0.0);
            Quaternion quat = PXR_Input.GetControllerPredictRotation(controller, 0.0);
            rotation = quat.eulerAngles;
            __.NormalizeEulerAngles(ref rotation);
        }

        private void FixedUpdate_Actions_Action(
            in InputDevice? device,
            in InputFeatureUsage<bool> usage,
            out bool value
        )
        {
            bool result = false;

            if (device is InputDevice device_)
            {
                device_.TryGetFeatureValue(usage, out result);
            }

            value = result;
        }

        private void FixedUpdate_Actions_Action(
            in InputDevice? device,
            in InputFeatureUsage<Vector2> usage,
            out Vector2 value
        )
        {
            Vector2 result = Vector2.zero;

            if (device is InputDevice device_)
            {
                device_.TryGetFeatureValue(usage, out result);
            }

            value = result;
        }

        private void FixedUpdate_Actions_Button(
            in bool touched,
            in bool pressed,
            out float value
        )
        {
            if (!touched && !pressed)
            {
                value = 0f;
            }
            else if (touched && !pressed)
            {
                value = 0.1f;
            }
            else
            {
                value = 1f;
            }
        }

        private void FixedUpdate_Actions_Joysticks(
            in Vector2 left,
            in Vector2 right,
            out Vector2 value
        )
        {
            value = left + right;
            value.x = Mathf.Clamp(value.x, -1f, 1f);
            value.y = Mathf.Clamp(value.y, -1f, 1f);
        }

        private void FixedUpdate_Actions_Sickness(
            in float aValue,
            in float bValue,
            in float xValue,
            in float yValue,
            out float sickness
        )
        {
            sickness = 0f;
            float threshold = 1f - 1e-3f;

            if (
                aValue >= threshold ||
                bValue >= threshold ||
                xValue >= threshold ||
                yValue >= threshold
            )
            {
                sickness = 1f;
            }
        }

        private void FixedUpdate_FaceObjects_ShapeWeights(
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

        private void FixedUpdate_Recording2_SaveRecording()
        {
            string recordsString = JsonUtility.ToJson(records);
            string recordsPath = Path.Combine(recordsFolderName, recordsFileName);
            File.WriteAllText(recordsPath, recordsString);
        }
    } // end class
} // end namespace

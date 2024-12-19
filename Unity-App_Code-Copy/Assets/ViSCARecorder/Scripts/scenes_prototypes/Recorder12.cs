// Copyright (C) 2024 Yucheng Liu. Under the GNU AGPL 3.0 License.
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
using ViSCARecorder.Recorder12NameSpace;

namespace ViSCARecorder
{
    public class Recorder12 : MonoBehaviour
    {
        public GameObject debugOutObject;
        public GameObject xrOriginObject;
        public GameObject mainCameraObject;
        public GameObject eyeGazeObject;
        public GameObject eyeGaze_LeftOpennessObject;
        public GameObject eyeGaze_RightOpennessObject;
        public GameObject face_LeftEyeObject;
        public GameObject face_RightEyeObject;
        public GameObject face_HeadObject;
        public GameObject face_TeethObject;
        public Record record_Current;

        // Timing.
        private DateTime time_SceneStartTime;
        private DateTime time_Now;
        private TimeSpan time_UTCOffset;
        private float time_SceneStartTimeSeconds;
        private float time_PreviousTimeSeconds;
        private float time_DeltaTimeSeconds;

        // Eye gaze legacy.
        private Camera eyeGaze_MainCamera;
        private Vector3 eyeGaze_Position;
        private Vector3 eyeGaze_Rotation;

        // Face legacy.
        private Dictionary<string, float> face_BlendShapeDict;
        private Dictionary<string, float> face_BlendShapeDictMirrored;

        // Tracked devices.
        private InputDevice? trackedDevice_Headset;
        private InputDevice? trackedDevice_LeftController;
        private InputDevice? trackedDevice_rightController;

        // Debug out.
        private TextMeshProUGUI debugOutTMP;

        // Eye gaze objects.
        private Vector3 eyeGaze_LeftOpenness_OriginalScale;
        private Vector3 eyeGaze_RightOpenness_OriginalScale;
        private float eyeGaze_LeftOpenness_YScaleMin = 0.25f;
        private float eyeGaze_LeftOpenness_YScaleMax = 1f;
        private float eyeGaze_RightOpenness_YScaleMin = 0.25f;
        private float eyeGaze_RightOpenness_YScaleMax = 1f;

        // Face objects.
        private SkinnedMeshRenderer face_LeftEyeRenderer;
        private SkinnedMeshRenderer face_RightEyeRenderer;
        private SkinnedMeshRenderer face_HeadRenderer;
        private SkinnedMeshRenderer face_TeethRenderer;
        private List<int> face_LeftEyeShapeIndexes;
        private List<int> face_RightEyeShapeIndexes;
        private List<int> face_HeadShapeIndexes;
        private List<int> face_TeethShapeIndexes;
        private bool face_MirrorEnabled = true;
        private float face_BlendShapeMultiplier = 1.5f;

        // Recording.
        private Record record_Previous;
        private Records record_Records;
        private float record_SavingIntervalSeconds = 1f;
        private float record_SavingCountdown;
        private float record_FileSwitchIntervalSeconds = 60f;
        private float record_FileSwitchingCountdown;
        private string record_DownloadFolderName = "/storage/emulated/0/Download";
        private string record_FolderName;
        private string record_FileName;
        private List<Thread> record_Threads;

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

        void OnDestroy()
        {
            Destory_FaceLegacy();
            Destory_Recording();
        }

        private void Start_Timing()
        {
            float timeSeconds = Time.time;

            time_SceneStartTime = DateTime.Now;
            time_Now = DateTime.Now;
            time_UTCOffset = TimeZoneInfo.Local.BaseUtcOffset;
            time_SceneStartTimeSeconds = timeSeconds;
            time_PreviousTimeSeconds = timeSeconds;
            time_DeltaTimeSeconds = Time.fixedDeltaTime;
        }

        private void FixedUpdate_Timing()
        {
            time_Now = DateTime.Now;
            time_UTCOffset = TimeZoneInfo.Local.BaseUtcOffset;
            float timeSeconds = Time.time;
            string dateTimeCustom = $"{time_Now:yyyyMMdd}-{time_Now:HHmmss}-{time_Now:ffffff}-utc{time_UTCOffset:hhmm}";
            string dateTime = $"{time_Now:o}";
            long unixMS = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            TimeSpan gameTimeSpan = time_Now - time_SceneStartTime;
            string gameTimeCustom = $"{gameTimeSpan:ddd} days, {gameTimeSpan:hh}:{gameTimeSpan:mm}:{gameTimeSpan:ss}.{gameTimeSpan:ffffff}";
            string gameTime = $"{gameTimeSpan:c}";
            float gameTimeSeconds = timeSeconds - time_SceneStartTimeSeconds;
            time_DeltaTimeSeconds = timeSeconds - time_PreviousTimeSeconds;
            time_PreviousTimeSeconds = timeSeconds;

            record_Current.timestamp.date_time_custom = dateTimeCustom;
            record_Current.timestamp.date_time = dateTime;
            record_Current.timestamp.unix_ms = unixMS;
            record_Current.timestamp.game_time_custom = gameTimeCustom;
            record_Current.timestamp.game_time = gameTime;
            record_Current.timestamp.game_time_seconds = gameTimeSeconds;
            record_Current.timestamp.delta_time_seconds = time_DeltaTimeSeconds;
        }

        private void Start_EyeGazeLegacy()
        {
            eyeGaze_MainCamera = mainCameraObject.GetComponent<Camera>();
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
            Vector3 eyeGazeViewportPosition = eyeGaze_MainCamera.WorldToViewportPoint(eyeGazePosition);
            PXR_EyeTracking.GetLeftEyeGazeOpenness(out float leftEyeOpenness);
            PXR_EyeTracking.GetRightEyeGazeOpenness(out float rightEyeOpenness);

            eyeGaze_Position = eyeGazePosition;
            eyeGaze_Rotation = eyeGazeRotation;

            Matrix4x4 xrOriginLocalMatrix = xrOriginObject.transform.worldToLocalMatrix;
            Vector3 eyeGazePointXROrigin = xrOriginLocalMatrix.MultiplyPoint(eyeGazePointWorld);
            Vector3 eyeGazeVectorXROrigin = xrOriginLocalMatrix.MultiplyVector(eyeGazeVectorWorld);
            eyeGazePosition = eyeGazePointXROrigin;
            eyeGazeRotation = Quaternion.LookRotation(eyeGazeVectorXROrigin, Vector3.up).eulerAngles;
            __.NormalizeEulerAngles(ref eyeGazeRotation);

            record_Current.eye_gaze.spatial_pose.position = eyeGazePosition;
            record_Current.eye_gaze.spatial_pose.rotation = eyeGazeRotation;
            record_Current.eye_gaze.viewport_pose.position = eyeGazeViewportPosition;
            record_Current.eye_gaze.left_eye_openness = leftEyeOpenness;
            record_Current.eye_gaze.right_eye_openness = rightEyeOpenness;
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
            float faceLaughing = info.laughingProb;

            face_BlendShapeDict = faceBlendShapeDict;
            face_BlendShapeDictMirrored = faceBlendShapeDictMirrored;

            record_Current.face.blend_shape_dict = new SerializableDict<string, float>(faceBlendShapeDict);
            record_Current.face.laughing = faceLaughing;
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

                out trackedDevice_Headset
            );

            FixedUpdate_TrackedDevices_Device(
                InputDeviceCharacteristics.Left
                    | InputDeviceCharacteristics.HeldInHand
                    | InputDeviceCharacteristics.TrackedDevice,

                out trackedDevice_LeftController
            );

            FixedUpdate_TrackedDevices_Device(
                InputDeviceCharacteristics.Right
                    | InputDeviceCharacteristics.HeldInHand
                    | InputDeviceCharacteristics.TrackedDevice,

                out trackedDevice_rightController
            );
        }

        private void FixedUpdate_TrackedDevices()
        {
            FixedUpdate_TrackedDevices_Position(trackedDevice_Headset, out Vector3 headsetPosition);
            FixedUpdate_TrackedDevices_Rotation(trackedDevice_Headset, out Vector3 headsetRotation);
            FixedUpdate_TrackedDevices_Position(trackedDevice_LeftController, out Vector3 leftControllerPosition);
            FixedUpdate_TrackedDevices_Rotation(trackedDevice_LeftController, out Vector3 leftControllerRotation);
            FixedUpdate_TrackedDevices_Position(trackedDevice_rightController, out Vector3 rightControllerPosition);
            FixedUpdate_TrackedDevices_Rotation(trackedDevice_rightController, out Vector3 rightControllerRotation);

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

            record_Current.headset_spatial_pose.position = headsetPosition;
            record_Current.headset_spatial_pose.rotation = headsetRotation;
            record_Current.left_controller_spatial_pose.position = leftControllerPosition;
            record_Current.left_controller_spatial_pose.rotation = leftControllerRotation;
            record_Current.right_controller_spatial_pose.position = rightControllerPosition;
            record_Current.right_controller_spatial_pose.rotation = rightControllerRotation;
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

            record_Current.left_controller_spatial_pose.position = leftControllerPosition;
            record_Current.left_controller_spatial_pose.rotation = leftControllerRotation;
            record_Current.right_controller_spatial_pose.position = rightControllerPosition;
            record_Current.right_controller_spatial_pose.rotation = rightControllerRotation;
        }

        private void Start_Actions()
        {
            // Do nothing.
        }

        private void FixedUpdate_Actions()
        {
            FixedUpdate_Actions_Action(trackedDevice_LeftController, CommonUsages.primary2DAxis, out Vector2 leftJoystickInput);
            FixedUpdate_Actions_Action(trackedDevice_LeftController, CommonUsages.primaryTouch, out bool xTouched);
            FixedUpdate_Actions_Action(trackedDevice_LeftController, CommonUsages.primaryButton, out bool xPressed);
            FixedUpdate_Actions_Action(trackedDevice_LeftController, CommonUsages.secondaryTouch, out bool yTouched);
            FixedUpdate_Actions_Action(trackedDevice_LeftController, CommonUsages.secondaryButton, out bool yPressed);

            FixedUpdate_Actions_Action(trackedDevice_rightController, CommonUsages.primary2DAxis, out Vector2 rightJoystickInput);
            FixedUpdate_Actions_Action(trackedDevice_rightController, CommonUsages.primaryTouch, out bool aTouched);
            FixedUpdate_Actions_Action(trackedDevice_rightController, CommonUsages.primaryButton, out bool aPressed);
            FixedUpdate_Actions_Action(trackedDevice_rightController, CommonUsages.secondaryTouch, out bool bTouched);
            FixedUpdate_Actions_Action(trackedDevice_rightController, CommonUsages.secondaryButton, out bool bPressed);

            FixedUpdate_Actions_Joysticks(leftJoystickInput, rightJoystickInput, out Vector2 activeInput);
            FixedUpdate_Actions_Button(xTouched, xPressed, out float xValue);
            FixedUpdate_Actions_Button(yTouched, yPressed, out float yValue);
            FixedUpdate_Actions_Button(aTouched, aPressed, out float aValue);
            FixedUpdate_Actions_Button(bTouched, bPressed, out float bValue);
            FixedUpdate_Actions_Sickness(aValue, bValue, xValue, yValue, out float sickness);
            
            record_Current.game_play.locomotion.left_joystick_input = leftJoystickInput;
            record_Current.game_play.locomotion.right_joystick_input= rightJoystickInput;
            record_Current.game_play.locomotion.active_input.input_value = activeInput;
            record_Current.sickness.button_input.a = aValue;
            record_Current.sickness.button_input.b = bValue;
            record_Current.sickness.button_input.x = xValue;
            record_Current.sickness.button_input.y = yValue;
            record_Current.sickness.reported = sickness;
            record_Current.sickness.deduced = sickness;
        }

        private void Start_Recording()
        {
            record_Current = new();
            record_Previous = new();
            record_Records = new();
            record_SavingCountdown = record_SavingIntervalSeconds;
            record_FileSwitchingCountdown = record_FileSwitchIntervalSeconds;
            record_FolderName = Path.Combine(record_DownloadFolderName, "liu_yucheng.visca_recorder");
            record_FileName = $"visca-records-{time_Now:yyyyMMdd}-{time_Now:HHmmss}-{time_Now:ffffff}-utc{time_UTCOffset:hhmm}.json";
            __.ReplaceInvalidCharsWith("_", ref record_FileName);
            record_Threads = new();

            Directory.CreateDirectory(record_FolderName);
            string recordsString = JsonUtility.ToJson(record_Records);
            string recordsFilePath = Path.Combine(record_FolderName, record_FileName);
            File.WriteAllText(recordsFilePath, recordsString);
        }

        private void FixedUpdate_Recording()
        {
            record_Current.ProcessWith(record_Previous);
            record_Records.items.Add(record_Current);
        }

        private void FixedUpdate_Recording2()
        {
            record_Previous = record_Current;
            record_Current = new(record_Previous);

            record_SavingCountdown -= time_DeltaTimeSeconds;

            if (record_SavingCountdown < 0)
            {
                ThreadStart threadStart = new ThreadStart(FixedUpdate_Recording2_SaveRecording);
                Thread thread = new Thread(threadStart);
                thread.Start();
                record_Threads.Add(thread);
                record_SavingCountdown = record_SavingIntervalSeconds;
            }

            record_FileSwitchingCountdown -= time_DeltaTimeSeconds;

            if (record_FileSwitchingCountdown < 0)
            {
                ThreadStart threadStart = new ThreadStart(FixedUpdate_Recording2_SaveRecording);
                Thread thread = new Thread(threadStart);
                thread.Start();
                record_Threads.Add(thread);

                record_Records = new Records();
                record_FileName = $"visca-records-{time_Now:yyyyMMdd}-{time_Now:HHmmss}-{time_Now:ffffff}-utc{time_UTCOffset:hhmm}.json";
                __.ReplaceInvalidCharsWith("_", ref record_FileName);
                record_FileSwitchingCountdown = record_FileSwitchIntervalSeconds;
            }

            List<Thread> oldThreads = record_Threads;
            record_Threads = new();

            foreach (Thread thread in oldThreads)
            {
                if (thread.ThreadState == ThreadState.Stopped)
                {
                    thread.Join();
                }
                else
                {
                    record_Threads.Add(thread);
                }
            }
        }

        private void Destory_Recording()
        {
            foreach (Thread thread in record_Threads)
            {
                thread.Join();
            }
        }

        private void Start_DebugOut()
        {
            debugOutTMP = debugOutObject.GetComponent<TextMeshProUGUI>();

            string debugOutString =
                "- begin ViSCARecorder.Recorder12.Start\n"
                + "\n"
                + "record: {...}\n"
                + "\n"
                + "- end ViSCARecorder.Recorder12.Start\n"
            ;

            debugOutTMP.text = debugOutString;
        }

        private void FixedUpdate_DebugOut()
        {
            string recordString = JsonUtility.ToJson(record_Current, true);

            string debugOutString =
                $"- begin ViSCARecorder.Recorder12.FixedUpdate\n"
                + $"\n"
                + $"{recordString}\n"
                + $"\n"
                + $"- end ViSCARecorder.Recorder12.FixedUpdate\n"
            ;

            debugOutTMP.text = debugOutString;
        }

        private void Start_EyeGazeObjects()
        {
            eyeGaze_LeftOpenness_OriginalScale = eyeGaze_LeftOpennessObject.transform.localScale;
            eyeGaze_RightOpenness_OriginalScale = eyeGaze_RightOpennessObject.transform.localScale;
            eyeGaze_LeftOpenness_YScaleMin *= eyeGaze_LeftOpenness_OriginalScale.y;
            eyeGaze_LeftOpenness_YScaleMax *= eyeGaze_LeftOpenness_OriginalScale.y;
            eyeGaze_RightOpenness_YScaleMin *= eyeGaze_RightOpenness_OriginalScale.y;
            eyeGaze_RightOpenness_YScaleMax *= eyeGaze_RightOpenness_OriginalScale.y;
        }

        private void FixedUpdate_EyeGazeObjects()
        {
            float leftEyeOpenness = record_Current.eye_gaze.left_eye_openness;
            float rightEyeOpenness = record_Current.eye_gaze.right_eye_openness;

            eyeGazeObject.transform.position = eyeGaze_Position;
            eyeGazeObject.transform.rotation = Quaternion.Euler(eyeGaze_Rotation);
            Vector3 leftEyeOpennessScale = eyeGaze_LeftOpennessObject.transform.localScale;
            leftEyeOpennessScale.y = eyeGaze_LeftOpenness_YScaleMin + leftEyeOpenness * (eyeGaze_LeftOpenness_YScaleMax - eyeGaze_LeftOpenness_YScaleMin);
            eyeGaze_LeftOpennessObject.transform.localScale = leftEyeOpennessScale;
            Vector3 rightEyeOpennessScale = eyeGaze_RightOpennessObject.transform.localScale;
            rightEyeOpennessScale.y = eyeGaze_RightOpenness_YScaleMin + rightEyeOpenness * (eyeGaze_RightOpenness_YScaleMax - eyeGaze_RightOpenness_YScaleMin);
            eyeGaze_RightOpennessObject.transform.localScale = rightEyeOpennessScale;
        }

        private void Start_FaceObjects()
        {
            face_LeftEyeRenderer = face_LeftEyeObject.GetComponent<SkinnedMeshRenderer>();
            face_RightEyeRenderer = face_RightEyeObject.GetComponent<SkinnedMeshRenderer>();
            face_HeadRenderer = face_HeadObject.GetComponent<SkinnedMeshRenderer>();
            face_TeethRenderer = face_TeethObject.GetComponent<SkinnedMeshRenderer>();
            Mesh leftEyeMesh = face_LeftEyeRenderer.sharedMesh;
            Mesh rightEyeMesh = face_RightEyeRenderer.sharedMesh;
            Mesh headMesh = face_HeadRenderer.sharedMesh;
            Mesh teethMesh = face_TeethRenderer.sharedMesh;
            __.FindShapeIndexes(__.face_LeftEyeShapeNames, leftEyeMesh, out face_LeftEyeShapeIndexes);
            __.FindShapeIndexes(__.face_RightEyeShapeNames, rightEyeMesh, out face_RightEyeShapeIndexes);
            __.FindShapeIndexes(__.face_ShapeNames, headMesh, out face_HeadShapeIndexes);
            __.FindShapeIndexes(__.face_TeethShapeNames, teethMesh, out face_TeethShapeIndexes);
        }

        private void FixedUpdate_FaceObjects()
        {
            FixedUpdate_FaceObjects_ShapeWeights(face_LeftEyeShapeIndexes, __.face_LeftEyeShapeNames, face_LeftEyeRenderer);
            FixedUpdate_FaceObjects_ShapeWeights(face_RightEyeShapeIndexes, __.face_RightEyeShapeNames, face_RightEyeRenderer);
            FixedUpdate_FaceObjects_ShapeWeights(face_HeadShapeIndexes, __.face_ShapeNames, face_HeadRenderer);
            FixedUpdate_FaceObjects_ShapeWeights(face_TeethShapeIndexes, __.face_TeethShapeNames, face_TeethRenderer);
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
                value = 0.5f;
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
                aValue > threshold ||
                bValue > threshold ||
                xValue > threshold ||
                yValue > threshold
            )
            {
                sickness = 0.5f;
            }

            if (
                (aValue > threshold && bValue > threshold) ||
                (xValue > threshold && yValue > threshold)
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

                if (face_MirrorEnabled)
                {
                    shapeDict = face_BlendShapeDictMirrored;
                }
                else
                {
                    shapeDict = face_BlendShapeDict;
                }

                float shapeWeight = shapeDict[shapeName];

                if (shapeIndex >= 0)
                {
                    float factoredShapeWeight = face_BlendShapeMultiplier * shapeWeight;
                    renderer.SetBlendShapeWeight(shapeIndex, factoredShapeWeight);
                }
            }
        }

        private void FixedUpdate_Recording2_SaveRecording()
        {
            string recordsString = JsonUtility.ToJson(record_Records, true);
            string recordsPath = Path.Combine(record_FolderName, record_FileName);
            File.WriteAllText(recordsPath, recordsString);
        }
    } // end class
} // end namespace

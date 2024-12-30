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
using Unity.XR.PICO.TOBSupport;
using Unity.XR.PXR;

using UnityEngine;
using UnityEngine.XR;

using ViSCARecorder;
using ViSCARecorder.Recorder13NameSpace;

namespace ViSCARecorder
{
    public class Recorder13 : MonoBehaviour
    {
        // Shared (public).
        public GameObject shared_XROriginObject;
        public GameObject shared_MainCameraObject;

        // Eye gaze legacy (public).
        public GameObject eyeGaze_Object;
        public GameObject eyeGaze_LeftOpennessObject;
        public GameObject eyeGaze_RightOpennessObject;

        // Face legacy (public).
        public GameObject face_LeftEyeObject;
        public GameObject face_RightEyeObject;
        public GameObject face_HeadObject;
        public GameObject face_TeethObject;

        // Debug output (public).
        public GameObject debugOutput_Object;

        // Recording (public).
        public Record record_Current;

        // Capturing (public).
        public GameObject capture_RecordCameraObject;

        // Shared.
        private float shared_DeltaTimeSeconds = 0.016_666_666f;
        private string shared_DownloadFolderName = "/storage/emulated/0/Download";
        private string shared_OutputFolderName = "liu_yucheng.visca_recorder";

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

        // Debug output.
        private TextMeshProUGUI debugOutput_TMP;

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
        private float record_SaveIntervalSeconds = 2f;
        private float record_SaveCountdown;
        private float record_FileSwitchIntervalSeconds = 60f;
        private float record_FileSwitchingCountdown;
        private string record_FileName;
        private List<Thread> record_Threads;

        // Capturing.
        private Camera capture_RecordCamera;
        private Texture2D capture_RecordTexture;
        private List<byte[]> capture_RecordBytesList;
        private float capture_IntervalSeconds = 0.033_333_333f;
        private float capture_Countdown;
        private float capture_SaveIntervalSeconds = 2f;
        private float capture_SaveCountdown;
        private float capture_FolderSwitchIntervalSeconds = 60f;
        private float capture_FolderSwitchCountdown;
        private int capture_RecordIndex;
        private string capture_RecordFolderName;
        private List<string> capture_RecordFilePaths;
        private List<Thread> capture_Threads;

        // begin MonoBehaviour callbacks.

        void Start()
        {
            Start_Shared();
            Start_Time();
            Start_EyeGazeLegacy();
            Start_FaceLegacy();
            Start_TrackedDevices();
            Start_Controllers();
            Start_Actions();
            Start_DebugOutput();
            Start_EyeGazeObjects();
            Start_FaceObjects();
            Start_Record();
            Start_Capture();
        }

        void FixedUpdate()
        {
            FixedUpdate_Shared();
            FixedUpdate_Time();
            FixedUpdate_EyeGazeLegacy();
            FixedUpdate_FaceLegacy();
            FixedUpdate_TrackedDevices();
            FixedUpdate_Controllers();
            FixedUpdate_Actions();
            FixedUpdate_Record();
            FixedUpdate_DebugOutput();
            FixedUpdate_EyeGazeObjects();
            FixedUpdate_FaceObjects();
            FixedUpdate_Record2();
            FixedUpdate_Capture();
        }

        private void LateUpdate()
        {
            LateUpdate_Capture();
        }

        void OnDestroy()
        {
            Destory_FaceLegacy();
            Destory_Record();
            Destory_Capture();
        }

        // end MonoBehaviour callbacks.
        // begin shared helpers.

        private void Start_Shared()
        {
            Application.runInBackground = true;
            Time.fixedDeltaTime = shared_DeltaTimeSeconds;
            shared_OutputFolderName = Path.Combine(shared_DownloadFolderName, shared_OutputFolderName);
            Directory.CreateDirectory(shared_OutputFolderName);
        }

        private void FixedUpdate_Shared()
        {
            // Do nothing.
        }

        // end shared helpers.
        // begin timing helpers.

        private void Start_Time()
        {
            float timeSeconds = Time.time;

            time_SceneStartTime = DateTime.Now;
            time_Now = DateTime.Now;
            time_UTCOffset = TimeZoneInfo.Local.BaseUtcOffset;
            time_SceneStartTimeSeconds = timeSeconds;
            time_PreviousTimeSeconds = timeSeconds;
            time_DeltaTimeSeconds = Time.fixedDeltaTime;
        }

        private void FixedUpdate_Time()
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

        // end timing helpers.
        // begin eye gaze legacy helpers.

        private void Start_EyeGazeLegacy()
        {
            eyeGaze_MainCamera = shared_MainCameraObject.GetComponent<Camera>();
        }

        private void FixedUpdate_EyeGazeLegacy()
        {
            Matrix4x4 xrOriginWorldMatrix = shared_XROriginObject.transform.localToWorldMatrix;
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

            Matrix4x4 xrOriginLocalMatrix = shared_XROriginObject.transform.worldToLocalMatrix;
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

        // end eye gaze legacy helpers.
        // begin face legacy helpers.

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

        // end face legacy helpers.
        // begin tracked devices helpers.

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

            Matrix4x4 xrOriginLocalMatrix = shared_XROriginObject.transform.worldToLocalMatrix;
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

        // end tracked devices helpers.
        // begin controller helpers.

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

            Matrix4x4 xrOriginLocalMatrix = shared_XROriginObject.transform.worldToLocalMatrix;
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

        // end controller helpers.
        // begin actions helpers.

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
        
        // end actions helpers.
        // begin debug output helpers.

        private void Start_DebugOutput()
        {
            debugOutput_TMP = debugOutput_Object.GetComponent<TextMeshProUGUI>();

            string debugOutputString =
                "- begin ViSCARecorder.Recorder13.Start\n"
                + "\n"
                + "record: {...}\n"
                + "\n"
                + "- end ViSCARecorder.Recorder13.Start\n"
            ;

            debugOutput_TMP.text = debugOutputString;
        }

        private void FixedUpdate_DebugOutput()
        {
            string recordString = JsonUtility.ToJson(record_Current, true);

            string debugOutputString =
                $"- begin ViSCARecorder.Recorder13.FixedUpdate\n"
                + $"\n"
                + $"{recordString}\n"
                + $"\n"
                + $"- end ViSCARecorder.Recorder13.FixedUpdate\n"
            ;

            debugOutput_TMP.text = debugOutputString;
        }

        // end debug output helpers.
        // begin eye gaze objects helpers.

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

            eyeGaze_Object.transform.position = eyeGaze_Position;
            eyeGaze_Object.transform.rotation = Quaternion.Euler(eyeGaze_Rotation);
            Vector3 leftEyeOpennessScale = eyeGaze_LeftOpennessObject.transform.localScale;
            leftEyeOpennessScale.y = eyeGaze_LeftOpenness_YScaleMin + leftEyeOpenness * (eyeGaze_LeftOpenness_YScaleMax - eyeGaze_LeftOpenness_YScaleMin);
            eyeGaze_LeftOpennessObject.transform.localScale = leftEyeOpennessScale;
            Vector3 rightEyeOpennessScale = eyeGaze_RightOpennessObject.transform.localScale;
            rightEyeOpennessScale.y = eyeGaze_RightOpenness_YScaleMin + rightEyeOpenness * (eyeGaze_RightOpenness_YScaleMax - eyeGaze_RightOpenness_YScaleMin);
            eyeGaze_RightOpennessObject.transform.localScale = rightEyeOpennessScale;
        }

        // end eye gaze objects helpers.
        // begin face objects helpers.

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

        // end face objects helpers.
        // begin recording helpers.

        private void Start_Record()
        {
            record_Current = new();
            record_Previous = new();
            record_Records = new();
            record_SaveCountdown = record_SaveIntervalSeconds;
            record_FileSwitchingCountdown = record_FileSwitchIntervalSeconds;

            record_FileName = $"visca-records-{time_Now:yyyyMMdd}-{time_Now:HHmmss}-{time_Now:ffffff}-utc{time_UTCOffset:hhmm}.json";
            __.ReplaceInvalidCharsWith("_", ref record_FileName);
            record_Threads = new();
            string recordsString = JsonUtility.ToJson(record_Records);
            string recordsFilePath = Path.Combine(shared_OutputFolderName, record_FileName);
            File.WriteAllText(recordsFilePath, recordsString);
        }

        private void FixedUpdate_Record()
        {
            record_Current.ProcessWith(record_Previous);
            record_Records.items.Add(record_Current);
        }

        private void FixedUpdate_Record2()
        {
            record_Previous = record_Current;
            record_Current = new(record_Previous);

            record_SaveCountdown -= time_DeltaTimeSeconds;

            if (record_SaveCountdown < 0)
            {
                record_SaveCountdown = record_SaveIntervalSeconds;
                ThreadStart threadStart = new(FixedUpdate_Record2_SaveRecords);
                Thread thread = new(threadStart);
                thread.Start();
                record_Threads.Add(thread);
            }

            record_FileSwitchingCountdown -= time_DeltaTimeSeconds;

            if (record_FileSwitchingCountdown < 0)
            {
                record_FileSwitchingCountdown = record_FileSwitchIntervalSeconds;
                ThreadStart threadStart = new(FixedUpdate_Record2_SaveRecords);
                Thread thread = new(threadStart);
                thread.Start();
                record_Threads.Add(thread);

                record_Records = new();
                record_FileName = $"visca-records-{time_Now:yyyyMMdd}-{time_Now:HHmmss}-{time_Now:ffffff}-utc{time_UTCOffset:hhmm}.json";
                __.ReplaceInvalidCharsWith("_", ref record_FileName);
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

        private void Destory_Record()
        {
            foreach (Thread thread in record_Threads)
            {
                thread.Join();
            }
        }

        // end recording helpers.
        // begin capturing helpers.

        private void Start_Capture()
        {
            capture_RecordCamera = capture_RecordCameraObject.GetComponent<Camera>();

            capture_RecordTexture = new Texture2D(
                capture_RecordCamera.targetTexture.width,
                capture_RecordCamera.targetTexture.height,
                TextureFormat.RGBA32,
                false
            );

            capture_RecordBytesList = new();
            capture_Countdown = capture_IntervalSeconds;
            capture_SaveCountdown = capture_SaveIntervalSeconds;
            capture_FolderSwitchCountdown = capture_FolderSwitchIntervalSeconds;
            capture_RecordIndex = 0;
            Start_Capture_SetupFolders();
            capture_RecordFilePaths = new();
            capture_Threads = new();
        }

        private void FixedUpdate_Capture()
        {
            capture_Countdown -= time_DeltaTimeSeconds;
            capture_SaveCountdown -= time_DeltaTimeSeconds;
            capture_FolderSwitchCountdown -= time_DeltaTimeSeconds;
        }

        private void LateUpdate_Capture()
        {
            if (capture_Countdown < 0f)
            {
                capture_Countdown = capture_IntervalSeconds;
                StartCoroutine(LateUpdate_Capture_MakeCaptures());
            }

            if (capture_SaveCountdown < 0f)
            {
                capture_SaveCountdown = capture_SaveIntervalSeconds;

                Capture_SaveCapturesContext context = new(
                    capture_RecordBytesList,
                    capture_RecordFilePaths
                );

                capture_RecordBytesList = new();
                capture_RecordFilePaths = new();
                ThreadStart threadStart = new(context.Start);
                Thread thread = new(threadStart);
                thread.Start();
                capture_Threads.Add(thread);
            }

            if (capture_FolderSwitchCountdown < 0f)
            {
                capture_FolderSwitchCountdown = capture_FolderSwitchIntervalSeconds;
                capture_RecordIndex = 0;
                Start_Capture_SetupFolders();
            }

            List<Thread> oldThreads = capture_Threads;
            capture_Threads = new();

            foreach (Thread thread in oldThreads)
            {
                if (thread.ThreadState == ThreadState.Stopped)
                {
                    thread.Join();
                }
                else
                {
                    capture_Threads.Add(thread);
                }
            }
        }

        private void Destory_Capture()
        {
            foreach (Thread thread in capture_Threads)
            {
                thread.Join();
            }
        }

        // end capturing helpers.
        // begin level 2 helpers.

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

        private void FixedUpdate_Record2_SaveRecords()
        {
            string recordsString = JsonUtility.ToJson(record_Records, true);
            string recordsPath = Path.Combine(shared_OutputFolderName, record_FileName);
            File.WriteAllText(recordsPath, recordsString);
        }

        private void Start_Capture_SetupFolders()
        {
            string dateTimeCustomString = $"{time_Now:yyyyMMdd}-{time_Now:HHmmss}-{time_Now:ffffff}-utc{time_UTCOffset:hhmm}";
            __.ReplaceInvalidCharsWith("_", ref dateTimeCustomString);
            capture_RecordFolderName = $"visca-capture-record-{dateTimeCustomString}";
            capture_RecordFolderName = Path.Combine(shared_OutputFolderName, capture_RecordFolderName);
            Directory.CreateDirectory(capture_RecordFolderName);
        }

        private IEnumerator LateUpdate_Capture_MakeCaptures()
        {
            yield return new WaitForEndOfFrame();
            RenderTexture.active = capture_RecordCamera.activeTexture;
            capture_RecordCamera.Render();

            capture_RecordTexture.ReadPixels(
                capture_RecordCamera.pixelRect,
                0,
                0
            );

            capture_RecordTexture.Apply();
            byte[] bytes = capture_RecordTexture.EncodeToPNG();
            RenderTexture.active = null;
            capture_RecordBytesList.Add(bytes);
            string path = $"{capture_RecordIndex:d5}.png";
            path = Path.Combine(capture_RecordFolderName, path);
            capture_RecordFilePaths.Add(path);
            capture_RecordIndex += 1;
        }

        // end level 2 helpers.
    } // end class
} // end namespace

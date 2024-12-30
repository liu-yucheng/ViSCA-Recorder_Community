// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using TMPro;

using Unity.XR.PXR;

using UnityEngine;
using UnityEngine.XR;

using ViSCARecorder.Recorder21_;
using Face = ViSCARecorder.Recorder21_.Face;
using Record = ViSCARecorder.Recorder21_.Record;
using Capture = ViSCARecorder.Recorder21_.Capture;
using UnityEngine.UI;
using Unity.VisualScripting;

namespace ViSCARecorder
{
    public class Recorder21 : MonoBehaviour
    {
        // Shared (public).
        public GameObject shared_XROriginObject;
        public GameObject shared_MainCameraObject;

        // Textual outputs (public).
        public GameObject text_DebugOutputObject;
        public GameObject text_HUDInfoOutputObject;
        public GameObject text_RecordStatusObject;
        public GameObject text_CaptureStatusObject;

        // Eye gaze objects (public).
        public GameObject eyeGaze_Object;
        public GameObject eyeGaze_LeftOpennessObject;
        public GameObject eyeGaze_RightOpennessObject;

        // Face objects (public).
        public GameObject face_LeftEyeObject;
        public GameObject face_RightEyeObject;
        public GameObject face_HeadObject;
        public GameObject face_TeethObject;

        // Recording (public).
        public Record::Record record_Current;

        // Capturing (public).
        public GameObject capture_RecordCameraObject;

        // Shared.
        private float shared_UpdateIntervalSeconds = 0.033_333_333f; // 30 Hz.
        private float shared_UpdateCountdown;
        private float shared_DeltaTimeSeconds = 0.011_111_111f; // 90 Hz.
        private Camera shared_MainCamera;
        private string shared_DownloadFolderName = "/storage/emulated/0/Download";
        private string shared_OutputFolderName = "liu_yucheng.visca_recorder";

        // Timing.
        private float time_UpdateIntervalSeconds = 0.033_333_333f; // 30 Hz.
        private float time_UpdateCountdown;
        private DateTime time_SceneStartTime;
        private DateTime time_Now;
        private TimeSpan time_UTCOffset;
        private float time_Seconds;
        private float time_SceneStartTimeSeconds;
        private float time_PreviousFixedUpdateSeconds;
        private float time_FixedDeltaTimeSeconds;

        // Eye gaze legacy.
        private float eyeGaze_UpdateIntervalSeconds = 0.033_333_333f; // 30 Hz.
        private float eyeGaze_UpdateCountdown;
        private bool eyeGaze_UpdatePending;
        private Camera eyeGaze_MainCamera;
        private Vector3 eyeGaze_Position;
        private Vector3 eyeGaze_Rotation;

        // Face legacy.
        private float face_UpdateIntervalSeconds = 0.033_333_333f; // 30 Hz.
        private float face_UpdateCountdown;
        private bool face_UpdatePending;
        private Dictionary<string, float> face_BlendShapeDict;
        private Dictionary<string, float> face_BlendShapeDictMirrored;

        // Tracked devices.
        private float trackedDevice_UpdateIntervalSeconds = 0.033_333_333f; // 30 Hz.
        private float trackedDevice_UpdateCountdown;
        private InputDevice? trackedDevice_Headset;
        private InputDevice? trackedDevice_LeftController;
        private InputDevice? trackedDevice_rightController;

        // Controllers.
        private float controller_UpdateIntervalSeconds = 0.033_333_333f; // 30 Hz.
        private float controller_UpdateCountdown;

        // Actions.
        private float action_UpdateIntervalSeconds = 0.033_333_333f; // 30 Hz.
        private float action_UpdateCountdown;

        // Textual outputs.
        private float text_DebugUpdateIntervalSeconds = 0.333_333_333f; // 3 Hz.
        private float text_DebugUpdateCountdown;
        private Text text_DebugOutputText;
        private float text_UpdateIntervalSeconds = 0.1f; // 10 Hz.
        private float text_UpdateCountdown;
        private Text text_HUDInfoOutputText;
        private Color text_HUDInfoNoSicknessColor = new(0f, 1f, 0f, 1f);
        private Color text_HUDInfoWeakSicknessColor = new(1f, 1f, 0f, 1f);
        private Color text_HUDInfoSicknessColor = new(1f, 0f, 0f, 1f);
        private Text text_RecordStatusText;
        private Color text_RecordStatusOKColor = new(0f, 1f, 0f, 1f);
        private Color text_RecordStatusWarningColor = new(1f, 1f, 0f, 1f);
        private Color text_RecordStatusCriticalColor = new(1f, 0f, 0f, 1f);
        private Text text_CaptureStatusText;
        private Color text_CaptureStatusOKColor = new(0f, 1f, 0f, 1f);
        private Color text_CaptureStatusWarningColor = new(1f, 1f, 0f, 1f);
        private Color text_CaptureStatusCriticalColor = new(1f, 0f, 0f, 1f);

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
        private float record_UpdateIntervalSeconds = 0.033_333_333f; // 30 Hz.
        private float record_UpdateCountdown;
        private bool record_UpdatePending;
        private float record_PreviousRecordSeconds;
        private float record_SaveIntervalSeconds = 2f;
        private float record_FileSwitchIntervalSeconds = 30f;
        private float record_FileSwitchingCountdown;
        private Record::Record record_Previous;
        private Record::Records record_Records;
        private Record::RecordTask record_Task;
        

        // Capturing.
        private float capture_UpdateIntervalSeconds = 0.033_333_333f; // 30 Hz.
        private float capture_UpdateCountdown;
        private float capture_IntervalSeconds = 0.033_333_333f; // 30 Hz.
        private float capture_FolderSwitchIntervalSeconds = 30f;
        private float capture_FolderSwitchCountdown;
        private Capture::CaptureTask capture_Task;

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
            Start_Texts();
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
            FixedUpdate_Texts();
            FixedUpdate_EyeGazeObjects();
            FixedUpdate_FaceObjects();
            FixedUpdate_Record2();
            FixedUpdate_Capture();
        }

        private void OnApplicationFocus(bool focus)
        {
            OnApplicationFocus_Text(focus);
        }

        private void OnApplicationPause(bool pause)
        {
            OnApplicationPause_Text(pause);
        }

        void OnDestroy()
        {
            OnDestroy_FaceLegacy();
            OnDestroy_Text();
        }

        // end MonoBehaviour callbacks.
        // begin shared helpers.

        private void Start_Shared()
        {
            Application.runInBackground = true;
            shared_UpdateCountdown = shared_UpdateIntervalSeconds;
            Time.fixedDeltaTime = shared_DeltaTimeSeconds;
            shared_MainCamera = shared_MainCameraObject.GetComponent<Camera>();
            shared_OutputFolderName = Path.Combine(shared_DownloadFolderName, shared_OutputFolderName);
            Directory.CreateDirectory(shared_OutputFolderName);
        }

        private void FixedUpdate_Shared()
        {
            shared_UpdateCountdown -= time_FixedDeltaTimeSeconds;

            if (shared_UpdateCountdown <= time_FixedDeltaTimeSeconds)
            {
                shared_UpdateCountdown = shared_UpdateIntervalSeconds;
            }
        }

        // end shared helpers.
        // begin timing helpers.

        private void Start_Time()
        {
            time_UpdateCountdown = time_UpdateIntervalSeconds;
            time_SceneStartTime = DateTime.Now;
            time_Now = DateTime.Now;
            time_UTCOffset = TimeZoneInfo.Local.BaseUtcOffset;
            time_Seconds = Time.time;
            time_SceneStartTimeSeconds = time_Seconds;
            time_PreviousFixedUpdateSeconds = time_Seconds;
            time_FixedDeltaTimeSeconds = Time.fixedDeltaTime;
        }

        private void FixedUpdate_Time()
        {
            time_Seconds = Time.time;
            time_FixedDeltaTimeSeconds = time_Seconds - time_PreviousFixedUpdateSeconds;
            time_PreviousFixedUpdateSeconds = time_Seconds;
            time_UpdateCountdown -= time_FixedDeltaTimeSeconds;

            if (time_UpdateCountdown <= time_FixedDeltaTimeSeconds)
            {
                time_UpdateCountdown = time_UpdateIntervalSeconds;

                time_Now = DateTime.Now;
                time_UTCOffset = TimeZoneInfo.Local.BaseUtcOffset;
                FixedUpdate_Time_FindCustomTimeString(out string dateTimeCustom);
                string dateTime = $"{time_Now:o}";
                long unixMS = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                TimeSpan gameTimeSpan = time_Now - time_SceneStartTime;
                string gameTimeCustom = $"{gameTimeSpan:ddd} days, {gameTimeSpan:hh}:{gameTimeSpan:mm}:{gameTimeSpan:ss}.{gameTimeSpan:ffffff}";
                string gameTime = $"{gameTimeSpan:c}";
                float gameTimeSeconds = time_Seconds - time_SceneStartTimeSeconds;

                record_Current.timestamp.date_time_custom = dateTimeCustom;
                record_Current.timestamp.date_time = dateTime;
                record_Current.timestamp.unix_ms = unixMS;
                record_Current.timestamp.game_time_custom = gameTimeCustom;
                record_Current.timestamp.game_time = gameTime;
                record_Current.timestamp.game_time_seconds = gameTimeSeconds;
                record_Current.timestamp.unity_fixed_delta_time_seconds = time_FixedDeltaTimeSeconds;
            }
        }

        // end timing helpers.
        // begin eye gaze legacy helpers.

        private void Start_EyeGazeLegacy()
        {
            eyeGaze_UpdateCountdown = eyeGaze_UpdateIntervalSeconds;
            eyeGaze_UpdatePending = false;
            eyeGaze_MainCamera = shared_MainCameraObject.GetComponent<Camera>();
        }

        private void FixedUpdate_EyeGazeLegacy()
        {
            eyeGaze_UpdateCountdown -= time_FixedDeltaTimeSeconds;
            
            if (eyeGaze_UpdateCountdown <= time_FixedDeltaTimeSeconds)
            {
                eyeGaze_UpdateCountdown = eyeGaze_UpdateIntervalSeconds;
                eyeGaze_UpdatePending = true;

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
        }

        // end eye gaze legacy helpers.
        // begin face legacy helpers.

        private void Start_FaceLegacy()
        {
            face_UpdateCountdown = face_UpdateIntervalSeconds;
            face_UpdatePending = false;
            PXR_System.EnableLipSync(true);
            PXR_System.EnableFaceTracking(true);
        }

        private unsafe void FixedUpdate_FaceLegacy()
        {
            face_UpdateCountdown -= time_FixedDeltaTimeSeconds;

            if (face_UpdateCountdown <= time_FixedDeltaTimeSeconds)
            {
                face_UpdateCountdown = face_UpdateIntervalSeconds;
                face_UpdatePending = true;

                PxrFaceTrackingInfo info = new();
                PXR_System.GetFaceTrackingData(0, GetDataType.PXR_GET_FACELIP_DATA, ref info);
                Face::__.FindShapeArray(info, out float[] blendShapeArray);
                Face::__.ShapeArrayToDict(blendShapeArray, false, out Dictionary<string, float> faceBlendShapeDict);
                Face::__.ShapeArrayToDict(blendShapeArray, true, out Dictionary<string, float> faceBlendShapeDictMirrored);
                float faceLaughing = info.laughingProb;

                face_BlendShapeDict = faceBlendShapeDict;
                face_BlendShapeDictMirrored = faceBlendShapeDictMirrored;

                record_Current.face.blend_shape_dict = new SerializableDict<string, float>(faceBlendShapeDict);
                record_Current.face.laughing = faceLaughing;
            }
        }

        private void OnDestroy_FaceLegacy()
        {
            PXR_System.EnableLipSync(false);
            PXR_System.EnableFaceTracking(false);
        }

        // end face legacy helpers.
        // begin tracked devices helpers.

        private void Start_TrackedDevices()
        {
            trackedDevice_UpdateCountdown = trackedDevice_UpdateIntervalSeconds;

            Start_TrackedDevices_Device(
                InputDeviceCharacteristics.HeadMounted
                | InputDeviceCharacteristics.TrackedDevice,

                out trackedDevice_Headset
            );

            Start_TrackedDevices_Device(
                InputDeviceCharacteristics.Left
                | InputDeviceCharacteristics.HeldInHand
                | InputDeviceCharacteristics.TrackedDevice,

                out trackedDevice_LeftController
            );

            Start_TrackedDevices_Device(
                InputDeviceCharacteristics.Right
                | InputDeviceCharacteristics.HeldInHand
                | InputDeviceCharacteristics.TrackedDevice,

                out trackedDevice_rightController
            );
        }

        private void FixedUpdate_TrackedDevices()
        {
            trackedDevice_UpdateCountdown -= time_FixedDeltaTimeSeconds;

            if (trackedDevice_UpdateCountdown <= time_FixedDeltaTimeSeconds)
            {
                trackedDevice_UpdateCountdown = trackedDevice_UpdateIntervalSeconds;

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
        }

        // end tracked devices helpers.
        // begin controller helpers.

        private void Start_Controllers()
        {
            controller_UpdateCountdown = controller_UpdateIntervalSeconds;
        }

        private void FixedUpdate_Controllers()
        {
            controller_UpdateCountdown -= time_FixedDeltaTimeSeconds;

            if (controller_UpdateCountdown <= time_FixedDeltaTimeSeconds)
            {
                controller_UpdateCountdown = controller_UpdateIntervalSeconds;

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
        }

        // end controller helpers.
        // begin actions helpers.

        private void Start_Actions()
        {
            action_UpdateCountdown = action_UpdateIntervalSeconds;
        }

        private void FixedUpdate_Actions()
        {
            action_UpdateCountdown -= time_FixedDeltaTimeSeconds;

            if (action_UpdateCountdown <= time_FixedDeltaTimeSeconds)
            {
                action_UpdateCountdown = action_UpdateIntervalSeconds;

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
                record_Current.game_play.locomotion.right_joystick_input = rightJoystickInput;
                record_Current.game_play.locomotion.active_input.input_value = activeInput;
                record_Current.sickness.button_input.a = aValue;
                record_Current.sickness.button_input.b = bValue;
                record_Current.sickness.button_input.x = xValue;
                record_Current.sickness.button_input.y = yValue;
                record_Current.sickness.reported = sickness;
                record_Current.sickness.deduced = sickness;
            }
        }

        // end actions helpers.
        // begin textual output helpers.

        private void Start_Texts()
        {
            text_DebugUpdateCountdown = text_DebugUpdateIntervalSeconds;
            text_DebugOutputText = text_DebugOutputObject.GetComponent<Text>();

            string debugOutputString =
                "- begin ViSCARecorder.Recorder21.Start\n"
                + "\n"
                + "record: {...}\n"
                + "\n"
                + "- end ViSCARecorder.Recorder21.Start\n"
            ;

            text_DebugOutputText.text = debugOutputString;

            text_UpdateCountdown = text_UpdateIntervalSeconds;
            text_HUDInfoOutputText = text_HUDInfoOutputObject.GetComponent<Text>();

            string timeOutputString =
                "00000102-030405-678901-utc0800; 0 days, 00:00:00.000000\n"
                + "sickness.reported: 0"
            ;

            text_HUDInfoOutputText.text = timeOutputString;
            text_HUDInfoOutputText.color = text_HUDInfoNoSicknessColor;

            text_RecordStatusText = text_RecordStatusObject.GetComponent<Text>();
            text_RecordStatusText.color = text_RecordStatusWarningColor;
            Start_Text_UpdateRecordStatusText();

            text_CaptureStatusText = text_CaptureStatusObject.GetComponent<Text>();
            text_CaptureStatusText.color = text_CaptureStatusWarningColor;
            Start_Text_UpdateCaptureStatusText();
        }

        private void FixedUpdate_Texts()
        {
            text_DebugUpdateCountdown -= time_FixedDeltaTimeSeconds;

            if (text_DebugUpdateCountdown <= time_FixedDeltaTimeSeconds)
            {
                text_DebugUpdateCountdown = text_DebugUpdateIntervalSeconds;
                string recordString = JsonUtility.ToJson(record_Current, true);

                string debugOutputString =
                    $"- begin ViSCARecorder.Recorder21.FixedUpdate\n"
                    + $"\n"
                    + $"{recordString}\n"
                    + $"\n"
                    + $"- end ViSCARecorder.Recorder21.FixedUpdate\n"
                ;

                text_DebugOutputText.text = debugOutputString;
            }

            text_UpdateCountdown -= time_FixedDeltaTimeSeconds;

            if (text_UpdateCountdown <= time_FixedDeltaTimeSeconds)
            {
                text_UpdateCountdown = text_UpdateIntervalSeconds;

                string timeOutputString =
                    $"{record_Current.timestamp.date_time_custom}; {record_Current.timestamp.game_time_custom}\n"
                    + $"sickness.reported: {record_Current.sickness.reported}"
                ;

                text_HUDInfoOutputText.text = timeOutputString;
                Color textColor;

                if (record_Current.sickness.reported <= 0f)
                {
                    textColor = text_HUDInfoNoSicknessColor;
                }
                else if (record_Current.sickness.reported <= 0.5f)
                {
                    textColor = text_HUDInfoWeakSicknessColor;
                }
                else  // else if (record_Current.sickness.reported <= 1f)
                {
                    textColor = text_HUDInfoSicknessColor;
                }

                text_HUDInfoOutputText.color = textColor;
                Start_Text_UpdateRecordStatusText();
                Start_Text_UpdateCaptureStatusText();
            }
        }

        private void OnApplicationFocus_Text(bool focus)
        {
            OnApplicationFocus_Text_Record(focus);
            OnApplicationFocus_Text_Capture(focus);
        }

        private void OnApplicationPause_Text(bool pause)
        {
            OnApplicationPause_Text_Record(pause);
            OnApplicationPause_Text_Capture(pause);
        }

        private void OnDestroy_Text()
        {
            OnDestroy_Text_Capture();
            OnDestroy_Text_Record();
        }

        // end textual output helpers.
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
            if (eyeGaze_UpdatePending)
            {
                eyeGaze_UpdatePending = false;
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
            Face::__.FindShapeIndexes(Face::__.leftEyeShapeNames, leftEyeMesh, out face_LeftEyeShapeIndexes);
            Face::__.FindShapeIndexes(Face::__.rightEyeShapeNames, rightEyeMesh, out face_RightEyeShapeIndexes);
            Face::__.FindShapeIndexes(Face::__.shapeNames, headMesh, out face_HeadShapeIndexes);
            Face::__.FindShapeIndexes(Face::__.teethShapeNames, teethMesh, out face_TeethShapeIndexes);
        }

        private void FixedUpdate_FaceObjects()
        {
            if (face_UpdatePending)
            {
                face_UpdatePending = false;

                FixedUpdate_FaceObjects_ShapeWeights(face_LeftEyeShapeIndexes, Face::__.leftEyeShapeNames, face_LeftEyeRenderer);
                FixedUpdate_FaceObjects_ShapeWeights(face_RightEyeShapeIndexes, Face::__.rightEyeShapeNames, face_RightEyeRenderer);
                FixedUpdate_FaceObjects_ShapeWeights(face_HeadShapeIndexes, Face::__.shapeNames, face_HeadRenderer);
                FixedUpdate_FaceObjects_ShapeWeights(face_TeethShapeIndexes, Face::__.teethShapeNames, face_TeethRenderer);
            }
        }

        // end face objects helpers.
        // begin recording helpers.

        private void Start_Record()
        {
            record_Current = new();

            record_UpdateCountdown = record_UpdateIntervalSeconds;
            record_UpdatePending = false;
            record_FileSwitchingCountdown = record_FileSwitchIntervalSeconds;
            record_Task = new();
            Start_Record_SetupFileNames();
            record_Previous = new();
            record_Records = new();

            record_Task.folderName = shared_OutputFolderName;
            record_Task.intervalSeconds = record_SaveIntervalSeconds;
            record_Task.records = record_Records;
            record_Task.countdown = record_SaveIntervalSeconds;
            record_Task.CreateSubtask();
        }

        private void FixedUpdate_Record()
        {
            record_UpdateCountdown -= time_FixedDeltaTimeSeconds;

            if (record_UpdateCountdown < 0f)
            {
                record_UpdateCountdown = record_UpdateIntervalSeconds;
                record_UpdatePending = true;

                record_Current.recorder_name = nameof(Recorder21);
                record_Current.timestamp.ideal_record_interval_seconds = record_UpdateIntervalSeconds;
                float actualRecordIntervalSeconds = time_Seconds - record_PreviousRecordSeconds;
                record_Current.timestamp.actual_record_interval_seconds = actualRecordIntervalSeconds;
                record_Current.ProcessWith(record_Previous);
                record_Records.items.Add(record_Current);
                record_PreviousRecordSeconds = time_Seconds;
            }
        }

        private void FixedUpdate_Record2()
        {
            record_Task.FixedUpdate(time_FixedDeltaTimeSeconds);

            if (record_UpdatePending)
            {
                record_UpdatePending = false;
                record_Previous = record_Current;
                record_Current = new(record_Previous);
                
            }

            record_FileSwitchingCountdown -= time_FixedDeltaTimeSeconds;
            
            if (record_FileSwitchingCountdown <= time_FixedDeltaTimeSeconds)
            {
                record_FileSwitchingCountdown = record_FileSwitchIntervalSeconds;
                record_Task.CreateSubtask();
                Start_Record_SetupFileNames();
                record_Records = new();
                record_Task.records = record_Records;
                record_Task.CreateSubtask();
            }
        }

        

        // end recording helpers.
        // begin capturing helpers.

        private void Start_Capture()
        {
            capture_UpdateCountdown = capture_UpdateIntervalSeconds;
            capture_FolderSwitchCountdown = capture_FolderSwitchIntervalSeconds;
            capture_Task = new();
            Start_Capture_SetupFolders();
            
            capture_Task.intervalSeconds = capture_IntervalSeconds;
            capture_Task.camera = capture_RecordCameraObject.GetComponent<Camera>();
            capture_Task.countdown = capture_IntervalSeconds;
        }

        private void FixedUpdate_Capture()
        {
            capture_Task.FixedUpdate(time_FixedDeltaTimeSeconds);
            capture_UpdateCountdown -= time_FixedDeltaTimeSeconds;

            if (capture_UpdateCountdown <= time_FixedDeltaTimeSeconds)
            {
                capture_UpdateCountdown = capture_UpdateIntervalSeconds;
            }

            capture_FolderSwitchCountdown -= time_FixedDeltaTimeSeconds;

            if (capture_FolderSwitchCountdown <= time_FixedDeltaTimeSeconds)
            {
                capture_FolderSwitchCountdown = capture_FolderSwitchIntervalSeconds;
                Start_Capture_SetupFolders();
            }
        }

        // end capturing helpers.
        // begin higher level helpers.

        private void FixedUpdate_Time_FindCustomTimeString(out string customTimeString)
        {
            if (time_UTCOffset > TimeSpan.Zero)
            {
                customTimeString = $"{time_Now:yyyyMMdd}-{time_Now:HHmmss}-{time_Now:ffffff}-utc{time_UTCOffset:hhmm}";
            }
            else
            {
                customTimeString = $"{time_Now:yyyyMMdd}-{time_Now:HHmmss}-{time_Now:ffffff}-utc-{time_UTCOffset:hhmm}";
            }
        }

        private void Start_TrackedDevices_Device(
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
            float weakThreshold = 0.5f;
            float threshold = 1f;

            if (
                aValue >= weakThreshold
                || bValue >= weakThreshold
                || xValue >= weakThreshold
                || yValue >= weakThreshold
            )
            {
                sickness = 0.5f;
            }

            if (
                aValue >= threshold
                || bValue >= threshold
                || xValue >= threshold
                || yValue >= threshold
            )
            {
                sickness = 1f;
            }
        }

        private void Start_Text_UpdateRecordStatusText()
        {
            int count;

            if (record_Task == null)
            {
                count = -1;
            }
            else
            {
                count = record_Task.subtasks.Count;
            }
            text_RecordStatusText.text = $"Recording tasks: {count}";
        }

        private void Start_Text_UpdateCaptureStatusText()
        {
            int count;

            if (capture_Task == null)
            {
                count = -1;
            }
            else
            {
                count = capture_Task.subtasks.Count;
            }

            text_CaptureStatusText.text = $"Capturing tasks: {count}";
        }

        private void OnApplicationFocus_Text_Record(bool focus)
        {
            if (!focus)
            {
                text_RecordStatusText.color = text_RecordStatusCriticalColor;
            }

            OnApplicationFocus_RecordCompleteAllTasks(focus);

            if (!focus)
            {
                text_RecordStatusText.color = text_RecordStatusOKColor;
                record_Task.fixedUpdateEnabled = false;
                record_Task.createSubtaskEnabled = false;
            }
            else
            {
                text_RecordStatusText.color = text_RecordStatusWarningColor;
                record_Task.fixedUpdateEnabled = true;
                record_Task.createSubtaskEnabled = true;
            }

            Start_Text_UpdateRecordStatusText();
        }

        private void OnApplicationFocus_Text_Capture(bool focus)
        {
            if (!focus)
            {
                text_CaptureStatusText.color = text_CaptureStatusCriticalColor;
            }

            OnApplicationFocus_Text_CaptureCompleteAllTasks(focus);

            if (!focus)
            {
                text_CaptureStatusText.color = text_CaptureStatusOKColor;
                capture_Task.fixedUpdateEnabled = false;
                capture_Task.createSubtaskEnabled = false;
            }
            else
            {
                text_CaptureStatusText.color = text_CaptureStatusWarningColor;
                capture_Task.fixedUpdateEnabled = true;
                capture_Task.createSubtaskEnabled = true;
            }

            Start_Text_UpdateCaptureStatusText();
        }

        private void OnApplicationPause_Text_Record(bool pause)
        {
            OnApplicationFocus_RecordCompleteAllTasks(!pause);
        }

        private void OnApplicationPause_Text_Capture(bool pause)
        {
            OnApplicationFocus_Text_CaptureCompleteAllTasks(!pause);
        }

        private void OnDestroy_Text_Record()
        {
            OnApplicationFocus_RecordCompleteAllTasks(false);
        }

        private void OnDestroy_Text_Capture()
        {
            OnApplicationFocus_Text_CaptureCompleteAllTasks(false);
        }

        private void OnApplicationFocus_RecordCompleteAllTasks(bool focus)
        {
            if (!focus)
            {
                record_Task.CompleteAllSubtasks();
            }
        }

        private void OnApplicationFocus_Text_CaptureCompleteAllTasks(bool focus)
        {
            if (!focus)
            {
                capture_Task.CompleteAllSubtasks();
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

        private void Start_Record_SetupFileNames()
        {
            FixedUpdate_Time_FindCustomTimeString(out string timestamp);
            string fileName = $"visca-records-{timestamp}.json";
            __.ReplaceInvalidCharsWith("_", ref fileName);
            record_Task.fileName = fileName;
        }

        private void Start_Capture_SetupFolders()
        {
            FixedUpdate_Time_FindCustomTimeString(out string timestamp);
            string folderName = $"visca-captures-{timestamp}";
            __.ReplaceInvalidPathCharsWith("_", ref folderName);
            folderName = Path.Combine(shared_OutputFolderName, folderName);
            Directory.CreateDirectory(folderName);
            capture_Task.folderName = folderName;
            capture_Task.folderStartTimeSeconds = time_Seconds;
        }

        // end higher level helpers.
    } // end class
} // end namespace

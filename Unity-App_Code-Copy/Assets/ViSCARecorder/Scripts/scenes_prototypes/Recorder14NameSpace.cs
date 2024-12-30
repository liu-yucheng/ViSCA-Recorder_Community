// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Unity.Jobs;
using Unity.XR.CoreUtils.Collections;
using Unity.XR.PXR;

using UnityEngine;
using UnityEngine.UIElements;

namespace ViSCARecorder.Recorder14NameSpace
{
    public enum Face_BlendShapes
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

    public enum Face_LeftEyeShapes
    {
        eyeLookDownLeft = 0,
        eyeLookInLeft = 2,
        eyeLookUpLeft = 31,
        eyeLookOutLeft = 44
    }

    public enum Face_RightEyeShapes
    {
        eyeLookInRight = 11,
        eyeLookDownRight = 12,
        eyeLookUpRight = 35,
        eyeLookOutRight = 45
    }

    public enum Face_TeethShapes
    {
        tongueOut = 51
    }

    public enum Face_BlendShapesMirrored
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

    public enum Face_LeftEyeShapesMirrored
    {
        eyeLookInLeft = 11,
        eyeLookDownLeft = 12,
        eyeLookUpLeft = 35,
        eyeLookOutLeft = 45
    }

    public enum Face_RightEyeShapesMirrored
    {
        eyeLookDownRight = 0,
        eyeLookInRight = 2,
        eyeLookUpRight = 31,
        eyeLookOutRight = 44
    }

    public enum Face_TeethShapesMirrored
    {
        tongueOut = 51
    }

    [Serializable]
    public class Records
    {
        public List<Record> items = new();

        public Records()
        {
            // Do nothing.
        }

        public Records(Records original)
        {
            items = new(original.items);
        }
    }

    [Serializable]
    public class Record
    {
        public Timestamp timestamp = new();
        public EyeGaze eye_gaze = new();
        public Face face = new();
        public SpatialPose headset_spatial_pose = new();
        public SpatialPose left_controller_spatial_pose = new();
        public SpatialPose right_controller_spatial_pose = new();
        public GamePlay game_play = new();
        public Sickness sickness = new();

        public Record()
        {
            // Do nothing.
        }

        public Record(Record original)
        {
            timestamp = new(original.timestamp);
            eye_gaze = new(original.eye_gaze);
            face = new(original.face);
            headset_spatial_pose = new(original.headset_spatial_pose);
            left_controller_spatial_pose = new(original.left_controller_spatial_pose);
            right_controller_spatial_pose = new(original.right_controller_spatial_pose);
            game_play = new(original.game_play);
            sickness = new(original.sickness);
        }

        public void ProcessWith(in Record previous)
        {
            timestamp.ProcessWith(previous.timestamp, this);
            eye_gaze.ProcessWith(previous.eye_gaze, this);
            face.ProcessWith(previous.face, this);
            headset_spatial_pose.ProcessWith(previous.headset_spatial_pose, this);
            left_controller_spatial_pose.ProcessWith(previous.left_controller_spatial_pose, this);
            right_controller_spatial_pose.ProcessWith(previous.right_controller_spatial_pose, this);
            game_play.ProcessWith(previous.game_play, this);
            sickness.ProcessWith(previous.sickness, this);
        }
    }

    [Serializable]
    public class Timestamp
    {
        public string date_time_custom = "";
        public string date_time = "";
        public long unix_ms = 0L;
        public string game_time_custom = "";
        public string game_time = "";
        public float game_time_seconds = 0f;
        public float delta_time_seconds = 0f;

        public Timestamp()
        {
            // Do nothing.
        }

        public Timestamp(Timestamp original)
        {
            date_time_custom = original.date_time_custom;
            date_time = original.date_time;
            unix_ms = original.unix_ms;
            game_time_seconds = original.game_time_seconds;
            delta_time_seconds = original.delta_time_seconds;
        }

        public void ProcessWith(in Timestamp previous, in Record currentRecord)
        {
            // Do nothing.
        }
    }

    [Serializable]
    public class EyeGaze
    {
        public SpatialPose spatial_pose = new();
        public ViewportPose viewport_pose = new();
        public float left_eye_openness = 0f;
        public float right_eye_openness = 0f;

        public EyeGaze()
        {
            // Do nothing.
        }

        public EyeGaze(EyeGaze original)
        {
            spatial_pose = new(original.spatial_pose);
            viewport_pose = new(original.viewport_pose);
            left_eye_openness = original.left_eye_openness;
            right_eye_openness = original.right_eye_openness;
        }

        public void ProcessWith(in EyeGaze previous, in Record currentRecord)
        {
            spatial_pose.ProcessWith(previous.spatial_pose, currentRecord);
            viewport_pose.ProcessWith(previous.viewport_pose, currentRecord);
        }
    }

    [Serializable]
    public class Face
    {
        public SerializableDict<string, float> blend_shape_dict = new();
        public float laughing = 0f;

        public Face()
        {
            // Do nothing.
        }

        public Face(Face original)
        {
            blend_shape_dict = new(original.blend_shape_dict);
            laughing = original.laughing;
        }

        public void ProcessWith(in Face previous, in Record currentRecord)
        {
            // Do nothing.
        }
    }

    [Serializable]
    public class GamePlay
    {
        public string scene_name = "";
        public int random_seed;
        public Goal goal = new();
        public Locomotion locomotion = new();

        public GamePlay()
        {
            // Do nothing.
        }

        public GamePlay(GamePlay original)
        {
            scene_name = original.scene_name;
            random_seed = original.random_seed;
            goal = new(original.goal);
            locomotion = new(original.locomotion);
        }

        public void ProcessWith(in GamePlay previous, in Record currentRecord)
        {
            goal.ProcessWith(previous.goal, currentRecord);
            locomotion.ProcessWith(previous.locomotion, currentRecord);
        }
    }

    [Serializable]
    public class Sickness
    {
        public ButtonInput button_input = new();
        public float reported = 0f;
        public float deduced = 0f;
        public float predicted = 0f;

        public Sickness()
        {
            // Do nothing.
        }

        public Sickness(Sickness original)
        {
            button_input = new(original.button_input);
            reported = original.reported;
            deduced = original.deduced;
            predicted = original.predicted;
        }

        public void ProcessWith(in Sickness previous, in Record currentRecord)
        {
            button_input.ProcessWith(previous.button_input, currentRecord);
        }
    }

    [Serializable]
    public class Goal
    {
        public int index = 0;
        public int count = 0;
        public List<float> times_seconds = new();
        public List<float> intervals_seconds = new();

        public Goal()
        {
            // Do nothing.
        }

        public Goal(Goal original)
        {
            index = original.index;
            count = original.count;
            times_seconds = new(original.times_seconds);
            intervals_seconds = new(original.intervals_seconds);
        }

        public void ProcessWith(in Goal previous, in Record currentRecord)
        {
            // Do nothing.
        }
    }

    [Serializable]
    public class Locomotion
    {
        public Vector2 left_joystick_input = Vector2.zero;
        public Vector2 right_joystick_input = Vector2.zero;
        public LocomotionInput active_input = new();
        public LocomotionInput passive_input = new();
        public SpatialPose spatial_pose = new();

        public Locomotion()
        {
            // Do nothing.
        }

        public Locomotion(Locomotion original)
        {
            left_joystick_input = original.left_joystick_input;
            right_joystick_input = original.right_joystick_input;
            active_input = new(original.active_input);
            passive_input = new(original.passive_input);
            spatial_pose = new(original.spatial_pose);
        }

        public void ProcessWith(in Locomotion previous, in Record currentRecord)
        {
            active_input.ProcessWith(previous.active_input, currentRecord);
            passive_input.ProcessWith(previous.passive_input, currentRecord);
            spatial_pose.ProcessWith(previous.spatial_pose, currentRecord);
        }
    }

    [Serializable]
    public class ButtonInput
    {
        public float a = 0f;
        public float b = 0f;
        public float x = 0f;
        public float y = 0f;

        public ButtonInput()
        {
            // Do nothing.
        }

        public ButtonInput(ButtonInput original)
        {
            a = original.a;
            b = original.b;
            x = original.x;
            y = original.y;
        }

        public void ProcessWith(in ButtonInput previous, in Record currentRecord)
        {
            // Do nothing.
        }
    }

    [Serializable]
    public class SpatialPose
    {
        public static void DuplicateRawValues(SpatialPose pose)
        {
            pose.position_raw = pose.position;
            pose.rotation_raw = pose.rotation;
            pose.velocity_raw = pose.velocity;
            pose.angular_velocity_raw = pose.angular_velocity;
            pose.acceleration_raw = pose.acceleration;
            pose.angular_acceleration_raw = pose.angular_acceleration;
        }

        public static void FindDerivativesWith(
            in SpatialPose previous,
            in float deltaTimeSeconds,
            SpatialPose current
        )
        {
            Vector3 positionRaw = current.position_raw;
            Vector3 rotationRaw = current.rotation_raw;

            Vector3 velocityRaw = (current.position_raw - previous.position_raw);
            velocityRaw *= 1 / deltaTimeSeconds;

            __.FindEulerAnglesDiff(previous.rotation_raw, current.rotation_raw, out Vector3 rotationDiff);
            Vector3 angularVelocityRaw = rotationDiff;
            angularVelocityRaw *= 1 / deltaTimeSeconds;

            Vector3 accelerationRaw = (velocityRaw - previous.velocity_raw);
            accelerationRaw *= 1 / deltaTimeSeconds;

            Vector3 angluarAccelerationRaw = (angularVelocityRaw - previous.angular_velocity_raw);
            angluarAccelerationRaw *= 1 / deltaTimeSeconds;

            current.position_raw = positionRaw;
            current.rotation_raw = rotationRaw;
            current.velocity_raw = velocityRaw;
            current.angular_velocity_raw = angularVelocityRaw;
            current.acceleration_raw = accelerationRaw;
            current.angular_acceleration_raw = angluarAccelerationRaw;

            __.FilterEMA(0.5f, positionRaw, previous.position, out current.position);
            __.FilterEMA(0.5f, rotationRaw, previous.rotation, out current.rotation);
            __.FilterEMA(0.5f, velocityRaw, previous.velocity, out current.velocity);
            __.FilterEMA(0.5f, angularVelocityRaw, previous.angular_velocity, out current.angular_velocity);
            __.FilterEMA(0.5f, accelerationRaw, previous.acceleration, out current.acceleration);
            __.FilterEMA(0.5f, angluarAccelerationRaw, previous.angular_acceleration, out current.angular_acceleration);
        }

        public Vector3 position = Vector3.zero;
        public Vector3 rotation = Vector3.zero;
        public Vector3 velocity = Vector3.zero;
        public Vector3 angular_velocity = Vector3.zero;
        public Vector3 acceleration = Vector3.zero;
        public Vector3 angular_acceleration = Vector3.zero;

        public Vector3 position_raw = Vector3.zero;
        public Vector3 rotation_raw = Vector3.zero;
        public Vector3 velocity_raw = Vector3.zero;
        public Vector3 angular_velocity_raw = Vector3.zero;
        public Vector3 acceleration_raw = Vector3.zero;
        public Vector3 angular_acceleration_raw = Vector3.zero;

        public SpatialPose()
        {
            // Do nothing.
        }

        public SpatialPose(SpatialPose original)
        {
            position = original.position;
            rotation = original.rotation;
            velocity = original.velocity;
            angular_velocity = original.angular_velocity;
            acceleration = original.acceleration;
            angular_acceleration = original.angular_acceleration;

            position_raw = original.position_raw;
            rotation_raw = original.rotation_raw;
            velocity_raw = original.velocity_raw;
            angular_velocity_raw = original.angular_velocity_raw;
            acceleration_raw = original.acceleration_raw;
            angular_acceleration_raw = original.angular_acceleration_raw;
        }

        public void ProcessWith(in SpatialPose previous, in Record currentRecord)
        {
            DuplicateRawValues(this);

            FindDerivativesWith(
                previous,
                currentRecord.timestamp.delta_time_seconds,
                this
            );
        }
    }

    [Serializable]
    public class ViewportPose
    {
        public static void DuplicateRawValues(ViewportPose pose)
        {
            pose.position_raw = pose.position;
            pose.velocity_raw = pose.velocity;
            pose.acceleration_raw = pose.acceleration;
        }

        public static void FindDerivativesWith(
            in ViewportPose previous,
            in float deltaTimeSeconds,
            ViewportPose current
        )
        {
            Vector2 positionRaw = current.position_raw;

            Vector2 velocityRaw = current.position_raw - previous.position_raw;
            velocityRaw *= 1 / deltaTimeSeconds;

            Vector2 accelerationRaw = velocityRaw - previous.velocity_raw;
            accelerationRaw *= 1 / deltaTimeSeconds;

            current.position_raw = positionRaw;
            current.velocity_raw = velocityRaw;
            current.acceleration_raw = accelerationRaw;

            __.FilterEMA(0.5f, positionRaw, previous.position, out current.position);
            __.FilterEMA(0.5f, velocityRaw, previous.velocity, out current.velocity);
            __.FilterEMA(0.5f, accelerationRaw, previous.acceleration, out current.acceleration);
        }

        public Vector2 position = Vector2.zero;
        public Vector2 velocity = Vector2.zero;
        public Vector2 acceleration = Vector2.zero;

        public Vector2 position_raw = Vector2.zero;
        public Vector2 velocity_raw = Vector2.zero;
        public Vector2 acceleration_raw = Vector2.zero;

        public ViewportPose()
        {
            // Do nothing.
        }

        public ViewportPose(ViewportPose original)
        {
            position = original.position;
            velocity = original.velocity;
            acceleration = original.acceleration;

            position_raw = original.position_raw;
            velocity_raw = original.velocity_raw;
            acceleration_raw = original.acceleration_raw;
        }

        public void ProcessWith(in ViewportPose previous, in Record currentRecord)
        {
            DuplicateRawValues(this);

            FindDerivativesWith(
                previous,
                currentRecord.timestamp.delta_time_seconds,
                this
            );
        }
    }

    [Serializable]
    public class LocomotionInput
    {
        public bool enabled = false;
        public Vector2 input_value = Vector2.zero;

        public LocomotionInput()
        {
            // Do nothing.
        }

        public LocomotionInput(LocomotionInput original)
        {
            enabled = original.enabled;
            input_value = original.input_value;
        }

        public void ProcessWith(in LocomotionInput previous, in Record currentRecord)
        {
            // Do nothing
        }
    }

    [Serializable]
    public class SerializableDict<KeyType, ValueType> : Dictionary<KeyType, ValueType>, ISerializationCallbackReceiver
    {
        [Serializable]
        public class Item
        {
            public KeyType key;
            public ValueType value;
        }

        [SerializeField]
        List<Item> items = new();

        public SerializableDict()
        {
            // Do nothing.
        }

        public SerializableDict(IDictionary<KeyType, ValueType> dict) : base(dict)
        {
            // Do nothing.
        }

        public virtual void OnBeforeSerialize()
        {
            items.Clear();

            foreach (KeyValuePair<KeyType, ValueType> pair in this)
            {
                Item item = new()
                {
                    key = pair.Key,
                    value = pair.Value
                };

                items.Add(item);
            }
        }

        public virtual void OnAfterDeserialize()
        {
            Clear();

            foreach (Item item in items)
            {
                if (ContainsKey(item.key))
                {
                    string message = $"Key ignored; Key: {item.key}; this: {this}; Type: {GetType().Name}";
                    Debug.LogWarning(message);
                    continue;
                }

                Add(item.key, item.value);
            }
        }
    }

    public class Capture_EncodingTask
    {
        public List<string> filePaths = new();
        public List<Texture2D> textures = new();
        public List<byte[]> bytesList = new();
        public bool coroutineEnabled = true;
        public bool completed = false;
        public Coroutine coroutine;

        public IEnumerator Encode()
        {
            int count;

            if (filePaths.Count < textures.Count)
            {
                count = filePaths.Count;
            }
            else
            {
                count = textures.Count;
            }

            for (int index = 0; index < count; index += 1)
            {
                string path = filePaths[index];
                Texture2D texture = textures[index];
                byte[] bytes = texture.EncodeToPNG();
                bytesList.Add(bytes);

                if (coroutineEnabled)
                {
                    yield return null;
                }
            }

            completed = true;
        }
    }

    public class Capture_SavingTask
    {
        public List<string> filePaths = new();
        public List<byte[]> bytesList = new();
        public bool completed = false;
        public Thread thread;

        public void Save()
        {
            int count;

            if (filePaths.Count < bytesList.Count)
            {
                count = filePaths.Count;
            }
            else
            {
                count = bytesList.Count;
            }

            for (int index = 0; index < count; index += 1)
            {
                string path = filePaths[index];
                byte[] bytes = bytesList[index];
                File.WriteAllBytes(path, bytes);
            }

            completed = true;
        }
    }

    public class __
    {
        public static int face_ShapeArrayLength = 72;

        public static List<string> face_ShapeNames = new()
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

        public static List<string> face_LeftEyeShapeNames = new()
        {
            "eyeLookDownLeft",
            "eyeLookInLeft",
            "eyeLookUpLeft",
            "eyeLookOutLeft"
        };

        public static List<string> face_RightEyeShapeNames = new()
        {
            "eyeLookInRight",
            "eyeLookDownRight",
            "eyeLookUpRight",
            "eyeLookOutRight"
        };

        public static List<string> face_TeethShapeNames = new()
        {
            "tongueOut"
        };

        public static List<string> face_ShapeNamesMirrored = new()
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

        public static List<string> face_LeftEyeShapeNamesMirrored = new()
        {
            "eyeLookInLeft",
            "eyeLookDownLeft",
            "eyeLookUpLeft",
            "eyeLookOutLeft"
        };

        public static List<string> face_RightEyeShapeNamesMirrored = new()
        {
            "eyeLookDownRight",
            "eyeLookInRight",
            "eyeLookUpRight",
            "eyeLookOutRight"
        };

        public static List<string> face_TeethShapeNamesMirrored = new()
        {
            "tongueOut"
        };

        public static void NormalizeEulerAngles(ref Vector3 angles)
        {
            angles.x %= 360f;
            angles.y %= 360f;
            angles.z %= 360f;

            if (angles.x > 180f)
            {
                angles.x -= 360f;
            }
            else if (angles.x < -180f)
            {
                angles.x += 360f;
            }

            if (angles.y > 180f)
            {
                angles.y -= 360f;
            }
            else if (angles.y < -180f)
            {
                angles.y += 360f;
            }

            if (angles.z > 180f)
            {
                angles.z -= 360f;
            }
            else if (angles.z < -180f)
            {
                angles.z += 360f;
            }
        }

        public static unsafe void FindShapeArray(in PxrFaceTrackingInfo info, out float[] array)
        {
            array = new float[face_ShapeArrayLength];

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
            in bool mirrorEnabled,
            out Dictionary<string, float> dict
        )
        {
            dict = new();
            List<string> shapeNames;

            if (mirrorEnabled)
            {
                shapeNames = face_ShapeNamesMirrored;
            }
            else
            {
                shapeNames = face_ShapeNames;
            }

            for (int index = 0; index < shapeNames.Count; index += 1)
            {
                if (index < array.Length)
                {
                    dict.Add(shapeNames[index], array[index]);
                }
                else
                {
                    dict.Add(shapeNames[index], 0f);
                }
            }
        }

        public static void ShapeArrayToList(in float[] array, out List<float> list)
        {
            list = new();

            for (int index = 0; index < array.Length; index += 1)
            {
                float element = array[index];
                list.Add(element);
            }
        }

        public static void ShapeDictToString(in Dictionary<string, float> dict, out string string_)
        {
            List<string> elementStringList = new();

            foreach (string key in dict.Keys)
            {
                float value = dict[key];
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
        )
        {
            shapeIndexes = new();

            for (int index = 0; index < shapeNames.Count; index += 1)
            {
                string shapeName = shapeNames[index];
                int shapeIndex = sharedMesh.GetBlendShapeIndex(shapeName);
                shapeIndexes.Add(shapeIndex);
            }
        }

        public static void FindEulerAnglesDiff(
            in Vector3 previous,
            in Vector3 current,
            out Vector3 diff
        )
        {
            Quaternion previousQuat = Quaternion.Euler(previous);
            Quaternion currentQuat = Quaternion.Euler(current);
            Quaternion diffQuat = currentQuat * Quaternion.Inverse(previousQuat);
            diff = diffQuat.eulerAngles;
            NormalizeEulerAngles(ref diff);
        }

        public static void FilterEMA(
            in float alpha,
            in Vector3 currentRaw,
            in Vector3 previous,
            out Vector3 filtered
        )
        {
            filtered =
                alpha * currentRaw +
                (1 - alpha) * previous
            ;
        }

        public static void FilterEMA(
            in float alpha,
            in Vector2 currentRaw,
            in Vector2 previous,
            out Vector2 filtered
        )
        {
            filtered =
                alpha * currentRaw +
                (1 - alpha) * previous
            ;
        }

        public static void Vector3ToList(in Vector3 vector3, out List<float> list)
        {
            list = new()
            {
                vector3.x,
                vector3.y,
                vector3.z
            };
        }

        public static void Vector2ToList(in Vector2 vector2, out List<float> list)
        {
            list = new()
            {
                vector2.x,
                vector2.y
            };
        }

        public static void ListToVector3(in List<float> list, out Vector3 vector3)
        {
            vector3 = Vector3.zero;
            int count = list.Count;

            if (count >= 1)
            {
                vector3.x = list[0];
            }
            
            if (count >= 2)
            {
                vector3.y = list[1];
            }
            
            if (count >= 3)
            {
                vector3.z = list[2];
            }
        }

        public static void ListToVector2(in List<float> list, out Vector2 vector2)
        {
            vector2 = Vector2.zero;
            int count = list.Count;

            if (count >= 1)
            {
                vector2.x = list[0];
            }

            if (count >= 2)
            {
                vector2.y = list[1];
            }
        }

        public static void ReplaceInvalidCharsWith(
            in string replacement,
            ref string string_
        )
        {
            ReplaceInvalidPathCharsWith(replacement, ref string_);
            ReplaceInvalidFileNameCharsWith(replacement, ref string_);
        }

        public static void ReplaceInvalidPathCharsWith(
            in string replacement,
            ref string string_
        )
        {
            char[] invalidChars = Path.GetInvalidPathChars();

            foreach (char char_ in invalidChars)
            {
                string charString = $"{char_}";
                string_ = string_.Replace(charString, replacement);
            }
        }

        public static void ReplaceInvalidFileNameCharsWith(
            in string replacement,
            ref string string_
        )
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();

            foreach (char char_ in invalidChars)
            {
                string charString = $"{char_}";
                string_ = string_.Replace(charString, replacement);
            }
        }
    } // end class
} // end namespace

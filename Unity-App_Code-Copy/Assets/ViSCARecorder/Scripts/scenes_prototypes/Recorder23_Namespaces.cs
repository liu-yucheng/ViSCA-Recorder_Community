// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Unity.Collections;
using Unity.XR.PXR;

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

using ViSCARecorder.Recorder23_.Record;

namespace ViSCARecorder.Recorder23_
{
    [Serializable]
    public class SerializableDict<KeyType, ValueType>
        : Dictionary<KeyType, ValueType>, ISerializationCallbackReceiver
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

    public class __
    {
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

        public static void ContinuousEMAFindConfigs(
            in float alphaIdeal,
            in float timeIntervalIdeal,
            in float timeIntervalActual,
            out float exponentRectification,
            out float alphaActual,
            out float _1MinusAlphaActual
        )
        {
            exponentRectification = timeIntervalActual / timeIntervalIdeal;
            _1MinusAlphaActual = (float)Math.Pow(1 - alphaIdeal, exponentRectification);
            alphaActual = 1 - _1MinusAlphaActual;
        }

        public static void FilterContinuousEMA(
            in float alphaIdeal,
            in float timeIntervalIdeal,
            in float timeIntervalActual,
            in Vector3 currentRaw,
            in Vector3 previousFiltered,
            out Vector3 currentFiltered
        )
        {
            ContinuousEMAFindConfigs(
                alphaIdeal,
                timeIntervalIdeal,
                timeIntervalActual,
                out float exponentRectification,
                out float alphaActual,
                out float _1MinusAlphaActual
            );

            currentFiltered = alphaActual * currentRaw + _1MinusAlphaActual * previousFiltered;
        }

        public static void FilterContinuousEMA(
            in float alphaIdeal,
            in float timeIntervalIdeal,
            in float timeIntervalActual,
            in Vector2 currentRaw,
            in Vector2 previousFiltered,
            out Vector2 currentFiltered
        )
        {
            ContinuousEMAFindConfigs(
                alphaIdeal,
                timeIntervalIdeal,
                timeIntervalActual,
                out float exponentRectification,
                out float alphaActual,
                out float _1MinusAlphaActual
            );

            currentFiltered = alphaActual * currentRaw + _1MinusAlphaActual * previousFiltered;
        }

        public static void VectorToList(in Vector3 vector3, out List<float> list)
        {
            list = new()
            {
                vector3.x,
                vector3.y,
                vector3.z
            };
        }

        public static void VectorToList(in Vector2 vector2, out List<float> list)
        {
            list = new()
            {
                vector2.x,
                vector2.y
            };
        }

        public static void ListToVector(in List<float> list, out Vector3 vector3)
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

        public static void ListToVector(in List<float> list, out Vector2 vector2)
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

namespace ViSCARecorder.Recorder23_.Face
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

    public enum LeftEyeShapes
    {
        eyeLookDownLeft = 0,
        eyeLookInLeft = 2,
        eyeLookUpLeft = 31,
        eyeLookOutLeft = 44
    }

    public enum RightEyeShapes
    {
        eyeLookInRight = 11,
        eyeLookDownRight = 12,
        eyeLookUpRight = 35,
        eyeLookOutRight = 45
    }

    public enum TeethShapes
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

    public enum LeftEyeShapesMirrored
    {
        eyeLookInLeft = 11,
        eyeLookDownLeft = 12,
        eyeLookUpLeft = 35,
        eyeLookOutLeft = 45
    }

    public enum RightEyeShapesMirrored
    {
        eyeLookDownRight = 0,
        eyeLookInRight = 2,
        eyeLookUpRight = 31,
        eyeLookOutRight = 44
    }

    public enum TeethShapesMirrored
    {
        tongueOut = 51
    }

    public class __
    {
        public static int shapeArrayLength = 72;

        public static List<string> shapeNames = new()
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

        public static List<string> leftEyeShapeNames = new()
        {
            "eyeLookDownLeft",
            "eyeLookInLeft",
            "eyeLookUpLeft",
            "eyeLookOutLeft"
        };

        public static List<string> rightEyeShapeNames = new()
        {
            "eyeLookInRight",
            "eyeLookDownRight",
            "eyeLookUpRight",
            "eyeLookOutRight"
        };

        public static List<string> teethShapeNames = new()
        {
            "tongueOut"
        };

        public static List<string> shapeNamesMirrored = new()
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

        public static List<string> leftEyeShapeNamesMirrored = new()
        {
            "eyeLookInLeft",
            "eyeLookDownLeft",
            "eyeLookUpLeft",
            "eyeLookOutLeft"
        };

        public static List<string> rightEyeShapeNamesMirrored = new()
        {
            "eyeLookDownRight",
            "eyeLookInRight",
            "eyeLookUpRight",
            "eyeLookOutRight"
        };

        public static List<string> teethShapeNamesMirrored = new()
        {
            "tongueOut"
        };

        public static unsafe void FindShapeArray(in PxrFaceTrackingInfo info, out float[] array)
        {
            array = new float[shapeArrayLength];

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
                shapeNames = shapeNamesMirrored;
            }
            else
            {
                shapeNames = __.shapeNames;
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
    } // end class
} // end namespace

namespace ViSCARecorder.Recorder23_.Record
{
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
        public string recorder_name = "Recorder0";
        public Timestamp timestamp = new();
        public EyeGaze eye_gaze = new();
        public Face face = new();
        public SpatialPose headset_spatial_pose = new();
        public SpatialPose left_controller_spatial_pose = new();
        public SpatialPose right_controller_spatial_pose = new();
        public GamePlay game_play = new();
        public Sickness sickness = new();
        public ContinuousEMA continuous_ema = new();

        public Record()
        {
            // Do nothing.
        }

        public Record(Record original)
        {
            recorder_name = original.recorder_name;
            timestamp = new(original.timestamp);
            eye_gaze = new(original.eye_gaze);
            face = new(original.face);
            headset_spatial_pose = new(original.headset_spatial_pose);
            left_controller_spatial_pose = new(original.left_controller_spatial_pose);
            right_controller_spatial_pose = new(original.right_controller_spatial_pose);
            game_play = new(original.game_play);
            sickness = new(original.sickness);
            continuous_ema = new(original.continuous_ema);
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
            continuous_ema.ProcessWith(previous.continuous_ema, this);
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
        public float unity_fixed_delta_time_seconds = 0f;
        public float ideal_record_interval_seconds = 0f;
        public float actual_record_interval_seconds = 0f;

        public Timestamp()
        {
            // Do nothing.
        }

        public Timestamp(Timestamp original)
        {
            date_time_custom = original.date_time_custom;
            date_time = original.date_time;
            unix_ms = original.unix_ms;
            game_time_custom = original.game_time_custom;
            game_time = original.game_time;
            game_time_seconds = original.game_time_seconds;
            unity_fixed_delta_time_seconds = original.unity_fixed_delta_time_seconds;
            ideal_record_interval_seconds = original.ideal_record_interval_seconds;
            actual_record_interval_seconds = original.actual_record_interval_seconds;
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
        public string scene_name = "scene-name";
        public int user_label = 0;
        public int preset_index = 0;
        public float vehicle_opacity = 100f;
        public int random_seed = 0;
        public Locomotion locomotion = new();
        public Specials specials = new();

        public GamePlay()
        {
            // Do nothing.
        }

        public GamePlay(GamePlay original)
        {
            scene_name = original.scene_name;
            user_label = original.user_label;
            preset_index = original.preset_index;
            random_seed = original.random_seed;
            vehicle_opacity = original.vehicle_opacity;
            locomotion = new(original.locomotion);
            specials = new(original.specials);
        }

        public void ProcessWith(in GamePlay previous, in Record currentRecord)
        {
            specials.ProcessWith(previous.specials, currentRecord);
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
    public class ContinuousEMA
    {
        public float time_interval_ideal = 0.033_333_333f;
        public float time_interval_actual = 0.033_333_333f;
        public float exponent_rectification = 1f;
        public float alpha_ideal = 0.5f;
        public float alpha_actual = 0.5f;
        public float _1_minus_alpha_ideal = 0.5f;
        public float _1_minus_alpha_actual = 0.5f;

        public ContinuousEMA()
        {
            // Do nothing.
        }

        public ContinuousEMA(ContinuousEMA original)
        {
            time_interval_ideal = original.time_interval_ideal;
            time_interval_actual = original.time_interval_actual;
            exponent_rectification = original.exponent_rectification;
            alpha_ideal = original.alpha_ideal;
            alpha_actual = original.alpha_actual;
            _1_minus_alpha_ideal = original._1_minus_alpha_ideal;
            _1_minus_alpha_actual = original._1_minus_alpha_actual;
        }

        public void ProcessWith(in ContinuousEMA previous, in Record currentRecord)
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
    public class Specials
    {
        public Color color1_path = Color.white;
        public Color color2_not_path = Color.black;
        public Color color3_not_path = Color.grey;

        public Specials()
        {
            // Do nothing.
        }

        public Specials(Specials original)
        {
            color1_path = original.color1_path;
            color2_not_path = original.color2_not_path;
            color3_not_path = original.color3_not_path;
        }

        public void ProcessWith(in Specials previous, in Record currentRecord)
        {
            // Do nothing.
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
        public static void DuplicateRawValuesFor(SpatialPose pose)
        {
            pose.raw_position = pose.position;
            pose.raw_rotation = pose.rotation;
            pose.raw_velocity = pose.velocity;
            pose.raw_angular_velocity = pose.angular_velocity;
            pose.raw_acceleration = pose.acceleration;
            pose.raw_angular_acceleration = pose.angular_acceleration;
        }

        public static void FindDerivativesWith(
            in SpatialPose previous,
            in float actualRecordIntervalSeconds,
            SpatialPose current
        )
        {
            Vector3 positionRaw = current.raw_position;
            Vector3 rotationRaw = current.raw_rotation;

            Vector3 velocityRaw = (current.raw_position - previous.raw_position);
            float actualRecordFrequencyHz = 1 / actualRecordIntervalSeconds;
            velocityRaw *= actualRecordFrequencyHz;

            Recorder23_.__.FindEulerAnglesDiff(previous.raw_rotation, current.raw_rotation, out Vector3 rotationDiff);
            Vector3 angularVelocityRaw = rotationDiff;
            angularVelocityRaw *= actualRecordFrequencyHz;

            Vector3 accelerationRaw = (velocityRaw - previous.raw_velocity);
            accelerationRaw *= actualRecordFrequencyHz;

            Vector3 angularAccelerationRaw = (angularVelocityRaw - previous.raw_angular_velocity);
            angularAccelerationRaw *= actualRecordFrequencyHz;

            current.raw_position = positionRaw;
            current.raw_rotation = rotationRaw;
            current.raw_velocity = velocityRaw;
            current.raw_angular_velocity = angularVelocityRaw;
            current.raw_acceleration = accelerationRaw;
            current.raw_angular_acceleration = angularAccelerationRaw;
        }

        public static void FilterEMAWith(in SpatialPose previous, SpatialPose current)
        {
            float alpha = 0.5f;
            Recorder23_.__.FilterEMA(alpha, current.raw_position, previous.position, out current.position);
            Recorder23_.__.FilterEMA(alpha, current.raw_rotation, previous.rotation, out current.rotation);
            Recorder23_.__.FilterEMA(alpha, current.raw_velocity, previous.velocity, out current.velocity);
            Recorder23_.__.FilterEMA(alpha, current.raw_angular_velocity, previous.angular_velocity, out current.angular_velocity);
            Recorder23_.__.FilterEMA(alpha, current.raw_acceleration, previous.acceleration, out current.acceleration);
            Recorder23_.__.FilterEMA(alpha, current.raw_angular_acceleration, previous.angular_acceleration, out current.angular_acceleration);
        }

        public static void FilterContinuousEMAWith(
            in float alphaIdeal,
            in float timeIntervalIdeal,
            in float timeIntervalActual,
            in SpatialPose previous,
            SpatialPose current
        )
        {
            Recorder23_.__.FilterContinuousEMA(
                alphaIdeal,
                timeIntervalIdeal,
                timeIntervalActual,
                current.raw_position,
                previous.position,
                out current.position
            );
            
            Recorder23_.__.FilterContinuousEMA(
                alphaIdeal,
                timeIntervalIdeal,
                timeIntervalActual,
                current.raw_rotation,
                previous.rotation,
                out current.rotation
            );
            
            Recorder23_.__.FilterContinuousEMA(
                alphaIdeal,
                timeIntervalIdeal,
                timeIntervalActual,
                current.raw_velocity,
                previous.velocity,
                out current.velocity
            );
            
            Recorder23_.__.FilterContinuousEMA(
                alphaIdeal,
                timeIntervalIdeal,
                timeIntervalActual,
                current.raw_angular_velocity,
                previous.angular_velocity,
                out current.angular_velocity
            );
            
            Recorder23_.__.FilterContinuousEMA(
                alphaIdeal,
                timeIntervalIdeal,
                timeIntervalActual,
                current.raw_acceleration,
                previous.acceleration,
                out current.acceleration
            );
            
            Recorder23_.__.FilterContinuousEMA(
                alphaIdeal,
                timeIntervalIdeal,
                timeIntervalActual,
                current.raw_angular_acceleration,
                previous.angular_acceleration,
                out current.angular_acceleration
            );
        }

        public static void FindVectorMagnitudesFor(SpatialPose current)
        {
            current.magnitude_position = current.position.magnitude;
            current.magnitude_rotation = current.rotation.magnitude;
            current.magnitude_velocity = current.velocity.magnitude;
            current.magnitude_angular_velocity = current.angular_velocity.magnitude;
            current.magnitude_acceleration = current.acceleration.magnitude;
            current.magnitude_angular_acceleration = current.angular_acceleration.magnitude;

            current.magnitude_raw_position = current.raw_position.magnitude;
            current.magnitude_raw_rotation = current.raw_rotation.magnitude;
            current.magnitude_raw_velocity = current.raw_velocity.magnitude;
            current.magnitude_raw_angular_velocity = current.raw_angular_velocity.magnitude;
            current.magnitude_raw_acceleration = current.raw_acceleration.magnitude;
            current.magnitude_raw_angular_acceleration = current.raw_angular_acceleration.magnitude;
        }

        public Vector3 position = Vector3.zero;
        public Vector3 rotation = Vector3.zero;
        public Vector3 velocity = Vector3.zero;
        public Vector3 angular_velocity = Vector3.zero;
        public Vector3 acceleration = Vector3.zero;
        public Vector3 angular_acceleration = Vector3.zero;

        public Vector3 raw_position = Vector3.zero;
        public Vector3 raw_rotation = Vector3.zero;
        public Vector3 raw_velocity = Vector3.zero;
        public Vector3 raw_angular_velocity = Vector3.zero;
        public Vector3 raw_acceleration = Vector3.zero;
        public Vector3 raw_angular_acceleration = Vector3.zero;

        public float magnitude_position = 0f;
        public float magnitude_rotation = 0f;
        public float magnitude_velocity = 0f;
        public float magnitude_angular_velocity = 0f;
        public float magnitude_acceleration = 0f;
        public float magnitude_angular_acceleration = 0f;

        public float magnitude_raw_position = 0f;
        public float magnitude_raw_rotation = 0f;
        public float magnitude_raw_velocity = 0f;
        public float magnitude_raw_angular_velocity = 0f;
        public float magnitude_raw_acceleration = 0f;
        public float magnitude_raw_angular_acceleration = 0f;

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

            raw_position = original.raw_position;
            raw_rotation = original.raw_rotation;
            raw_velocity = original.raw_velocity;
            raw_angular_velocity = original.raw_angular_velocity;
            raw_acceleration = original.raw_acceleration;
            raw_angular_acceleration = original.raw_angular_acceleration;

            magnitude_position = original.magnitude_position;
            magnitude_rotation = original.magnitude_rotation;
            magnitude_velocity = original.magnitude_velocity;
            magnitude_angular_velocity = original.magnitude_angular_velocity;
            magnitude_acceleration = original.magnitude_acceleration;
            magnitude_angular_acceleration = original.magnitude_angular_acceleration;

            magnitude_raw_position = original.magnitude_raw_position;
            magnitude_raw_rotation = original.magnitude_raw_rotation;
            magnitude_raw_velocity = original.magnitude_raw_velocity;
            magnitude_raw_angular_velocity = original.magnitude_raw_angular_velocity;
            magnitude_raw_acceleration = original.magnitude_raw_acceleration;
            magnitude_raw_angular_acceleration = original.magnitude_raw_angular_acceleration;
        }

        public void ProcessWith(in SpatialPose previous, in Record currentRecord)
        {
            DuplicateRawValuesFor(this);

            FindDerivativesWith(
                previous,
                currentRecord.timestamp.actual_record_interval_seconds,
                this
            );

            currentRecord.continuous_ema.time_interval_ideal = currentRecord.timestamp.ideal_record_interval_seconds;
            currentRecord.continuous_ema.time_interval_actual = currentRecord.timestamp.actual_record_interval_seconds;
            currentRecord.continuous_ema.alpha_ideal = 0.5f;
            currentRecord.continuous_ema._1_minus_alpha_ideal = 1 - currentRecord.continuous_ema.alpha_ideal;

            Recorder23_.__.ContinuousEMAFindConfigs(
                currentRecord.continuous_ema.alpha_ideal,
                currentRecord.continuous_ema.time_interval_ideal,
                currentRecord.continuous_ema.time_interval_actual,
                out currentRecord.continuous_ema.exponent_rectification,
                out currentRecord.continuous_ema.alpha_actual,
                out currentRecord.continuous_ema._1_minus_alpha_actual
            );

            FilterContinuousEMAWith(
                currentRecord.continuous_ema.alpha_ideal,
                currentRecord.continuous_ema.time_interval_ideal,
                currentRecord.continuous_ema.time_interval_actual,
                previous,
                this
            );

            FindVectorMagnitudesFor(this);
        }
    }

    [Serializable]
    public class ViewportPose
    {
        public static void DuplicateRawValuesFor(ViewportPose pose)
        {
            pose.raw_position = pose.position;
            pose.raw_velocity = pose.velocity;
            pose.raw_acceleration = pose.acceleration;
        }

        public static void FindDerivativesWith(
            in ViewportPose previous,
            in float actualRecordIntervalSeconds,
            ViewportPose current
        )
        {
            Vector2 positionRaw = current.raw_position;

            Vector2 velocityRaw = current.raw_position - previous.raw_position;
            float actualRecordFrequencyHz = 1 / actualRecordIntervalSeconds;
            velocityRaw *= actualRecordFrequencyHz;

            Vector2 accelerationRaw = velocityRaw - previous.raw_velocity;
            accelerationRaw *= actualRecordFrequencyHz;

            current.raw_position = positionRaw;
            current.raw_velocity = velocityRaw;
            current.raw_acceleration = accelerationRaw;
        }

        public static void FilterEMAWith(in ViewportPose previous, ViewportPose current)
        {
            float alpha = 0.5f;
            Recorder23_.__.FilterEMA(alpha, current.raw_position, previous.position, out current.position);
            Recorder23_.__.FilterEMA(alpha, current.raw_velocity, previous.velocity, out current.velocity);
            Recorder23_.__.FilterEMA(alpha, current.raw_acceleration, previous.acceleration, out current.acceleration);
        }

        public static void FilterContinuousEMAWith(
            in float alphaIdeal,
            in float idealRecordIntervalSeconds,
            in float actualRecordIntervalSeconds,
            in ViewportPose previous,
            ViewportPose current
        )
        {
            Recorder23_.__.FilterContinuousEMA(
                alphaIdeal,
                idealRecordIntervalSeconds,
                actualRecordIntervalSeconds,
                current.raw_position,
                previous.position,
                out current.position
            );

            Recorder23_.__.FilterContinuousEMA(
                alphaIdeal,
                idealRecordIntervalSeconds,
                actualRecordIntervalSeconds,
                current.raw_velocity,
                previous.velocity,
                out current.velocity
            );

            Recorder23_.__.FilterContinuousEMA(
                alphaIdeal,
                idealRecordIntervalSeconds,
                actualRecordIntervalSeconds,
                current.raw_acceleration,
                previous.acceleration,
                out current.acceleration
            );
        }

        public static void FindVectorMagnitudesFor(ViewportPose current)
        {
            current.magnitude_position = current.position.magnitude;
            current.magnitude_velocity = current.velocity.magnitude;
            current.magnitude_acceleration = current.acceleration.magnitude;

            current.magnitude_raw_position = current.raw_position.magnitude;
            current.magnitude_raw_velocity = current.raw_velocity.magnitude;
            current.magnitude_raw_acceleration = current.raw_acceleration.magnitude;
        }

        public Vector2 position = Vector2.zero;
        public Vector2 velocity = Vector2.zero;
        public Vector2 acceleration = Vector2.zero;

        public Vector2 raw_position = Vector2.zero;
        public Vector2 raw_velocity = Vector2.zero;
        public Vector2 raw_acceleration = Vector2.zero;

        public float magnitude_position = 0f;
        public float magnitude_velocity = 0f;
        public float magnitude_acceleration = 0f;

        public float magnitude_raw_position = 0f;
        public float magnitude_raw_velocity = 0f;
        public float magnitude_raw_acceleration = 0f;

        public ViewportPose()
        {
            // Do nothing.
        }

        public ViewportPose(ViewportPose original)
        {
            position = original.position;
            velocity = original.velocity;
            acceleration = original.acceleration;

            raw_position = original.raw_position;
            raw_velocity = original.raw_velocity;
            raw_acceleration = original.raw_acceleration;

            magnitude_position = original.magnitude_position;
            magnitude_velocity = original.magnitude_velocity;
            magnitude_acceleration = original.magnitude_acceleration;

            magnitude_raw_position = original.magnitude_raw_position;
            magnitude_raw_velocity = original.magnitude_raw_velocity;
            magnitude_raw_acceleration = original.magnitude_raw_acceleration;
        }

        public void ProcessWith(in ViewportPose previous, in Record currentRecord)
        {
            DuplicateRawValuesFor(this);

            FindDerivativesWith(
                previous,
                currentRecord.timestamp.actual_record_interval_seconds,
                this
            );

            currentRecord.continuous_ema.time_interval_ideal = currentRecord.timestamp.ideal_record_interval_seconds;
            currentRecord.continuous_ema.time_interval_actual = currentRecord.timestamp.actual_record_interval_seconds;
            currentRecord.continuous_ema.alpha_ideal = 0.5f;
            currentRecord.continuous_ema._1_minus_alpha_ideal = 1 - currentRecord.continuous_ema.alpha_ideal;

            Recorder23_.__.ContinuousEMAFindConfigs(
                currentRecord.continuous_ema.alpha_ideal,
                currentRecord.continuous_ema.time_interval_ideal,
                currentRecord.continuous_ema.time_interval_actual,
                out currentRecord.continuous_ema.exponent_rectification,
                out currentRecord.continuous_ema.alpha_actual,
                out currentRecord.continuous_ema._1_minus_alpha_actual
            );

            FilterContinuousEMAWith(
                currentRecord.continuous_ema.alpha_ideal,
                currentRecord.continuous_ema.time_interval_ideal,
                currentRecord.continuous_ema.time_interval_actual,
                previous,
                this
            );

            FindVectorMagnitudesFor(this);
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

    public class RecordTask
    {
        public string folderName;
        public string fileName;
        public float intervalSeconds;
        public Records records;

        public bool fixedUpdateEnabled = true;
        public float countdown;
        public bool createSubtaskEnabled = true;
        public List<RecordSubtask> subtasks = new();
        public bool processingSubtasks = false;
        public object subtaskLock = new();

        public void FixedUpdate(float fixedDeltaTimeSeconds)
        {
            if (fixedUpdateEnabled)
            {
                countdown -= fixedDeltaTimeSeconds;

                if (countdown <= fixedDeltaTimeSeconds)
                {
                    countdown = intervalSeconds;
                    CreateSubtask();
                }

                ProcessSubtasks();
            }
        }

        public void CompleteAllSubtasks()
        {
            bool backupFixedUpdateEnabled = fixedUpdateEnabled;
            bool backupCreateSubtaskEnabled = createSubtaskEnabled;
            fixedUpdateEnabled = false;
            createSubtaskEnabled = false;

            while (subtasks.Count > 0)
            {
                ProcessSubtasks();
            }

            fixedUpdateEnabled = backupFixedUpdateEnabled;
            createSubtaskEnabled = backupCreateSubtaskEnabled;
        }

        public void CreateSubtask()
        {
            if (createSubtaskEnabled)
            {
                string path = Path.Combine(folderName, fileName);

                RecordSubtask subtask = new()
                {
                    path = path,
                    records = records,
                    lock_ = subtaskLock,
                };

                ThreadStart start = new(subtask.Start);
                subtask.thread = new(start);
                subtask.thread.Start();
                subtasks.Add(subtask);
            }
        }

        private void ProcessSubtasks()
        {
            if (!processingSubtasks)
            {
                processingSubtasks = true;
                List<RecordSubtask> oldSubtasks = new(subtasks);
                subtasks.Clear();

                foreach (RecordSubtask subtask in oldSubtasks)
                {
                    if (
                        subtask.thread.ThreadState == ThreadState.Stopped
                        || subtask.thread.ThreadState == ThreadState.Aborted
                    )
                    {
                        subtask.thread.Join();
                    }
                    else
                    {
                        subtasks.Add(subtask);
                    }
                }

                processingSubtasks = false;
            }
        } // end method
    } // end class

    public class RecordSubtask
    {
        public string path;
        public Records records;
        public object lock_;

        public Thread thread;

        public void Start()
        {
            lock (lock_)
            {
                string text = JsonUtility.ToJson(records, true);
                File.WriteAllText(path, text);
            }
        }
    } // end class

    public class __
    {

    } // end class
} // end namespace

namespace ViSCARecorder.Recorder23_.Capture
{
    public enum CaptureFrameFormat
    {
        PNG,
        JPG
    }

    public class CaptureTask
    {
        public string folderName;
        public float folderStartTimeSeconds;
        public float intervalSeconds;
        public Camera camera;
        public Record.Record record_Current;

        public bool fixedUpdateEnabled = true;
        public float countdown;
        public bool createSubtaskEnabled = true;
        public List<CaptureSubtask> subtasks = new();
        public bool processingSubtasks = false;
        public Dictionary<string, object> subtaskPathLocks = new();
        public CaptureFrameFormat frameFormat = CaptureFrameFormat.JPG;

        public void FixedUpdate(float fixedDeltaTimeSeconds)
        {
            if (fixedUpdateEnabled)
            {
                countdown -= fixedDeltaTimeSeconds;

                if (countdown <= fixedDeltaTimeSeconds)
                {
                    countdown = intervalSeconds;
                    AsyncGPUReadback.Request(camera.activeTexture, 0, ReadbackCallback);
                }

                ProcessSubtasks();
            }
        }

        public void CompleteAllSubtasks()
        {
            bool backupFixedUpdateEnabled = fixedUpdateEnabled;
            bool backupCreateSubtaskEnabled = createSubtaskEnabled;
            fixedUpdateEnabled = false;
            createSubtaskEnabled = false;

            while (subtasks.Count > 0)
            {
                ProcessSubtasks();
            }

            fixedUpdateEnabled = backupFixedUpdateEnabled;
            createSubtaskEnabled = backupCreateSubtaskEnabled;
        }

        public void CreateSubtask(byte[] pixels)
        {
            if (createSubtaskEnabled)
            {
                __.FindFileNameExtension(frameFormat, out string extension);
                FindPath(extension, out string path);
                int width = camera.pixelWidth;
                int height = camera.pixelHeight;

                CaptureSubtask subtask = new()
                {
                    path = path,
                    pixels = pixels,
                    width = width,
                    height = height,
                    pathLocks = subtaskPathLocks,
                    frameFormat = frameFormat
                };

                ThreadStart start = new(subtask.Start);
                subtask.thread = new(start);
                subtask.thread.Start();
                subtasks.Add(subtask);
            }
        }

        private void ReadbackCallback(AsyncGPUReadbackRequest request)
        {
            if (request.done && !request.hasError)
            {
                NativeArray<byte> pixelsNativeArrayByte = request.GetData<byte>();
                byte[] pixels = pixelsNativeArrayByte.ToArray();
                pixelsNativeArrayByte.Dispose();
                CreateSubtask(pixels);
            }
        }

        private void FindPath(in string extension, out string path)
        {
            float time = Time.time - folderStartTimeSeconds;
            float sickness;

            if (record_Current == null)
            {
                sickness = 0;
            }
            else
            {
                sickness = record_Current.sickness.reported;
            }

            string fileName = $"time_{time:000000.000000}_sickness_{sickness:0.0}{extension}";
            Recorder23_.__.ReplaceInvalidCharsWith(".", ref fileName);
            path = Path.Combine(folderName, fileName);
        }

        private void ProcessSubtasks()
        {
            if (!processingSubtasks)
            {
                processingSubtasks = true;
                List<CaptureSubtask> oldSubtasks = new(subtasks);
                subtasks.Clear();

                foreach (CaptureSubtask subtask in oldSubtasks)
                {
                    if (
                        subtask.thread.ThreadState == ThreadState.Stopped
                        || subtask.thread.ThreadState == ThreadState.Aborted
                    )
                    {
                        subtask.thread.Join();
                    }
                    else
                    {
                        subtasks.Add(subtask);
                    }
                }

                processingSubtasks = false;
            }
        } // end method
    } // end class

    public class CaptureSubtask
    {
        public string path;
        public byte[] pixels;
        public int width;
        public int height;
        public Dictionary<string, object> pathLocks;
        public CaptureFrameFormat frameFormat;

        public Thread thread;

        public void Start()
        {
            object pathLock;

            if (pathLocks.ContainsKey(path))
            {
                pathLock = pathLocks[path];
            }
            else
            {
                lock (pathLocks)
                {
                    pathLock = new object();
                    pathLocks.Add(path, pathLock);
                }
            }

            __.FindFrame(frameFormat, pixels, width, height, out byte[] frame);

            lock (pathLock)
            {
                File.WriteAllBytes(path, frame);
            }

            lock (pathLocks)
            {
                pathLocks.Remove(path);
            }
        }
    }

    public class __
    {
        public static int jpgQuality = 90;

        public static void FindFileNameExtension(
            in CaptureFrameFormat format,
            out string extension
        )
        {
            switch (format)
            {
                case CaptureFrameFormat.PNG:
                    extension = ".png";
                break;
                case CaptureFrameFormat.JPG:
                    extension = ".jpg";
                break;
                default:
                    extension = ".png";
                break;
            }
        }

        public static void FindFrame(
            in CaptureFrameFormat format,
            in byte[] pixels,
            in int width,
            in int height,
            out byte[] frame
        )
        {
            switch (format)
            {
                case CaptureFrameFormat.PNG:
                    frame = ImageConversion.EncodeArrayToPNG(
                        pixels,
                        GraphicsFormat.R8G8B8A8_UNorm,
                        (uint)width,
                        (uint)height,
                        0
                    );
                break;
                case CaptureFrameFormat.JPG:
                    frame = ImageConversion.EncodeArrayToJPG(
                        pixels,
                        GraphicsFormat.R8G8B8A8_UNorm,
                        (uint)width,
                        (uint)height,
                        0,
                        jpgQuality
                    );
                break;
                default:
                    frame = ImageConversion.EncodeArrayToPNG(
                        pixels,
                        GraphicsFormat.R8G8B8A8_UNorm,
                        (uint)width,
                        (uint)height,
                        0
                    );
                break;
            }
        }
    } // end class
} // end namespace

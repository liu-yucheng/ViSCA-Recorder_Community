// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unity.Collections;
using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

using ViSCARecorder.Recorder25_.Face;


namespace ViSCARecorder.Recorder25_
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

namespace ViSCARecorder.Recorder25_.Face
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

    [Serializable]
    public class BlendShapesData
    {
        public float eyeLookDownLeft = 0f;
        public float noseSneerLeft = 0f;
        public float eyeLookInLeft = 0f;
        public float browInnerUp = 0f;
        public float browDownRight = 0f;
        public float mouthClose = 0f;
        public float mouthLowerDownRight = 0f;
        public float jawOpen = 0f;
        public float mouthUpperUpRight = 0f;
        public float mouthShrugUpper = 0f;
        public float mouthFunnel = 0f;
        public float eyeLookInRight = 0f;
        public float eyeLookDownRight = 0f;
        public float noseSneerRight = 0f;
        public float mouthRollUpper = 0f;
        public float jawRight = 0f;
        public float browDownLeft = 0f;
        public float mouthShrugLower = 0f;
        public float mouthRollLower = 0f;
        public float mouthSmileLeft = 0f;
        public float mouthPressLeft = 0f;
        public float mouthSmileRight = 0f;
        public float mouthPressRight = 0f;
        public float mouthDimpleRight = 0f;
        public float mouthLeft = 0f;
        public float jawForward = 0f;
        public float eyeSquintLeft = 0f;
        public float mouthFrownLeft = 0f;
        public float eyeBlinkLeft = 0f;
        public float cheekSquintLeft = 0f;
        public float browOuterUpLeft = 0f;
        public float eyeLookUpLeft = 0f;
        public float jawLeft = 0f;
        public float mouthStretchLeft = 0f;
        public float mouthPucker = 0f;
        public float eyeLookUpRight = 0f;
        public float browOuterUpRight = 0f;
        public float cheekSquintRight = 0f;
        public float eyeBlinkRight = 0f;
        public float mouthUpperUpLeft = 0f;
        public float mouthFrownRight = 0f;
        public float eyeSquintRight = 0f;
        public float mouthStretchRight = 0f;
        public float cheekPuff = 0f;
        public float eyeLookOutLeft = 0f;
        public float eyeLookOutRight = 0f;
        public float eyeWideRight = 0f;
        public float eyeWideLeft = 0f;
        public float mouthRight = 0f;
        public float mouthDimpleLeft = 0f;
        public float mouthLowerDownLeft = 0f;
        public float tongueOut = 0f;
        public float viseme_PP = 0f;
        public float viseme_CH = 0f;
        public float viseme_o = 0f;
        public float viseme_O = 0f;
        public float viseme_i = 0f;
        public float viseme_I = 0f;
        public float viseme_RR = 0f;
        public float viseme_XX = 0f;
        public float viseme_aa = 0f;
        public float viseme_FF = 0f;
        public float viseme_u = 0f;
        public float viseme_U = 0f;
        public float viseme_TH = 0f;
        public float viseme_kk = 0f;
        public float viseme_SS = 0f;
        public float viseme_e = 0f;
        public float viseme_DD = 0f;
        public float viseme_E = 0f;
        public float viseme_nn = 0f;
        public float viseme_sil = 0f;

        public BlendShapesData()
        {
            // Do nothing.
        }

        public BlendShapesData(BlendShapesData original)
        {
            eyeLookDownLeft = original.eyeLookDownLeft;
            noseSneerLeft = original.noseSneerLeft;
            eyeLookInLeft = original.eyeLookInLeft;
            browInnerUp = original.browInnerUp;
            browDownRight = original.browDownRight;
            mouthClose = original.mouthClose;
            mouthLowerDownRight = original.mouthLowerDownRight;
            jawOpen = original.jawOpen;
            mouthUpperUpRight = original.mouthUpperUpRight;
            mouthShrugUpper = original.mouthShrugUpper;
            mouthFunnel = original.mouthFunnel;
            eyeLookInRight = original.eyeLookInRight;
            eyeLookDownRight = original.eyeLookDownRight;
            noseSneerRight = original.noseSneerRight;
            mouthRollUpper = original.mouthRollUpper;
            jawRight = original.jawRight;
            browDownLeft = original.browDownLeft;
            mouthShrugLower = original.mouthShrugLower;
            mouthRollLower = original.mouthRollLower;
            mouthSmileLeft = original.mouthSmileLeft;
            mouthPressLeft = original.mouthPressLeft;
            mouthSmileRight = original.mouthSmileRight;
            mouthPressRight = original.mouthPressRight;
            mouthDimpleRight = original.mouthDimpleRight;
            mouthLeft = original.mouthLeft;
            jawForward = original.jawForward;
            eyeSquintLeft = original.eyeSquintLeft;
            mouthFrownLeft = original.mouthFrownLeft;
            eyeBlinkLeft = original.eyeBlinkLeft;
            cheekSquintLeft = original.cheekSquintLeft;
            browOuterUpLeft = original.browOuterUpLeft;
            eyeLookUpLeft = original.eyeLookUpLeft;
            jawLeft = original.jawLeft;
            mouthStretchLeft = original.mouthStretchLeft;
            mouthPucker = original.mouthPucker;
            eyeLookUpRight = original.eyeLookUpRight;
            browOuterUpRight = original.browOuterUpRight;
            cheekSquintRight = original.cheekSquintRight;
            eyeBlinkRight = original.eyeBlinkRight;
            mouthUpperUpLeft = original.mouthUpperUpLeft;
            mouthFrownRight = original.mouthFrownRight;
            eyeSquintRight = original.eyeSquintRight;
            mouthStretchRight = original.mouthStretchRight;
            cheekPuff = original.cheekPuff;
            eyeLookOutLeft = original.eyeLookOutLeft;
            eyeLookOutRight = original.eyeLookOutRight;
            eyeWideRight = original.eyeWideRight;
            eyeWideLeft = original.eyeWideLeft;
            mouthRight = original.mouthRight;
            mouthDimpleLeft = original.mouthDimpleLeft;
            mouthLowerDownLeft = original.mouthLowerDownLeft;
            tongueOut = original.tongueOut;
            viseme_PP = original.viseme_PP;
            viseme_CH = original.viseme_CH;
            viseme_o = original.viseme_o;
            viseme_O = original.viseme_O;
            viseme_i = original.viseme_i;
            viseme_I = original.viseme_I;
            viseme_RR = original.viseme_RR;
            viseme_XX = original.viseme_XX;
            viseme_aa = original.viseme_aa;
            viseme_FF = original.viseme_FF;
            viseme_u = original.viseme_u;
            viseme_U = original.viseme_U;
            viseme_TH = original.viseme_TH;
            viseme_kk = original.viseme_kk;
            viseme_SS = original.viseme_SS;
            viseme_e = original.viseme_e;
            viseme_DD = original.viseme_DD;
            viseme_E = original.viseme_E;
            viseme_nn = original.viseme_nn;
            viseme_sil = original.viseme_sil;
        }
    }

    [Serializable]
    public class BlendShapesDataMirrored
    {
        public float eyeLookDownRight = 0f;
        public float noseSneerRight = 0f;
        public float eyeLookInRight = 0f;
        public float browInnerUp = 0f;
        public float browDownLeft = 0f;
        public float mouthClose = 0f;
        public float mouthLowerDownLeft = 0f;
        public float jawOpen = 0f;
        public float mouthUpperUpLeft = 0f;
        public float mouthShrugUpper = 0f;
        public float mouthFunnel = 0f;
        public float eyeLookInLeft = 0f;
        public float eyeLookDownLeft = 0f;
        public float noseSneerLeft = 0f;
        public float mouthRollUpper = 0f;
        public float jawLeft = 0f;
        public float browDownRight = 0f;
        public float mouthShrugLower = 0f;
        public float mouthRollLower = 0f;
        public float mouthSmileRight = 0f;
        public float mouthPressRight = 0f;
        public float mouthSmileLeft = 0f;
        public float mouthPressLeft = 0f;
        public float mouthDimpleLeft = 0f;
        public float mouthRight = 0f;
        public float jawForward = 0f;
        public float eyeSquintRight = 0f;
        public float mouthFrownRight = 0f;
        public float eyeBlinkRight = 0f;
        public float cheekSquintRight = 0f;
        public float browOuterUpRight = 0f;
        public float eyeLookUpRight = 0f;
        public float jawRight = 0f;
        public float mouthStretchRight = 0f;
        public float mouthPucker = 0f;
        public float eyeLookUpLeft = 0f;
        public float browOuterUpLeft = 0f;
        public float cheekSquintLeft = 0f;
        public float eyeBlinkLeft = 0f;
        public float mouthUpperUpRight = 0f;
        public float mouthFrownLeft = 0f;
        public float eyeSquintLeft = 0f;
        public float mouthStretchLeft = 0f;
        public float cheekPuff = 0f;
        public float eyeLookOutRight = 0f;
        public float eyeLookOutLeft = 0f;
        public float eyeWideLeft = 0f;
        public float eyeWideRight = 0f;
        public float mouthLeft = 0f;
        public float mouthDimpleRight = 0f;
        public float mouthLowerDownRight = 0f;
        public float tongueOut = 0f;
        public float viseme_PP = 0f;
        public float viseme_CH = 0f;
        public float viseme_o = 0f;
        public float viseme_O = 0f;
        public float viseme_i = 0f;
        public float viseme_I = 0f;
        public float viseme_RR = 0f;
        public float viseme_XX = 0f;
        public float viseme_aa = 0f;
        public float viseme_FF = 0f;
        public float viseme_u = 0f;
        public float viseme_U = 0f;
        public float viseme_TH = 0f;
        public float viseme_kk = 0f;
        public float viseme_SS = 0f;
        public float viseme_e = 0f;
        public float viseme_DD = 0f;
        public float viseme_E = 0f;
        public float viseme_nn = 0f;
        public float viseme_sil = 0f;

        public BlendShapesDataMirrored()
        {
            // Do nothing.
        }

        public BlendShapesDataMirrored(BlendShapesDataMirrored original)
        {
            eyeLookDownRight = original.eyeLookDownRight;
            noseSneerRight = original.noseSneerRight;
            eyeLookInRight = original.eyeLookInRight;
            browInnerUp = original.browInnerUp;
            browDownLeft = original.browDownLeft;
            mouthClose = original.mouthClose;
            mouthLowerDownLeft = original.mouthLowerDownLeft;
            jawOpen = original.jawOpen;
            mouthUpperUpLeft = original.mouthUpperUpLeft;
            mouthShrugUpper = original.mouthShrugUpper;
            mouthFunnel = original.mouthFunnel;
            eyeLookInLeft = original.eyeLookInLeft;
            eyeLookDownLeft = original.eyeLookDownLeft;
            noseSneerLeft = original.noseSneerLeft;
            mouthRollUpper = original.mouthRollUpper;
            jawLeft = original.jawLeft;
            browDownRight = original.browDownRight;
            mouthShrugLower = original.mouthShrugLower;
            mouthRollLower = original.mouthRollLower;
            mouthSmileRight = original.mouthSmileRight;
            mouthPressRight = original.mouthPressRight;
            mouthSmileLeft = original.mouthSmileLeft;
            mouthPressLeft = original.mouthPressLeft;
            mouthDimpleLeft = original.mouthDimpleLeft;
            mouthRight = original.mouthRight;
            jawForward = original.jawForward;
            eyeSquintRight = original.eyeSquintRight;
            mouthFrownRight = original.mouthFrownRight;
            eyeBlinkRight = original.eyeBlinkRight;
            cheekSquintRight = original.cheekSquintRight;
            browOuterUpRight = original.browOuterUpRight;
            eyeLookUpRight = original.eyeLookUpRight;
            jawRight = original.jawRight;
            mouthStretchRight = original.mouthStretchRight;
            mouthPucker = original.mouthPucker;
            eyeLookUpLeft = original.eyeLookUpLeft;
            browOuterUpLeft = original.browOuterUpLeft;
            cheekSquintLeft = original.cheekSquintLeft;
            eyeBlinkLeft = original.eyeBlinkLeft;
            mouthUpperUpRight = original.mouthUpperUpRight;
            mouthFrownLeft = original.mouthFrownLeft;
            eyeSquintLeft = original.eyeSquintLeft;
            mouthStretchLeft = original.mouthStretchLeft;
            cheekPuff = original.cheekPuff;
            eyeLookOutRight = original.eyeLookOutRight;
            eyeLookOutLeft = original.eyeLookOutLeft;
            eyeWideLeft = original.eyeWideLeft;
            eyeWideRight = original.eyeWideRight;
            mouthLeft = original.mouthLeft;
            mouthDimpleRight = original.mouthDimpleRight;
            mouthLowerDownRight = original.mouthLowerDownRight;
            tongueOut = original.tongueOut;
            viseme_PP = original.viseme_PP;
            viseme_CH = original.viseme_CH;
            viseme_o = original.viseme_o;
            viseme_O = original.viseme_O;
            viseme_i = original.viseme_i;
            viseme_I = original.viseme_I;
            viseme_RR = original.viseme_RR;
            viseme_XX = original.viseme_XX;
            viseme_aa = original.viseme_aa;
            viseme_FF = original.viseme_FF;
            viseme_u = original.viseme_u;
            viseme_U = original.viseme_U;
            viseme_TH = original.viseme_TH;
            viseme_kk = original.viseme_kk;
            viseme_SS = original.viseme_SS;
            viseme_e = original.viseme_e;
            viseme_DD = original.viseme_DD;
            viseme_E = original.viseme_E;
            viseme_nn = original.viseme_nn;
            viseme_sil = original.viseme_sil;
        }
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

        public static float[] emptyBlendShapeArray =
        {
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,

            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,

            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f
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
            List<string> shapeNames_;

            if (mirrorEnabled)
            {
                shapeNames_ = shapeNamesMirrored;
            }
            else
            {
                shapeNames_ = shapeNames;
            }

            for (int index = 0; index < shapeNames_.Count; index += 1)
            {
                if (index < array.Length)
                {
                    dict.Add(shapeNames_[index], array[index]);
                }
                else
                {
                    dict.Add(shapeNames_[index], 0f);
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

        public static void ShapeArrayToData(
            in float[] array,
            out BlendShapesData data
        )
        {
            data = new();

            if (array.Length >= shapeArrayLength)
            {
                data.eyeLookDownLeft = array[0];
                data.noseSneerLeft = array[1];
                data.eyeLookInLeft = array[2];
                data.browInnerUp = array[3];
                data.browDownRight = array[4];
                data.mouthClose = array[5];
                data.mouthLowerDownRight = array[6];
                data.jawOpen = array[7];
                data.mouthUpperUpRight = array[8];
                data.mouthShrugUpper = array[9];
                data.mouthFunnel = array[10];
                data.eyeLookInRight = array[11];
                data.eyeLookDownRight = array[12];
                data.noseSneerRight = array[13];
                data.mouthRollUpper = array[14];
                data.jawRight = array[15];
                data.browDownLeft = array[16];
                data.mouthShrugLower = array[17];
                data.mouthRollLower = array[18];
                data.mouthSmileLeft = array[19];
                data.mouthPressLeft = array[20];
                data.mouthSmileRight = array[21];
                data.mouthPressRight = array[22];
                data.mouthDimpleRight = array[23];
                data.mouthLeft = array[24];
                data.jawForward = array[25];
                data.eyeSquintLeft = array[26];
                data.mouthFrownLeft = array[27];
                data.eyeBlinkLeft = array[28];
                data.cheekSquintLeft = array[29];
                data.browOuterUpLeft = array[30];
                data.eyeLookUpLeft = array[31];
                data.jawLeft = array[32];
                data.mouthStretchLeft = array[33];
                data.mouthPucker = array[34];
                data.eyeLookUpRight = array[35];
                data.browOuterUpRight = array[36];
                data.cheekSquintRight = array[37];
                data.eyeBlinkRight = array[38];
                data.mouthUpperUpLeft = array[39];
                data.mouthFrownRight = array[40];
                data.eyeSquintRight = array[41];
                data.mouthStretchRight = array[42];
                data.cheekPuff = array[43];
                data.eyeLookOutLeft = array[44];
                data.eyeLookOutRight = array[45];
                data.eyeWideRight = array[46];
                data.eyeWideLeft = array[47];
                data.mouthRight = array[48];
                data.mouthDimpleLeft = array[49];
                data.mouthLowerDownLeft = array[50];
                data.tongueOut = array[51];
                data.viseme_PP = array[52];
                data.viseme_CH = array[53];
                data.viseme_o = array[54];
                data.viseme_O = array[55];
                data.viseme_i = array[56];
                data.viseme_I = array[57];
                data.viseme_RR = array[58];
                data.viseme_XX = array[59];
                data.viseme_aa = array[60];
                data.viseme_FF = array[61];
                data.viseme_u = array[62];
                data.viseme_U = array[63];
                data.viseme_TH = array[64];
                data.viseme_kk = array[65];
                data.viseme_SS = array[66];
                data.viseme_e = array[67];
                data.viseme_DD = array[68];
                data.viseme_E = array[69];
                data.viseme_nn = array[70];
                data.viseme_sil = array[71];
            }
            else
            {
                Debug.LogError("ShapeArrayToData: array.Length < shapeArrayLength.");
            }
        }

        public static void ShapeArrayToDataMirrored(
            in float[] array,
            out BlendShapesDataMirrored dataMirrored
        )
        {
            dataMirrored = new();

            if (array.Length >= shapeArrayLength)
            {
                dataMirrored.eyeLookDownRight = array[0];
                dataMirrored.noseSneerRight = array[1];
                dataMirrored.eyeLookInRight = array[2];
                dataMirrored.browInnerUp = array[3];
                dataMirrored.browDownLeft = array[4];
                dataMirrored.mouthClose = array[5];
                dataMirrored.mouthLowerDownLeft = array[6];
                dataMirrored.jawOpen = array[7];
                dataMirrored.mouthUpperUpLeft = array[8];
                dataMirrored.mouthShrugUpper = array[9];
                dataMirrored.mouthFunnel = array[10];
                dataMirrored.eyeLookInLeft = array[11];
                dataMirrored.eyeLookDownLeft = array[12];
                dataMirrored.noseSneerLeft = array[13];
                dataMirrored.mouthRollUpper = array[14];
                dataMirrored.jawLeft = array[15];
                dataMirrored.browDownRight = array[16];
                dataMirrored.mouthShrugLower = array[17];
                dataMirrored.mouthRollLower = array[18];
                dataMirrored.mouthSmileRight = array[19];
                dataMirrored.mouthPressRight = array[20];
                dataMirrored.mouthSmileLeft = array[21];
                dataMirrored.mouthPressLeft = array[22];
                dataMirrored.mouthDimpleLeft = array[23];
                dataMirrored.mouthRight = array[24];
                dataMirrored.jawForward = array[25];
                dataMirrored.eyeSquintRight = array[26];
                dataMirrored.mouthFrownRight = array[27];
                dataMirrored.eyeBlinkRight = array[28];
                dataMirrored.cheekSquintRight = array[29];
                dataMirrored.browOuterUpRight = array[30];
                dataMirrored.eyeLookUpRight = array[31];
                dataMirrored.jawRight = array[32];
                dataMirrored.mouthStretchRight = array[33];
                dataMirrored.mouthPucker = array[34];
                dataMirrored.eyeLookUpLeft = array[35];
                dataMirrored.browOuterUpLeft = array[36];
                dataMirrored.cheekSquintLeft = array[37];
                dataMirrored.eyeBlinkLeft = array[38];
                dataMirrored.mouthUpperUpRight = array[39];
                dataMirrored.mouthFrownLeft = array[40];
                dataMirrored.eyeSquintLeft = array[41];
                dataMirrored.mouthStretchLeft = array[42];
                dataMirrored.cheekPuff = array[43];
                dataMirrored.eyeLookOutRight = array[44];
                dataMirrored.eyeLookOutLeft = array[45];
                dataMirrored.eyeWideLeft = array[46];
                dataMirrored.eyeWideRight = array[47];
                dataMirrored.mouthLeft = array[48];
                dataMirrored.mouthDimpleRight = array[49];
                dataMirrored.mouthLowerDownRight = array[50];
                dataMirrored.tongueOut = array[51];
                dataMirrored.viseme_PP = array[52];
                dataMirrored.viseme_CH = array[53];
                dataMirrored.viseme_o = array[54];
                dataMirrored.viseme_O = array[55];
                dataMirrored.viseme_i = array[56];
                dataMirrored.viseme_I = array[57];
                dataMirrored.viseme_RR = array[58];
                dataMirrored.viseme_XX = array[59];
                dataMirrored.viseme_aa = array[60];
                dataMirrored.viseme_FF = array[61];
                dataMirrored.viseme_u = array[62];
                dataMirrored.viseme_U = array[63];
                dataMirrored.viseme_TH = array[64];
                dataMirrored.viseme_kk = array[65];
                dataMirrored.viseme_SS = array[66];
                dataMirrored.viseme_e = array[67];
                dataMirrored.viseme_DD = array[68];
                dataMirrored.viseme_E = array[69];
                dataMirrored.viseme_nn = array[70];
                dataMirrored.viseme_sil = array[71];
            }
            else
            {
                Debug.LogError("ShapeArrayToDataMirrored: array.Length < shapeArrayLength.");
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
                    elementString = $"{key}: <format_unknown>";
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

namespace ViSCARecorder.Recorder25_.Record
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
        public GameConfigs game_configs = new();
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
            game_configs = new(original.game_configs);
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
            game_configs.ProcessWith(previous.game_configs, this);
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
        public BlendShapesData blend_shapes_data = new();
        public float laughing = 0f;

        public Face()
        {
            // Do nothing.
        }

        public Face(Face original)
        {
            blend_shapes_data = new(original.blend_shapes_data);
            laughing = original.laughing;
        }

        public void ProcessWith(in Face previous, in Record currentRecord)
        {
            // Do nothing.
        }
    }

    [Serializable]
    public class GameConfigs
    {
        public int user_index = 0;
        public int preset_index = 0;
        public float vehicle_opacity = 1f;
        public bool autopilot_enabled = true;
        public int random_seed_terrain = 0;
        public int random_seed_autopilot = 0;
        public int random_seed_specials = 0;
        public int specials_index = 0;
        public Specials specials = new();

        public GameConfigs()
        {
            // Do nothing.
        }

        public GameConfigs(GameConfigs original)
        {
            user_index = original.user_index;
            preset_index = original.preset_index;
            vehicle_opacity = original.vehicle_opacity;
            random_seed_terrain = original.random_seed_terrain;
            random_seed_autopilot = original.random_seed_autopilot;
            random_seed_specials = original.random_seed_specials;
            specials_index = original.specials_index;
            specials = new(original.specials);
        }

        public void ProcessWith(in GameConfigs previous, in Record currentRecord)
        {
            specials.ProcessWith(previous.specials, currentRecord);
        }
    }

    [Serializable]
    public class GamePlay
    {
        public string scene_name = "scene-name";
        public bool autopilot_enabled = false;
        public Locomotion locomotion = new();

        public GamePlay()
        {
            // Do nothing.
        }

        public GamePlay(GamePlay original)
        {
            scene_name = original.scene_name;
            autopilot_enabled = original.autopilot_enabled;
            locomotion = new(original.locomotion);
        }

        public void ProcessWith(in GamePlay previous, in Record currentRecord)
        {
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
        public Vector2 combined_input = Vector2.zero;
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
            combined_input = original.combined_input;
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
        public Color color1_path = SpecialsStandards.colorSpecialsRed;
        public Color color2_not_path = SpecialsStandards.colorSpecialsGreen;
        public Color color3_not_path = SpecialsStandards.colorSpecialsBlue;
        public string color1_name_path = SpecialsStandards.colorNameSpecialsRed;
        public string color2_name_not_path = SpecialsStandards.colorNameSpecialsGreen;
        public string color3_name_not_path = SpecialsStandards.colorNameSpecialsBlue;

        public Specials()
        {
            // Do nothing.
        }

        public Specials(Specials original)
        {
            color1_path = original.color1_path;
            color2_not_path = original.color2_not_path;
            color3_not_path = original.color3_not_path;
            color1_name_path = original.color1_name_path;
            color2_name_not_path = original.color2_name_not_path;
            color3_name_not_path = original.color3_name_not_path;
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

            Recorder25_.__.FindEulerAnglesDiff(previous.raw_rotation, current.raw_rotation, out Vector3 rotationDiff);
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
            Recorder25_.__.FilterEMA(alpha, current.raw_position, previous.position, out current.position);
            Recorder25_.__.FilterEMA(alpha, current.raw_rotation, previous.rotation, out current.rotation);
            Recorder25_.__.FilterEMA(alpha, current.raw_velocity, previous.velocity, out current.velocity);
            Recorder25_.__.FilterEMA(alpha, current.raw_angular_velocity, previous.angular_velocity, out current.angular_velocity);
            Recorder25_.__.FilterEMA(alpha, current.raw_acceleration, previous.acceleration, out current.acceleration);
            Recorder25_.__.FilterEMA(alpha, current.raw_angular_acceleration, previous.angular_acceleration, out current.angular_acceleration);
        }

        public static void FilterContinuousEMAWith(
            in float alphaIdeal,
            in float timeIntervalIdeal,
            in float timeIntervalActual,
            in SpatialPose previous,
            SpatialPose current
        )
        {
            Recorder25_.__.FilterContinuousEMA(
                alphaIdeal,
                timeIntervalIdeal,
                timeIntervalActual,
                current.raw_position,
                previous.position,
                out current.position
            );
            
            Recorder25_.__.FilterContinuousEMA(
                alphaIdeal,
                timeIntervalIdeal,
                timeIntervalActual,
                current.raw_rotation,
                previous.rotation,
                out current.rotation
            );
            
            Recorder25_.__.FilterContinuousEMA(
                alphaIdeal,
                timeIntervalIdeal,
                timeIntervalActual,
                current.raw_velocity,
                previous.velocity,
                out current.velocity
            );
            
            Recorder25_.__.FilterContinuousEMA(
                alphaIdeal,
                timeIntervalIdeal,
                timeIntervalActual,
                current.raw_angular_velocity,
                previous.angular_velocity,
                out current.angular_velocity
            );
            
            Recorder25_.__.FilterContinuousEMA(
                alphaIdeal,
                timeIntervalIdeal,
                timeIntervalActual,
                current.raw_acceleration,
                previous.acceleration,
                out current.acceleration
            );
            
            Recorder25_.__.FilterContinuousEMA(
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

            Recorder25_.__.ContinuousEMAFindConfigs(
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
            Recorder25_.__.FilterEMA(alpha, current.raw_position, previous.position, out current.position);
            Recorder25_.__.FilterEMA(alpha, current.raw_velocity, previous.velocity, out current.velocity);
            Recorder25_.__.FilterEMA(alpha, current.raw_acceleration, previous.acceleration, out current.acceleration);
        }

        public static void FilterContinuousEMAWith(
            in float alphaIdeal,
            in float idealRecordIntervalSeconds,
            in float actualRecordIntervalSeconds,
            in ViewportPose previous,
            ViewportPose current
        )
        {
            Recorder25_.__.FilterContinuousEMA(
                alphaIdeal,
                idealRecordIntervalSeconds,
                actualRecordIntervalSeconds,
                current.raw_position,
                previous.position,
                out current.position
            );

            Recorder25_.__.FilterContinuousEMA(
                alphaIdeal,
                idealRecordIntervalSeconds,
                actualRecordIntervalSeconds,
                current.raw_velocity,
                previous.velocity,
                out current.velocity
            );

            Recorder25_.__.FilterContinuousEMA(
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

            Recorder25_.__.ContinuousEMAFindConfigs(
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

    public class SpecialsStandards
    {
        public static Color colorSpecialsRed = new(0.9f, 0.45f, 0.45f, 1f);
        public static Color colorSpecialsGreen = new(0.45f, 0.9f, 0.45f, 1f);
        public static Color colorSpecialsBlue = new(0.45f, 0.45f, 0.9f, 1f);
        public static string colorNameSpecialsRed = "specials_red";
        public static string colorNameSpecialsGreen = "specials_green";
        public static string colorNameSpecialsBlue = "specials_blue";

        public static List<Specials> standards = new()
        {
            new()
            {
                color1_path = colorSpecialsRed,
                color2_not_path = colorSpecialsGreen,
                color3_not_path = colorSpecialsBlue,
                color1_name_path = colorNameSpecialsRed,
                color2_name_not_path = colorNameSpecialsGreen,
                color3_name_not_path = colorNameSpecialsBlue
            },
            new()
            {
                color1_path = colorSpecialsGreen,
                color2_not_path = colorSpecialsBlue,
                color3_not_path = colorSpecialsRed,
                color1_name_path = colorNameSpecialsGreen,
                color2_name_not_path = colorNameSpecialsBlue,
                color3_name_not_path = colorNameSpecialsRed
            },
            new()
            {
                color1_path = colorSpecialsBlue,
                color2_not_path = colorSpecialsRed,
                color3_not_path = colorSpecialsGreen,
                color1_name_path = colorNameSpecialsBlue,
                color2_name_not_path = colorNameSpecialsRed,
                color3_name_not_path = colorNameSpecialsGreen
            }
        };

        public static void CreateSpecialsByIndex(in int index, out Specials specials)
        {
            int index_ = index;

            if (index_ < 0)
            {
                index_ = -index_;
            }

            index_ %= standards.Count;
            specials = new(standards[index_]);
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
        } // end method
    } // end class

    public class __
    {
        // Do nothing.
    } // end class
} // end namespace

namespace ViSCARecorder.Recorder25_.Capture
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
            Recorder25_.__.ReplaceInvalidCharsWith(".", ref fileName);
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

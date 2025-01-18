// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using ViSCARecorder.GameConfigs2_;


namespace ViSCARecorder
{
    [Serializable]
    public class GameConfigs2 : MonoBehaviour
    {
        public static int UserIndex = 0;
        public static int PresetIndex = 0;
        public static float VehicleOpacity = 0.8f;
        public static bool AutopilotEnabled = true;
        public static int RandomSeed_Terrain = 0;
        public static int RandomSeed_Autopilot = 0;
        public static int RandomSeed_Specials = 0;
        public static int Specials_Index = 0;
        public static Recorder25_.Record.Specials Specials_Instance;

        public static void Standard_Apply()
        {
            GameConfigs_Standards.ByUserIndex_AndByPresetIndex_Create(
                UserIndex,
                PresetIndex,
                out GameConfigs_Standard GameConfigsStandard_
            );

            VehicleOpacity = GameConfigsStandard_.VehicleOpacity;
            AutopilotEnabled = GameConfigsStandard_.AutopilotEnabled;
            RandomSeed_Terrain = GameConfigsStandard_.RandomSeed_Terrain;
            RandomSeed_Autopilot = GameConfigsStandard_.RandomSeed_Autopilot;
            RandomSeed_Specials = GameConfigsStandard_.RandomSeed_Specials;
        }
    } // end class

    namespace GameConfigs2_
    {
        [Serializable]
        public class GameConfigs_Standard
        {
            public float VehicleOpacity = 1f;
            public bool AutopilotEnabled = true;
            public int RandomSeed_Terrain = 0;
            public int RandomSeed_Autopilot = 0;
            public int RandomSeed_Specials = 0;

            public GameConfigs_Standard()
            {
                // Do nothing.
            }

            public GameConfigs_Standard(GameConfigs_Standard Original)
            {
                VehicleOpacity = Original.VehicleOpacity;
                AutopilotEnabled = Original.AutopilotEnabled;
                RandomSeed_Terrain = Original.RandomSeed_Terrain;
                RandomSeed_Autopilot = Original.RandomSeed_Autopilot;
                RandomSeed_Specials = Original.RandomSeed_Specials;
            }
        }

        [Serializable]
        public class GameConfigs_Standards
        {
            public static List<GameConfigs_Standard> Standards = new()
            {
                new()
                {
                    VehicleOpacity = 0.8f,
                    AutopilotEnabled = true,
                    RandomSeed_Terrain = 0,
                    RandomSeed_Autopilot = 0,
                    RandomSeed_Specials = 0,
                },
                new()
                {
                    VehicleOpacity = 0.2f,
                    AutopilotEnabled = true,
                    RandomSeed_Terrain = 1,
                    RandomSeed_Autopilot = 1,
                    RandomSeed_Specials = 0,
                },
                new()
                {
                    VehicleOpacity = 0.8f,
                    AutopilotEnabled = true,
                    RandomSeed_Terrain = 2,
                    RandomSeed_Autopilot = 2,
                    RandomSeed_Specials = 0,
                },
                new()
                {
                    VehicleOpacity = 0.2f,
                    AutopilotEnabled = true,
                    RandomSeed_Terrain = 3,
                    RandomSeed_Autopilot = 3,
                    RandomSeed_Specials = 0,
                },
                new()
                {
                    VehicleOpacity = 0.8f,
                    AutopilotEnabled = true,
                    RandomSeed_Terrain = 4,
                    RandomSeed_Autopilot = 4,
                    RandomSeed_Specials = 0,
                },
                new()
                {
                    VehicleOpacity = 0.2f,
                    AutopilotEnabled = true,
                    RandomSeed_Terrain = 5,
                    RandomSeed_Autopilot = 5,
                    RandomSeed_Specials = 0,
                },
            };

            public static void ByUserIndex_AndByPresetIndex_Create(
                in int UserIndex,
                in int PresetIndex,
                out GameConfigs_Standard GameConfigsStandard_
            )
            {
                int PresetIndex_ = PresetIndex;
                PresetIndex_ %= Standards.Count;
                GameConfigsStandard_ = new(Standards[PresetIndex_]);
                GameConfigsStandard_.RandomSeed_Specials = UserIndex;
            }
        }
    } // end namespace
} // end namespace

// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

// begin References.
//
// Reference 1: ./GamePlay4.cs .
//
// end References.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Random = UnityEngine.Random;


namespace ViSCARecorder
{
    public class GameMenu1 : MonoBehaviour
    {
        // begin public fields
        public GameObject Start_Button_Object;
        public GameObject Exit_Button_Object;

        public GameObject UserIndex_Config_Object;
        public GameObject PresetIndex_Config_Object;

        public GameObject VehicleOpacity_Text_Object;
        public GameObject VehicleOpacity_Scrollbar_Object;

        public GameObject AutopilotToggle_Text_Object;
        public GameObject AutopilotToggle_Scrollbar_Object;

        public GameObject RandomSeedTerrain_Config_Object;
        public GameObject RandomSeedAutopilot_Config_Object;
        public GameObject RandomSeedSpecials_Config_Object;
        // end public fields

        // begin private fields
        private GameConfigs2_.GameConfigs_Backup GameConfigs_Backup_;
        private bool GameConfigs_Initialized = false;
        private float GameConfigs_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float GameConfigs_Refresh_Countdown_Seconds = 0f;
        private bool GameConfigs_StandardApply_Needed = false;

        private Scrollbar Start_Button;
        private string Start_SceneName = "lab-1";

        private Scrollbar Exit_Button;

        private UI_Config_Int_Custom1 UserIndex_Config;
        private float UserIndex_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float UserIndex_Refresh_Countdown_Seconds = 0f;

        private UI_Config_Int_Custom1 PresetIndex_Config;
        private float PresetIndex_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float PresetIndex_Refresh_Countdown_Seconds = 0f;

        private Text VehicleOpacity_Text;
        private Scrollbar VehicleOpacity_Scrollbar;
        private float VehicleOpacity_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float VehicleOpacity_Refresh_Countdown_Seconds = 0f;
        private float VehicleOpacity_Normalized = 1f;
        private float VehicleOpacity_Percent = 100f;
        private bool VehicleOpacity_Scrollbar_Sync_Needed = true;

        private Text AutopilotToggle_Text;
        private Scrollbar AutopilotToggle_Scrollbar;
        private float AutopilotToggle_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float AutopilotToggle_Refresh_Countdown_Seconds = 0f;
        private float AutopilotToggle_Normalized = 1f;
        private bool AutopilotToggle_Enabled = true;
        private float AutopilotToggle_EnableThreshold = 0.5f;
        private bool AutopilotToggle_Scrollbar_Sync_Needed = true;

        private UI_Config_Int_Custom1 RandomSeedTerrain_Config;
        private float RandomSeedTerrain_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float RandomSeedTerrain_Refresh_Countdown_Seconds = 0f;

        private UI_Config_Int_Custom1 RandomSeedAutopilot_Config;
        private float RandomSeedAutopilot_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float RandomSeedAutopilot_Refresh_Countdown_Seconds = 0f;

        private UI_Config_Int_Custom1 RandomSeedSpecials_Config;
        private float RandomSeedSpecials_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float RandomSeedSpecials_Refresh_Countdown_Seconds = 0f;
        private int RandomSeedSpecials_SpecialsIndex = 0;
        private Recorder25_.Record.Specials RandomSeedSpecials_SpecialsInstance;
        private Random.State RandomSeedSpecials_Random_State;
        private bool RandomSeedSpecials_Random_Initialized = false;
        // end private fields

        // begin MonoBehaviour callbacks
        void Start()
        {
            GameConfigs_Setup();
            Start_Setup();
            Exit_Setup();
            UserIndex_Setup();
            PresetIndex_Setup();
            VehicleOpacity_Setup();
            AutopilotToggle_Setup();
            RandomSeedTerrain_Setup();
            RandomSeedAutopilot_Setup();
            RandomSeedSpecials_Setup();
        }

        void FixedUpdate()
        {
            GameConfigs_Refresh();
            UserIndex_Refresh();
            PresetIndex_Refresh();
            VehicleOpacity_Refresh();
            AutopilotToggle_Refresh();
            RandomSeedTerrain_Refresh();
            RandomSeedAutopilot_Refresh();
            RandomSeedSpecials_Refresh();
        }

        void OnDestroy()
        {
            Start_TearDown();
            Exit_TearDown();
            UserIndex_TearDown();
            PresetIndex_TearDown();
            VehicleOpacity_TearDown();
            AutopilotToggle_TearDown();
            RandomSeedTerrain_TearDown();
            RandomSeedAutopilot_TearDown();
            RandomSeedSpecials_TearDown();
        }
        // end MonoBehaviour callbacks

        private void GameConfigs_Setup()
        {
            GameConfigs2_.GameConfigs_Backup.FromGameConfigs_Backup(out GameConfigs_Backup_);
        }

        // begin helpers GameConfigs
        private void GameConfigs_Refresh()
        {
            if (
                UserIndex_Config == null
                || PresetIndex_Config == null
                || VehicleOpacity_Text == null
                || VehicleOpacity_Scrollbar == null
                || AutopilotToggle_Text == null
                || AutopilotToggle_Scrollbar == null
                || RandomSeedTerrain_Config == null
                || RandomSeedAutopilot_Config == null
                || RandomSeedSpecials_Config == null
            )
            {
                return;
            }

            if (!GameConfigs_Initialized)
            {
                GameConfigs_Initialized = true;

                GameConfigs2_.GameConfigs_Backup.ToGameConfigs_Restore(GameConfigs_Backup_);
                UserIndex_Config.Config_Value = GameConfigs2.UserIndex;
                PresetIndex_Config.Config_Value = GameConfigs2.PresetIndex;
                VehicleOpacity_Normalized = GameConfigs2.VehicleOpacity;
                AutopilotToggle_Enabled = GameConfigs2.AutopilotEnabled;
                RandomSeedTerrain_Config.Config_Value = GameConfigs2.RandomSeed_Terrain;
                RandomSeedAutopilot_Config.Config_Value = GameConfigs2.RandomSeed_Autopilot;
                RandomSeedSpecials_Config.Config_Value = GameConfigs2.RandomSeed_Specials;
                RandomSeedSpecials_SpecialsIndex = GameConfigs2.Specials_Index;
                RandomSeedSpecials_SpecialsInstance = GameConfigs2.Specials_Instance;

                VehicleOpacity_Scrollbar_Sync_Needed = true;
                AutopilotToggle_Scrollbar_Sync_Needed = true;
            }

            GameConfigs_Refresh_Countdown_Seconds -= Time.fixedDeltaTime;

            if (GameConfigs_Refresh_Countdown_Seconds < Time.fixedDeltaTime)
            {
                GameConfigs_Refresh_Countdown_Seconds = GameConfigs_Refresh_Interval_Seconds;

                GameConfigs2.UserIndex = UserIndex_Config.Config_Value;
                GameConfigs2.PresetIndex = PresetIndex_Config.Config_Value;

                if (GameConfigs_StandardApply_Needed)
                {
                    GameConfigs_StandardApply_Needed = false;
                    GameConfigs2.Standard_Apply();

                    VehicleOpacity_Normalized = GameConfigs2.VehicleOpacity;
                    AutopilotToggle_Enabled = GameConfigs2.AutopilotEnabled;
                    RandomSeedTerrain_Config.Config_Value = GameConfigs2.RandomSeed_Terrain;
                    RandomSeedAutopilot_Config.Config_Value = GameConfigs2.RandomSeed_Autopilot;
                    RandomSeedSpecials_Config.Config_Value = GameConfigs2.RandomSeed_Specials;

                    VehicleOpacity_Scrollbar_Sync_Needed = true;
                    AutopilotToggle_Scrollbar_Sync_Needed = true;
                }
                else
                {
                    GameConfigs2.VehicleOpacity = VehicleOpacity_Normalized;
                    GameConfigs2.AutopilotEnabled = AutopilotToggle_Enabled;
                    GameConfigs2.RandomSeed_Terrain = RandomSeedTerrain_Config.Config_Value;
                    GameConfigs2.RandomSeed_Autopilot = RandomSeedAutopilot_Config.Config_Value;
                    GameConfigs2.RandomSeed_Specials = RandomSeedSpecials_Config.Config_Value;
                    GameConfigs2.Specials_Index = RandomSeedSpecials_SpecialsIndex;
                    GameConfigs2.Specials_Instance = RandomSeedSpecials_SpecialsInstance;
                }
            }
        } // end method
        // end helpers GameConfigs

        // begin helpers Start
        private void Start_Setup()
        {
            Start_Button = Start_Button_Object.GetComponent<Scrollbar>();
            Start_Button.onValueChanged.AddListener(Start_Button_OnClick);
        }

        private void Start_TearDown()
        {
            Start_Button.onValueChanged.RemoveListener(Start_Button_OnClick);
        }

        private void Start_Button_OnClick(float Value)
        {
            SceneManager.LoadScene(Start_SceneName);
        }
        // end helpers Start

        // begin helpers Exit
        private void Exit_Setup()
        {
            Exit_Button = Exit_Button_Object.GetComponent<Scrollbar>();
            Exit_Button.onValueChanged.AddListener(Exit_Button_OnClick);
        }

        private void Exit_TearDown()
        {
            Exit_Button.onValueChanged.RemoveListener(Exit_Button_OnClick);
        }

        private void Exit_Button_OnClick(float Value)
        {
            Application.Quit();
        }
        // end helpers Exit

        // begin helpers UserIndex
        private void UserIndex_Setup()
        {
            UserIndex_Config = UserIndex_Config_Object.GetComponent<UI_Config_Int_Custom1>();
            UserIndex_Config.Config_Value_OnChanged.AddListener(UserIndex_Value_OnChanged);
            UserIndex_Config.Config_Refresh_OnClicked.AddListener(UserIndex_Refresh_OnClicked);
        }

        private void UserIndex_Refresh()
        {
            UserIndex_Refresh_Countdown_Seconds -= Time.fixedDeltaTime;

            if (UserIndex_Refresh_Countdown_Seconds < Time.fixedDeltaTime)
            {
                UserIndex_Refresh_Countdown_Seconds = UserIndex_Refresh_Interval_Seconds;
            }
        }

        private void UserIndex_TearDown()
        {
            UserIndex_Config.Config_Value_OnChanged.RemoveListener(UserIndex_Value_OnChanged);
            UserIndex_Config.Config_Refresh_OnClicked.RemoveListener(UserIndex_Refresh_OnClicked);
        }

        private void UserIndex_Value_OnChanged(float Value)
        {
            GameConfigs_StandardApply_Needed = true;
        }

        private void UserIndex_Refresh_OnClicked(float Value)
        {
            // Do nothing.
        }
        // end helpers UserIndex

        // begin helpers PresetIndex
        private void PresetIndex_Setup()
        {
            PresetIndex_Config = PresetIndex_Config_Object.GetComponent<UI_Config_Int_Custom1>();
            PresetIndex_Config.Config_Value_OnChanged.AddListener(PresetIndex_Value_OnChanged);
            PresetIndex_Config.Config_Refresh_OnClicked.AddListener(PresetIndex_Refresh_OnClicked);
        }

        private void PresetIndex_Refresh()
        {
            PresetIndex_Refresh_Countdown_Seconds -= Time.fixedDeltaTime;

            if (PresetIndex_Refresh_Countdown_Seconds < Time.fixedDeltaTime)
            {
                PresetIndex_Refresh_Countdown_Seconds = PresetIndex_Refresh_Interval_Seconds;
            }
        }

        private void PresetIndex_TearDown()
        {
            PresetIndex_Config.Config_Value_OnChanged.RemoveListener(PresetIndex_Value_OnChanged);
            PresetIndex_Config.Config_Refresh_OnClicked.RemoveListener(PresetIndex_Refresh_OnClicked);
        }

        private void PresetIndex_Value_OnChanged(float Value)
        {
            GameConfigs_StandardApply_Needed = true;
        }

        private void PresetIndex_Refresh_OnClicked(float Value)
        {
            // Do nothing.
        }
        // end helpers PresetIndex

        // begin helpers VehicleOpacity
        private void VehicleOpacity_Setup()
        {
            VehicleOpacity_Text = VehicleOpacity_Text_Object.GetComponent<Text>();
            VehicleOpacity_Scrollbar = VehicleOpacity_Scrollbar_Object.GetComponent<Scrollbar>();
            VehicleOpacity_Scrollbar.onValueChanged.AddListener(VehicleOpacity_Scrollbar_OnValueChanged);
            VehicleOpacity_Reset(VehicleOpacity_Normalized);
        }

        private void VehicleOpacity_Refresh()
        {
            VehicleOpacity_Refresh_Countdown_Seconds -= Time.fixedDeltaTime;

            if (VehicleOpacity_Refresh_Countdown_Seconds < Time.fixedDeltaTime)
            {
                VehicleOpacity_Refresh_Countdown_Seconds = VehicleOpacity_Refresh_Interval_Seconds;

                if (VehicleOpacity_Scrollbar_Sync_Needed)
                {
                    VehicleOpacity_Scrollbar_Sync_Needed = false;
                    VehicleOpacity_Scrollbar_Sync();
                }

                VehicleOpacity_Percent = VehicleOpacity_Normalized * 100f;
                VehicleOpacity_Text.text = $"Vehicle opacity | 载具不透明度：{VehicleOpacity_Percent:000} %";
            }
        }

        private void VehicleOpacity_TearDown()
        {
            VehicleOpacity_Scrollbar.onValueChanged.RemoveListener(VehicleOpacity_Scrollbar_OnValueChanged);
            VehicleOpacity_Reset(1f);
        }

        private void VehicleOpacity_Scrollbar_Sync()
        {
            VehicleOpacity_Percent = VehicleOpacity_Normalized * 100f;
            VehicleOpacity_Scrollbar.value = VehicleOpacity_Normalized;
        }

        private void VehicleOpacity_Scrollbar_OnValueChanged(float Value)
        {
            VehicleOpacity_Reset(Value);
        }

        private void VehicleOpacity_Reset(float Value)
        {
            VehicleOpacity_Normalized = Value;
        }
        // end helpers VehicleOpacity

        // begin helpers AutopilotToggle
        private void AutopilotToggle_Setup()
        {
            AutopilotToggle_Text = AutopilotToggle_Text_Object.GetComponent<Text>();
            AutopilotToggle_Scrollbar = AutopilotToggle_Scrollbar_Object.GetComponent<Scrollbar>();
            AutopilotToggle_Scrollbar.onValueChanged.AddListener(AutopilotToggle_Scrollbar_OnValueChanged);
        }

        private void AutopilotToggle_Refresh()
        {
            AutopilotToggle_Refresh_Countdown_Seconds -= Time.fixedDeltaTime;

            if (AutopilotToggle_Refresh_Countdown_Seconds < Time.fixedDeltaTime)
            {
                AutopilotToggle_Refresh_Countdown_Seconds = AutopilotToggle_Refresh_Interval_Seconds;

                if (AutopilotToggle_Scrollbar_Sync_Needed)
                {
                    AutopilotToggle_Scrollbar_Sync_Needed = false;
                    AutopilotToggle_Scrollbar_Sync();
                }

                if (AutopilotToggle_Normalized < AutopilotToggle_EnableThreshold)
                {
                    AutopilotToggle_Enabled = false;
                }
                else
                {
                    AutopilotToggle_Enabled = true;
                }

                AutopilotToggle_Text.text = $"Autopilot enabled | 启用自动驾驶：{AutopilotToggle_Enabled}";
            }
        }

        private void AutopilotToggle_TearDown()
        {
            AutopilotToggle_Scrollbar.onValueChanged.RemoveListener(AutopilotToggle_Scrollbar_OnValueChanged);
        }

        private void AutopilotToggle_Scrollbar_Sync()
        {
            if (AutopilotToggle_Enabled)
            {
                AutopilotToggle_Normalized = 1f;
            }
            else
            {
                AutopilotToggle_Normalized = 0f;
            }

            AutopilotToggle_Scrollbar.value = AutopilotToggle_Normalized;
        }

        private void AutopilotToggle_Scrollbar_OnValueChanged(float Value)
        {
            AutopilotToggle_Normalized = Value;
        }
        // end helpers AutopilotToggle

        // begin helpers RandomSeedTerrain
        private void RandomSeedTerrain_Setup()
        {
            RandomSeedTerrain_Config = RandomSeedTerrain_Config_Object.GetComponent<UI_Config_Int_Custom1>();
            RandomSeedTerrain_Config.Config_Value_OnChanged.AddListener(RandomSeedTerrain_Value_OnChanged);
            RandomSeedTerrain_Config.Config_Refresh_OnClicked.AddListener(RandomSeedTerrain_Refresh_OnClicked);
        }

        private void RandomSeedTerrain_Refresh()
        {
            RandomSeedTerrain_Refresh_Countdown_Seconds -= Time.fixedDeltaTime;

            if (RandomSeedTerrain_Refresh_Countdown_Seconds < Time.fixedDeltaTime)
            {
                RandomSeedTerrain_Refresh_Countdown_Seconds = RandomSeedTerrain_Refresh_Interval_Seconds;
            }
        }

        private void RandomSeedTerrain_TearDown()
        {
            RandomSeedTerrain_Config.Config_Value_OnChanged.RemoveListener(RandomSeedTerrain_Value_OnChanged);
            RandomSeedTerrain_Config.Config_Refresh_OnClicked.RemoveListener(RandomSeedTerrain_Refresh_OnClicked);
        }

        private void RandomSeedTerrain_Value_OnChanged(float Value)
        {
            // Do nothing.
        }

        private void RandomSeedTerrain_Refresh_OnClicked(float Value)
        {
            // Do nothing.
        }
        // end helpers RandomSeedAutopilot

        // begin helpers RandomSeedAutopilot
        private void RandomSeedAutopilot_Setup()
        {
            RandomSeedAutopilot_Config = RandomSeedAutopilot_Config_Object.GetComponent<UI_Config_Int_Custom1>();
            RandomSeedAutopilot_Config.Config_Value_OnChanged.AddListener(RandomSeedAutopilot_Value_OnChanged);
        }

        private void RandomSeedAutopilot_Refresh()
        {
            RandomSeedAutopilot_Refresh_Countdown_Seconds -= Time.fixedDeltaTime;

            if (RandomSeedAutopilot_Refresh_Countdown_Seconds < Time.fixedDeltaTime)
            {
                RandomSeedAutopilot_Refresh_Countdown_Seconds = RandomSeedAutopilot_Refresh_Interval_Seconds;
            }
        }

        private void RandomSeedAutopilot_TearDown()
        {
            RandomSeedAutopilot_Config.Config_Value_OnChanged.RemoveListener(RandomSeedAutopilot_Value_OnChanged);
        }

        private void RandomSeedAutopilot_Value_OnChanged(float Value)
        {
            // Do nothing.
        }
        // end helpers RandomSeedAutopilot

        // begin helpers RandomSeedSpecials
        private void RandomSeedSpecials_Setup()
        {
            RandomSeedSpecials_Config = RandomSeedSpecials_Config_Object.GetComponent<UI_Config_Int_Custom1>();
            RandomSeedSpecials_Config.Config_Value_OnChanged.AddListener(RandomSeedSpecials_Value_OnChanged);
            RandomSeedSpecials_Index_AndInstance_Generate();
        }

        private void RandomSeedSpecials_Refresh()
        {
            RandomSeedSpecials_Refresh_Countdown_Seconds -= Time.fixedDeltaTime;

            if (RandomSeedSpecials_Refresh_Countdown_Seconds < Time.fixedDeltaTime)
            {
                RandomSeedSpecials_Refresh_Countdown_Seconds = RandomSeedSpecials_Refresh_Interval_Seconds;
            }
        }

        private void RandomSeedSpecials_TearDown()
        {
            RandomSeedSpecials_Config.Config_Value_OnChanged.RemoveListener(RandomSeedSpecials_Value_OnChanged);
        }

        private void RandomSeedSpecials_Value_OnChanged(float Value)
        {
            RandomSeedSpecials_Index_AndInstance_Generate();
        }

        private void RandomSeedSpecials_Index_AndInstance_Generate()
        {
            RandomSeedSpecials_RandomState_Restore();
            RandomSeedSpecials_SpecialsIndex = Random.Range(0, Recorder25_.Record.SpecialsStandards.standards.Count);
            RandomSeedSpecials_RandomState_Backup();
            RandomSeedSpecials_SpecialsInstance = Recorder25_.Record.SpecialsStandards.standards[RandomSeedSpecials_SpecialsIndex];
        }

        private void RandomSeedSpecials_RandomState_Restore()
        {
            if (!RandomSeedSpecials_Random_Initialized)
            {
                RandomSeedSpecials_Random_Initialized = true;
                Random.InitState(RandomSeedSpecials_Config.Config_Value);
                RandomSeedSpecials_Random_State = Random.state;
            }
            else
            {
                Random.state = RandomSeedSpecials_Random_State;
            }
        }

        private void RandomSeedSpecials_RandomState_Backup()
        {
            RandomSeedSpecials_Random_State = Random.state;
        }
        // end helpers RandomSeedSpecials
    } // end class
} // end namespace

// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Random = UnityEngine.Random;


namespace ViSCARecorder
{
    public class GamePlay4 : MonoBehaviour
    {
        // begin public fields
        public GameObject Recorder_Object;
        public GameObject TerrainGenerator_Object;
        public GameObject Vehicle_Object;
        public GameObject Vehicle_Boundary_Down_Object;
        public GameObject Autopilot_Object;

        public GameObject Restart_Button_Object;
        public GameObject Exit_Button_Object;

        public GameObject UserIndex_Config_Object;
        public GameObject PresetIndex_Config_Object;

        public GameObject VehicleOpacity_Text_Object;
        public GameObject VehicleOpacity_Scrollbar_Object;
        public List<Material> VehicleOpacity_Materials = new();

        public GameObject AutopilotToggle_Text_Object;
        public GameObject AutopilotToggle_Scrollbar_Object;

        public GameObject RandomSeedTerrain_Config_Object;
        public GameObject RandomSeedAutopilot_Config_Object;
        public GameObject RandomSeedSpecials_Config_Object;

        public GameObject AutopilotStatus_TextObject;

        public GameObject Exit2_Button_Object;
        // end public fields

        // begin private fields
        private GameConfigs2_.GameConfigs_Backup GameConfigs_Backup_;
        private bool GameConfigs_Initialized = false;
        private float GameConfigs_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float GameConfigs_Refresh_Countdown_Seconds = 0f;
        private bool GameConfigs_StandardApply_Needed = false;

        private Recorder25 Recorder_;
        private float Recorder_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float Recorder_Refresh_Countdown_Seconds = 0f;

        private TerrainGenerator2 TerrainGenerator;
        private float TerrainGenerator_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float TerrainGenerator_Refresh_Countdown_Seconds = 0f;
        private bool TerrainGenerator_Generate_Needed = true;

        private Vehicle4 Vehicle_;
        private float Vehicle_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float Vehicle_Refresh_Countdown_Seconds = 0f;
        private bool Vehicle_PositionY_Reset_Needed = true;
        private float Vehicle_PositionY_AboveTerrain = 1f + 10f;

        private Autopilot3 Autopilot;
        private float Autopilot_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float Autopilot_Refresh_Countdown_Seconds = 0f;
        private bool Autopilot_RandomSeedAutopilot_Reset_Needed = true;
        private bool Autopilot_RandomSpecialsIndex_Reset_Needed = true;

        private Scrollbar Restart_Button;

        private Scrollbar Exit_Button;
        private string Exit_SceneName = "lab-1_menu";

        private UI_Config_Int_Custom1 UserIndex_Config;
        private float UserIndex_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float UserIndex_Refresh_Countdown_Seconds = 0f;

        private UI_Config_Int_Custom1 PresetIndex_Config;
        private float PresetIndex_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float PresetIndex_Refresh_Countdown_Seconds = 0f;

        private Text VehicleOpacity_Text;
        private Scrollbar VehicleOpacity_Scrollbar;
        private List<Color> VehicleOpacity_MaterialColors_Original = new();
        private List<Color> VehicleOpacity_MaterialColors_Current = new();
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

        private Text AutopilotStatus_Text;
        private float AutopilotStatus_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float AutopilotStatus_Refresh_Countdown_Seconds = 0f;
        private bool AutopilotStatus_Enabled = false;
        private Color AutopilotStatus_EnabledColor = new(0f, 1f, 0f, 1f);
        private Color AutopilotStatus_DisabledColor = new(1f, 0f, 0f, 1f);

        private Scrollbar Exit2_Button;
        // end private fields

        // begin MonoBehaviour callbacks
        void Start()
        {
            GameConfigs_Setup();
            Recorder_Setup();
            TerrainGenerator_Setup();
            Vehicle_Setup();
            Autopilot_Setup();
            Restart_Setup();
            Exit_Setup();
            UserIndex_Setup();
            PresetIndex_Setup();
            VehicleOpacity_Setup();
            AutopilotToggle_Setup();
            RandomSeedTerrain_Setup();
            RandomSeedAutopilot_Setup();
            RandomSeedSpecials_Setup();
            AutopilotStatus_Setup();
            Exit2_Setup();
        }

        void FixedUpdate()
        {
            GameConfigs_Refresh();
            Recorder_Refresh();
            TerrainGenerator_Refresh();
            Vehicle_Refresh();
            Autopilot_Refresh();
            UserIndex_Refresh();
            PresetIndex_Refresh();
            VehicleOpacity_Refresh();
            AutopilotToggle_Refresh();
            RandomSeedTerrain_Refresh();
            RandomSeedAutopilot_Refresh();
            RandomSeedSpecials_Refresh();
            AutopilotStatus_Refresh();
        }

        void OnDestroy()
        {
            Restart_TearDown();
            Exit_TearDown();
            UserIndex_TearDown();
            PresetIndex_TearDown();
            VehicleOpacity_TearDown();
            AutopilotToggle_TearDown();
            RandomSeedTerrain_TearDown();
            RandomSeedAutopilot_TearDown();
            RandomSeedSpecials_TearDown();
            Exit2_TearDown();
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
                    
                    TerrainGenerator_Generate_Needed = true;
                    Autopilot_RandomSeedAutopilot_Reset_Needed = true;
                    Autopilot_RandomSpecialsIndex_Reset_Needed = true;
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
        }
        // end helpers GameConfigs

        // begin helpers Recorder
        private void Recorder_Setup()
        {
            Recorder_ = Recorder_Object.GetComponent<Recorder25>();
        }

        private void Recorder_Refresh()
        {
            Recorder_Refresh_Countdown_Seconds -= Time.fixedDeltaTime;

            if (Recorder_Refresh_Countdown_Seconds < Time.fixedDeltaTime)
            {
                Recorder_Refresh_Countdown_Seconds = Recorder_Refresh_Interval_Seconds;

                if (Recorder_.record_Current != null)
                {
                    Recorder_.record_Current.game_configs.user_index = GameConfigs2.UserIndex;
                    Recorder_.record_Current.game_configs.preset_index = GameConfigs2.PresetIndex;
                    Recorder_.record_Current.game_configs.vehicle_opacity = GameConfigs2.VehicleOpacity;
                    Recorder_.record_Current.game_configs.autopilot_enabled = GameConfigs2.AutopilotEnabled;
                    Recorder_.record_Current.game_configs.random_seed_terrain = GameConfigs2.RandomSeed_Terrain;
                    Recorder_.record_Current.game_configs.random_seed_autopilot = GameConfigs2.RandomSeed_Autopilot;
                    Recorder_.record_Current.game_configs.random_seed_specials = GameConfigs2.RandomSeed_Specials;
                    Recorder_.record_Current.game_configs.specials_index = GameConfigs2.Specials_Index;

                    if (GameConfigs2.Specials_Instance != null)
                    {
                        Recorder_.record_Current.game_configs.specials = new(GameConfigs2.Specials_Instance);
                    }

                    Recorder_.record_Current.game_play.scene_name = SceneManager.GetActiveScene().name;
                    Recorder_.record_Current.game_play.autopilot_enabled = Vehicle_.Autopilot_Enabled;
                    Recorder_.record_Current.game_play.locomotion.spatial_pose.position = Vehicle_.Vehicle_RigidBody_Object.transform.position;
                    Recorder_.record_Current.game_play.locomotion.spatial_pose.rotation = Vehicle_.Vehicle_RigidBody_Object.transform.rotation.eulerAngles;
                    Recorder_.record_Current.game_play.locomotion.combined_input = Vehicle_.Input_Combined;
                    Recorder_.record_Current.game_play.locomotion.active_input.enabled = !Vehicle_.Autopilot_Enabled;
                    Recorder_.record_Current.game_play.locomotion.active_input.input_value = Vehicle_.Input_DriveManual;
                    Recorder_.record_Current.game_play.locomotion.passive_input.enabled = Vehicle_.Autopilot_Enabled;
                    Recorder_.record_Current.game_play.locomotion.passive_input.input_value = Vehicle_.Input_Autopilot;
                }
            }
        }
        // end helpers Recorder

        // begin helpers TerrainGenerator
        private void TerrainGenerator_Setup()
        {
            TerrainGenerator = TerrainGenerator_Object.GetComponent<TerrainGenerator2>();
        }

        private void TerrainGenerator_Refresh()
        {
            TerrainGenerator_Refresh_Countdown_Seconds -= Time.fixedDeltaTime;

            if (TerrainGenerator_Refresh_Countdown_Seconds < Time.fixedDeltaTime)
            {
                TerrainGenerator_Refresh_Countdown_Seconds = TerrainGenerator_Refresh_Interval_Seconds;

                if (TerrainGenerator_Generate_Needed)
                {
                    TerrainGenerator_Generate_Needed = false;
                    TerrainGenerator.Random_Seed_Reset(RandomSeedTerrain_Config.Config_Value);
                    TerrainGenerator.Terrain_AndNature_Generate();
                    Vehicle_PositionY_Reset_Needed = true;
                }
            }
        }
        // end helpers TerrainGenerator

        // begin helpers Vehicle
        private void Vehicle_Setup()
        {
            Vehicle_ = Vehicle_Object.GetComponent<Vehicle4>();
        }

        private void Vehicle_Refresh()
        {
            Vehicle_Refresh_Countdown_Seconds -= Time.fixedDeltaTime;

            if (Vehicle_Refresh_Countdown_Seconds < Time.fixedDeltaTime)
            {
                Vehicle_Refresh_Countdown_Seconds = Vehicle_Refresh_Interval_Seconds;
                Vehicle_.Autopilot_Enabled = AutopilotToggle_Enabled;

                if (Vehicle_PositionY_Reset_Needed)
                {
                    Vehicle_PositionY_Reset_Needed = false;
                    Vehicle_PositionY_Reset();
                }
            }
        } // end method

        private void Vehicle_PositionY_Reset()
        {
            Vector3 Position_Boundary_Down = Vehicle_Boundary_Down_Object.transform.position;

            Vector3 Position_BoundaryDown_RelativeToVehicle =
                Vehicle_Boundary_Down_Object.transform.position
                - Vehicle_Object.transform.position
            ;

            Vector3 Position_Target = Position_Boundary_Down;
            Position_Target.y = Vehicle_PositionY_AboveTerrain;
            RaycastHit RaycastHit_;

            if (
                Physics.Raycast(Position_Target, Vector3.down, out RaycastHit_)
                && (
                    RaycastHit_.collider.CompareTag("terrain")
                    || RaycastHit_.collider.CompareTag("water")
                    || RaycastHit_.collider.CompareTag("walls")
                    || RaycastHit_.collider.CompareTag("ground-base")
                )
            ) 
            {
                Position_Target = RaycastHit_.point;
                Position_Target -= Position_BoundaryDown_RelativeToVehicle;
                Position_Target.y += 0.25f;
            }

            Vehicle_Object.transform.position = Position_Target;
            Physics.SyncTransforms();
        }
        // end helpers Vehicle

        // begin helpers Autopilot
        private void Autopilot_Setup()
        {
            Autopilot = Autopilot_Object.GetComponent<Autopilot3>();
        }

        private void Autopilot_Refresh()
        {
            Autopilot_Refresh_Countdown_Seconds -= Time.fixedDeltaTime;

            if (Autopilot_Refresh_Countdown_Seconds < Time.fixedDeltaTime)
            {
                Autopilot_Refresh_Countdown_Seconds = Autopilot_Refresh_Interval_Seconds;

                if (Autopilot_RandomSeedAutopilot_Reset_Needed)
                {
                    Autopilot_RandomSeedAutopilot_Reset_Needed = false;
                    Autopilot.Random_Autopilot_Seed_Reset(RandomSeedAutopilot_Config.Config_Value);
                }

                if (Autopilot_RandomSpecialsIndex_Reset_Needed)
                {
                    Autopilot_RandomSpecialsIndex_Reset_Needed = false;
                    Autopilot.Specials_Index_Reset(RandomSeedSpecials_SpecialsIndex);
                } // end if
            } // end if
        } // end method
        // end helpers Autopilot

        // begin helpers Restart
        private void Restart_Setup()
        {
            Restart_Button = Restart_Button_Object.GetComponent<Scrollbar>();
            Restart_Button.onValueChanged.AddListener(Restart_Button_OnClick);
        }

        private void Restart_TearDown()
        {
            Restart_Button.onValueChanged.RemoveListener(Restart_Button_OnClick);
        }

        private void Restart_Button_OnClick(float Value)
        {
            if (Recorder_Object.activeInHierarchy)
            {
                Recorder_.Recording_Exit();
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        // end helpers Restart

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
            if (Recorder_Object.activeInHierarchy)
            {
                Recorder_.Recording_Exit();
            }

            SceneManager.LoadScene(Exit_SceneName);
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
            // Do nothing.
        }

        private void UserIndex_Refresh_OnClicked(float Value)
        {
            GameConfigs_StandardApply_Needed = true;
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
            // Do nothing.
        }

        private void PresetIndex_Refresh_OnClicked(float Value)
        {
            GameConfigs_StandardApply_Needed = true;
        }
        // end helpers PresetIndex

        // begin helpers VehicleOpacity
        private void VehicleOpacity_Setup()
        {
            VehicleOpacity_Text = VehicleOpacity_Text_Object.GetComponent<Text>();
            VehicleOpacity_Scrollbar = VehicleOpacity_Scrollbar_Object.GetComponent<Scrollbar>();

            foreach (Material Material_ in VehicleOpacity_Materials)
            {
                VehicleOpacity_MaterialColors_Original.Add(Material_.color);
                VehicleOpacity_MaterialColors_Current.Add(Material_.color);
            }

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

            for (int Index_ = 0; Index_ < VehicleOpacity_Materials.Count; Index_ += 1)
            {
                Color Color_ = VehicleOpacity_MaterialColors_Original[Index_];
                Color_.a *= VehicleOpacity_Normalized;
                VehicleOpacity_Materials[Index_].color = Color_;
            }
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
            TerrainGenerator_Generate_Needed = true;
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
            Autopilot_RandomSeedAutopilot_Reset_Needed = true;
        }
        // end helpers RandomSeedAutopilot

        // begin helpers RandomSeedSpecials
        private void RandomSeedSpecials_Setup()
        {
            RandomSeedSpecials_Config = RandomSeedSpecials_Config_Object.GetComponent<UI_Config_Int_Custom1>();
            RandomSeedSpecials_Config.Config_Value_OnChanged.AddListener(RandomSeedSpecials_Value_OnChanged);
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
            Autopilot_RandomSpecialsIndex_Reset_Needed = true;
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

        // begin helpers AutopilotStatus
        private void AutopilotStatus_Setup()
        {
            AutopilotStatus_Text = AutopilotStatus_TextObject.GetComponent<Text>();
        }

        private void AutopilotStatus_Refresh()
        {
            AutopilotStatus_Refresh_Countdown_Seconds -= Time.fixedDeltaTime;

            if (AutopilotStatus_Refresh_Countdown_Seconds < Time.fixedDeltaTime)
            {
                AutopilotStatus_Refresh_Countdown_Seconds = AutopilotStatus_Refresh_Interval_Seconds;
                AutopilotStatus_Enabled = Vehicle_.Autopilot_Enabled;
                Color Color_;

                if (AutopilotStatus_Enabled)
                {
                    Color_ = AutopilotStatus_EnabledColor;
                }
                else
                {
                    Color_ = AutopilotStatus_DisabledColor;
                }

                AutopilotStatus_Text.text = $"Autopilot enabled: {AutopilotStatus_Enabled}";
                AutopilotStatus_Text.color = Color_;
            }
        }
        // end helpers AutopilotStatus

        // begin helpers Exit2
        private void Exit2_Setup()
        {
            Exit2_Button = Exit2_Button_Object.GetComponent<Scrollbar>();
            Exit2_Button.onValueChanged.AddListener(Exit_Button_OnClick);
        }

        private void Exit2_TearDown()
        {
            Exit2_Button.onValueChanged.RemoveListener(Exit_Button_OnClick);
        }
        // end helpers Exit2
    } // end class
} // end namespace

// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ViSCARecorder {
    /** The gameplay script for scene locomotion-2.*/
    public class GamePlay2 : MonoBehaviour
    {
        public GameObject Recorder_Object;
        
        public GameObject Vehicle_Object;
        public GameObject Vehicle_Autopilot_Object;

        public GameObject VehicleOpacity_Text_Object;
        public GameObject VehicleOpacity_Scrollbar_Object;
        public List<Material> VehicleOpacity_Materials = new();

        public GameObject AutopilotToggle_Text_Object;
        public GameObject AutopilotToggle_Scrollbar_Object;

        public GameObject AutopilotStatus_TextObject;


        private Recorder23 Recorder_;
        private float Recorder_RefreshInterval_Seconds = 0.033_333_333f; // 30 Hz.
        private float Recorder_RefreshCountdown_Seconds = 0f;

        private Vehicle2 Vehicle_;
        private Autopilot1 Vehicle_Autopilot;
        private float Vehicle_RefreshInterval_Seconds = 0.033_333_333f; // 30 Hz.
        private float Vehicle_RefreshCountdown_Seconds = 0f;

        private Text VehicleOpacity_Text;
        private Scrollbar VehicleOpacity_Scrollbar;
        private List<Color> VehicleOpacity_MaterialColors_Original = new();
        private List<Color> VehicleOpacity_MaterialColors_Current = new();
        private float VehicleOpacity_RefreshInterval_Seconds = 0.1f; // 10 Hz.
        private float VehicleOpacity_RefreshCountdown_Seconds = 0f;
        private float VehicleOpacity_Normalized = 1f;
        private float VehicleOpacity_Percent = 100f;

        private Text AutopilotToggle_Text;
        private Scrollbar AutopilotToggle_Scrollbar;
        private float AutopilotToggle_RefreshInterval_Seconds = 0.1f; // 10 Hz.
        private float AutopilotToggle_RefreshCountdown_Seconds = 0f;
        private float AutopilotToggle_Normalized = 1f;
        private bool AutopilotToggle_Enabled = true;
        private float AutopilotToggle_EnableThreshold = 0.5f;
        
        private Text AutopilotStatus_Text;
        private float AutopilotStatus_RefreshInterval_Seconds = 0.1f; // 10 Hz.
        private float AutopilotStatus_RefreshCountdown_Seconds = 0f;
        private bool AutopilotStatus_Enabled = false;
        private Color AutopilotStatus_EnabledColor = new(0f, 1f, 0f, 1f);
        private Color AutopilotStatus_DisabledColor = new(1f, 0f, 0f, 1f);


        void Start()
        {
            Recorder_Setup();
            Vehicle_Setup();
            VehicleOpacity_Setup();
            AutopilotToggle_Setup();
            AutopilotStatus_Setup();

        }

        void FixedUpdate()
        {
            Recorder_Refresh();
            Vehicle_Refresh();
            VehicleOpacity_Refresh();
            AutopilotToggle_Refresh();
            AutopilotStatus_Refresh();
        }

        void OnDestroy()
        {
            Recorder_TearDown();
            Vehicle_TearDown();
            VehicleOpacity_TearDown();
            AutopilotToggle_TearDown();
            AutopilotStatus_TearDown();
        }


        private void Recorder_Setup()
        {
            Recorder_ = Recorder_Object.GetComponent<Recorder23>();
        }

        private void Recorder_Refresh()
        {
            Recorder_RefreshCountdown_Seconds -= Time.fixedDeltaTime;

            if (Recorder_RefreshCountdown_Seconds < Time.fixedDeltaTime)
            {
                Recorder_RefreshCountdown_Seconds = Recorder_RefreshInterval_Seconds;

                if (Recorder_.record_Current != null)
                {
                    Recorder_.record_Current.game_play.scene_name = SceneManager.GetActiveScene().name;
                    Recorder_.record_Current.game_play.vehicle_opacity = VehicleOpacity_Normalized;
                    Recorder_.record_Current.game_play.locomotion.spatial_pose.position = Vehicle_.Vehicle_RigidBody_Object.transform.position;
                    Recorder_.record_Current.game_play.locomotion.spatial_pose.rotation = Vehicle_.Vehicle_RigidBody_Object.transform.rotation.eulerAngles;
                    Recorder_.record_Current.game_play.locomotion.passive_input.enabled = Vehicle_.Autopilot_Enabled;
                    Recorder_.record_Current.game_play.locomotion.passive_input.input_value = Vehicle_Autopilot.Output_Autopilot;
                }
            }
        }

        private void Recorder_TearDown()
        {
            // Do nothing.
        }


        private void Vehicle_Setup()
        {
            Vehicle_ = Vehicle_Object.GetComponent<Vehicle2>();
            Vehicle_Autopilot = Vehicle_Autopilot_Object.GetComponent<Autopilot1>();
        }

        private void Vehicle_Refresh()
        {
            Vehicle_RefreshCountdown_Seconds -= Time.fixedDeltaTime;

            if (Vehicle_RefreshCountdown_Seconds < Time.fixedDeltaTime)
            {
                Vehicle_RefreshCountdown_Seconds = Vehicle_RefreshInterval_Seconds;
                Vehicle_.Autopilot_Enabled = AutopilotToggle_Enabled;
            }
        }

        private void Vehicle_TearDown()
        {
            // Do nothing.
        }


        private void VehicleOpacity_Setup()
        {
            VehicleOpacity_Text = VehicleOpacity_Text_Object.GetComponent<Text>();
            VehicleOpacity_Scrollbar = VehicleOpacity_Scrollbar_Object.GetComponent<Scrollbar>();

            foreach (Material Material_ in VehicleOpacity_Materials)
            {
                VehicleOpacity_MaterialColors_Original.Add(Material_.color);
                VehicleOpacity_MaterialColors_Current.Add(Material_.color);
            }

            VehicleOpacity_RefreshValue(1f);
        }

        private void VehicleOpacity_Refresh()
        {
            VehicleOpacity_RefreshCountdown_Seconds -= Time.fixedDeltaTime;

            if (VehicleOpacity_RefreshCountdown_Seconds < Time.fixedDeltaTime)
            {
                VehicleOpacity_RefreshCountdown_Seconds = VehicleOpacity_RefreshInterval_Seconds;
                VehicleOpacity_RefreshValue(VehicleOpacity_Scrollbar.value);
            }
        }

        private void VehicleOpacity_TearDown()
        {
            VehicleOpacity_RefreshValue(1f);
        }

        private void VehicleOpacity_RefreshValue(float Value)
        {
            VehicleOpacity_Normalized = Value;
            VehicleOpacity_Percent = VehicleOpacity_Normalized * 100f;
            VehicleOpacity_Text.text = $"Vehicle opacity | 载具不透明度：{VehicleOpacity_Percent:000} %";

            for (int Index_ = 0; Index_ < VehicleOpacity_Materials.Count; Index_ += 1)
            {
                Color Color_ = VehicleOpacity_MaterialColors_Original[Index_];
                Color_.a *= VehicleOpacity_Normalized;
                VehicleOpacity_Materials[Index_].color = Color_;
            }
        }

        private void AutopilotToggle_Setup()
        {
            AutopilotToggle_Text = AutopilotToggle_Text_Object.GetComponent<Text>();
            AutopilotToggle_Scrollbar = AutopilotToggle_Scrollbar_Object.GetComponent<Scrollbar>();
        }

        private void AutopilotToggle_Refresh()
        {
            AutopilotToggle_RefreshCountdown_Seconds -= Time.fixedDeltaTime;

            if (AutopilotToggle_RefreshCountdown_Seconds < Time.fixedDeltaTime)
            {
                AutopilotToggle_RefreshCountdown_Seconds = AutopilotToggle_RefreshInterval_Seconds;
                AutopilotToggle_Normalized = AutopilotToggle_Scrollbar.value;

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
            // Do nothing.
        }


        private void AutopilotStatus_Setup()
        {
            AutopilotStatus_Text = AutopilotStatus_TextObject.GetComponent<Text>();
        }

        private void AutopilotStatus_Refresh()
        {
            AutopilotStatus_RefreshCountdown_Seconds -= Time.fixedDeltaTime;

            if (AutopilotStatus_RefreshCountdown_Seconds < Time.fixedDeltaTime)
            {
                AutopilotStatus_RefreshCountdown_Seconds = AutopilotStatus_RefreshInterval_Seconds;
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

        private void AutopilotStatus_TearDown()
        {
            // Do nothing.
        }
    } // end class
} // end namespace

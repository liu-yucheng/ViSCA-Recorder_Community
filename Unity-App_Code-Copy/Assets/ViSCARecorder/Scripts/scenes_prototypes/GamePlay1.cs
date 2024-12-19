// Copyright (C) 2024 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ViSCARecorder {
    /** The gameplay script for scene locomotion-1.*/
    public class GamePlay1 : MonoBehaviour
    {
        public GameObject Recorder_Object;

        public GameObject Menu_VehicleOpacity_TextObject;
        public GameObject Menu_VehicleOpacity_ScrollbarObject;
        public List<Material> Menu_VehicleOpacity_Materials = new();

        public GameObject Menu_Autopilot_VehicleObject;
        public GameObject Menu_Autopilot_TextObject;

        private Recorder22 Recorder_;
        private float Recorder_RefreshInterval_Seconds = 0.1f; // 10 Hz.
        private float Recorder_RefreshCountdown_Seconds = 0f;

        private Text Menu_VehicleOpacity_Text;
        private Scrollbar Menu_VehicleOpacity_Scrollbar;
        private List<Color> Menu_VehicleOpacity_MaterialColors_Original = new();
        private List<Color> Menu_VehicleOpacity_MaterialColors_Current = new();
        private float Menu_VehicleOpacity_RefreshInterval_Seconds = 0.1f; // 10 Hz.
        private float Menu_VehicleOpacity_RefreshCountdown_Seconds = 0f;
        private float Menu_VehicleOpacity_Units = 1f;
        private float Menu_VehicleOpacity_PercentPerUnit = 100f;
        private float Menu_VehicleOpacity_Percent = 100f;

        private Vehicle1 Menu_Autopilot_Vehicle;
        private Text Menu_Autopilot_Text;
        private float Menu_Autopilot_RefreshInterval_Seconds = 0.1f; // 10 Hz.
        private float Menu_Autopilot_RefreshCountdown_Seconds = 0f;
        private bool Menu_Autopilot_Enabled = false;
        private Color Menu_Autopilot_EnabledColor = new(0f, 1f, 0f, 1f);
        private Color Menu_Autopilot_DisabledColor = new(1f, 0f, 0f, 1f);

        void Start()
        {
            Recorder_Setup();
            Menu_VehicleOpacity_Setup();
            Menu_Autopilot_Setup();

        }

        void FixedUpdate()
        {
            Recorder_Refresh();
            Menu_VehicleOpacity_Refresh();
            Menu_Autopilot_Refresh();
        }

        void OnDestroy()
        {
            Recorder_TearDown();
            Menu_VehicleOpacity_TearDown();
            Menu_Autopilot_TearDown();
        }


        private void Recorder_Setup()
        {
            Recorder_ = Recorder_Object.GetComponent<Recorder22>();
        }

        private void Recorder_Refresh()
        {
            Recorder_RefreshCountdown_Seconds -= Time.fixedDeltaTime;

            if (Recorder_RefreshCountdown_Seconds < Time.fixedDeltaTime)
            {
                Recorder_RefreshCountdown_Seconds = Recorder_RefreshInterval_Seconds;
                string SceneName = SceneManager.GetActiveScene().name;

                Recorder_RefreshRecord(
                    SceneName,
                    Menu_VehicleOpacity_Units,
                    ref Recorder_
                );
            }
        }

        private void Recorder_TearDown()
        {
            // Do nothing.
        }

        private void Recorder_RefreshRecord(
            in string SceneName,
            in float VehicleOpacity,
            ref Recorder22 Recorder_
        )
        {
            if (Recorder_.record_Current != null)
            {
                Recorder_.record_Current.game_play.scene_name = SceneName;
                Recorder_.record_Current.game_play.vehicle_opacity = VehicleOpacity;
            }
        }


        private void Menu_VehicleOpacity_Setup()
        {
            Menu_VehicleOpacity_Text = Menu_VehicleOpacity_TextObject.GetComponent<Text>();
            Menu_VehicleOpacity_Scrollbar = Menu_VehicleOpacity_ScrollbarObject.GetComponent<Scrollbar>();

            foreach (Material Material_ in Menu_VehicleOpacity_Materials)
            {
                Menu_VehicleOpacity_MaterialColors_Original.Add(Material_.color);
                Menu_VehicleOpacity_MaterialColors_Current.Add(Material_.color);
            }

            Menu_VehicleOpacity_RefreshValue(1f);
        }

        private void Menu_VehicleOpacity_Refresh()
        {
            Menu_VehicleOpacity_RefreshCountdown_Seconds -= Time.fixedDeltaTime;

            if (Menu_VehicleOpacity_RefreshCountdown_Seconds < Time.fixedDeltaTime)
            {
                Menu_VehicleOpacity_RefreshCountdown_Seconds = Menu_VehicleOpacity_RefreshInterval_Seconds;
                Menu_VehicleOpacity_RefreshValue(Menu_VehicleOpacity_Scrollbar.value);
            }
        }

        private void Menu_VehicleOpacity_TearDown()
        {
            Menu_VehicleOpacity_RefreshValue(1f);
        }

        private void Menu_VehicleOpacity_RefreshValue(float Value)
        {
            Menu_VehicleOpacity_Units = Value;
            Menu_VehicleOpacity_Percent = Menu_VehicleOpacity_Units * Menu_VehicleOpacity_PercentPerUnit;
            Menu_VehicleOpacity_Text.text = $"Vehicle opacity | 载具不透明度：{Menu_VehicleOpacity_Percent:000} %";

            for (int Index_ = 0; Index_ < Menu_VehicleOpacity_Materials.Count; Index_ += 1)
            {
                Color Color_ = Menu_VehicleOpacity_MaterialColors_Original[Index_];
                Color_.a *= Menu_VehicleOpacity_Units;
                Menu_VehicleOpacity_Materials[Index_].color = Color_;
            }
        }

        private void Menu_Autopilot_Setup()
        {
            Menu_Autopilot_Vehicle = Menu_Autopilot_VehicleObject.GetComponent<Vehicle1>();
            Menu_Autopilot_Text = Menu_Autopilot_TextObject.GetComponent<Text>();
        }

        private void Menu_Autopilot_Refresh()
        {
            Menu_Autopilot_RefreshCountdown_Seconds -= Time.fixedDeltaTime;

            if (Menu_Autopilot_RefreshCountdown_Seconds < Time.fixedDeltaTime)
            {
                Menu_Autopilot_RefreshCountdown_Seconds = Menu_Autopilot_RefreshInterval_Seconds;
                Menu_Autopilot_Enabled = Menu_Autopilot_Vehicle.Input_AutoPilot_Enabled;
                Color Color_;

                if (Menu_Autopilot_Enabled)
                {
                    Color_ = Menu_Autopilot_EnabledColor;
                }
                else
                {
                    Color_ = Menu_Autopilot_DisabledColor;
                }

                Menu_Autopilot_Text.text = $"Autopilot enabled: {Menu_Autopilot_Enabled}";
                Menu_Autopilot_Text.color = Color_;
            }
        }

        private void Menu_Autopilot_TearDown()
        {
            // Do nothing.
        }
    } // end class
} // end namespace

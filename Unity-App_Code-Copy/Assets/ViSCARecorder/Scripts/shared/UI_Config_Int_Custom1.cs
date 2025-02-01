// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

// Begin References.
//
// Reference 1: Package UnityEngine.UI, Scrollbar.cs.
// Reference 2: Package UnityEngine.UI, Button.cs.
//
// End References.

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ViSCARecorder
{
    public class UI_Config_Int_Custom1 : MonoBehaviour
    {
        // Begin public fields.
        /// <summary>
        /// Configuration value range.
        /// Closed on both minimum and maximum sides.
        /// </summary>
        public Vector2Int Config_Value_Range = new(0, 99);
        
        public int Config_Value
        {
            get {
                return Config_Value_;
            }

            set
            {
                bool OnValueChanged_InvokeNeeded = value != Config_Value_;
                Config_Value_ = value;
                Config_Value_ = Mathf.Clamp(Config_Value_, Config_Value_Range.x, Config_Value_Range.y);

                if (OnValueChanged_InvokeNeeded)
                {
                    Config_ToValuesNormalized_FromValues_Sync();
                    Config_Value_ScrollbarSync_Needed = true;
                    Config_ToValuesToRefresh_FromValues_Sync();
                    Config_Value_OnChanged.Invoke(value);
                }
            }
        }

        public UnityEvent<float> Config_Value_OnChanged = new();
        public UnityEvent<float> Config_Refresh_OnClicked = new();

        public string UI_Text_Prefix = "[UI_Config_Int]";
        public GameObject UI_Text_Object;
        public GameObject UI_Scrollbar_Object;
        public bool UI_Scrollbar_Enabled = true;
        public GameObject UI_Button_Minus10_Object;
        public bool UI_Button_Minus10_Enabled = true;
        public GameObject UI_Button_Minus1_Object;
        public bool UI_Button_Minus1_Enabled = true;
        public GameObject UI_Button_Plus1_Object;
        public bool UI_Button_Plus1_Enabled = true;
        public GameObject UI_Button_Plus10_Object;
        public bool UI_Button_Plus10_Enabled = true;
        public GameObject UI_Button_Refresh_Object;
        public bool UI_Button_Refresh_Enabled = true;
        // End public fields.

        // Begin private fields.
        private float Config_Refresh_Interval = 0.033_333_333f; // 30 Hz.
        private float Config_Refresh_Countdown = 0f;
        private int Config_Value_Range_Length = 100;
        private int Config_Value_ = 0;
        private float Config_Value_Normalized = 0f;
        private int Config_Value_ToRefresh = 0;
        private float Config_Value_ToRefresh_Normalized = 0f;
        private bool Config_Value_ScrollbarSync_Needed = false;
        private Vector2 Config_Value_UISize_Range = new(0.2f, 1f);
        private float Config_Value_UISize = 0.2f;

        private float UI_Refresh_Interval = 0.033_333_333f; // 30 Hz.
        private float UI_Refresh_Countdown = 0f;
        private Text UI_Text;
        private Scrollbar UI_Scrollbar;
        private Scrollbar UI_Button_Minus10;
        private Scrollbar UI_Button_Minus1;
        private Scrollbar UI_Button_Plus1;
        private Scrollbar UI_Button_Plus10;
        private Scrollbar UI_Button_Refresh;
        // End private fields.

        // Begin callbacks MonoBehaviour.
        void Start()
        {
            Config_Setup();
            UI_Setup();
        }

        void FixedUpdate()
        {
            Config_Refresh();
            UI_Refresh();
        }

        void OnDestroy()
        {
            Config_TearDown();
            UI_TearDown();
        }
        // End callbacks MonoBehaviour.

        // Begin helpers config.
        private void Config_Setup()
        {
            // Do nothing.
        }

        private void Config_Refresh()
        {
            Config_Refresh_Countdown -= Time.fixedDeltaTime;

            if (Config_Refresh_Countdown < Time.fixedDeltaTime)
            {
                Config_Refresh_Countdown = Config_Refresh_Interval;

                if (Config_Value_Range.y < Config_Value_Range.x)
                {
                    Config_Value_Range = new(Config_Value_Range.y, Config_Value_Range.x);
                }

                Config_Value_Range_Length = Config_Value_Range.y - Config_Value_Range.x + 1;
                Config_Value_ = Mathf.Clamp(Config_Value_, Config_Value_Range.x, Config_Value_Range.y);
                Config_ToValuesNormalized_FromValues_Sync();

                if (Config_Value_UISize_Range.y <= Config_Value_UISize_Range.x)
                {
                    Config_Value_UISize_Range = new(Config_Value_UISize_Range.y, Config_Value_UISize_Range.x);
                }

                Config_Value_UISize = 1f / Config_Value_Range_Length;
                
                Config_Value_UISize
                = Mathf.Clamp(Config_Value_UISize, Config_Value_UISize_Range.x, Config_Value_UISize_Range.y);
            } // end if
        } // end method

        private void Config_TearDown()
        {
            // Do nothing.
        }

        private void Config_ToValuesNormalized_FromValues_Sync()
        {
            if (Config_Value_Range_Length - 1 > 0)
            {
                Config_Value_Normalized
                = (float)(Config_Value_ - Config_Value_Range.x)
                    / (float)(Config_Value_Range_Length - 1);

                Config_Value_ToRefresh_Normalized
                = (float)(Config_Value_ToRefresh - Config_Value_Range.x)
                    / (float)(Config_Value_Range_Length - 1);
            }
            else
            {
                Config_Value_Normalized = 0f;
                Config_Value_ToRefresh_Normalized = 0f;
            }
        }

        private void Config_ToValues_FromValuesNormalized_Sync()
        {
            if (Config_Value_Range_Length - 1 > 0)
            {
                 
                Config_Value_ 
                = Mathf.RoundToInt(
                    Config_Value_Range.x
                    + Config_Value_Normalized * (Config_Value_Range_Length - 1)
                );

                Config_Value_ToRefresh
                = Mathf.RoundToInt(
                    Config_Value_Range.x
                    + Config_Value_ToRefresh_Normalized * (Config_Value_Range_Length - 1)
                );
            }
            else
            {
                Config_Value_ = 0;
                Config_Value_ToRefresh = 0;
            }
        }

        private void Config_ToValuesToRefresh_FromValues_Sync()
        {
            Config_Value_ToRefresh = Config_Value_;
            Config_Value_ToRefresh_Normalized = Config_Value_Normalized;
        }

        private void Config_ToValues_FromValuesToRefresh_Sync()
        {
            Config_Value_ = Config_Value_ToRefresh;
            Config_Value_Normalized = Config_Value_ToRefresh_Normalized;
        }
        // End helpers config.

        // Begin helpers UI.
        private void UI_Setup()
        {
            UI_Text = UI_Text_Object.GetComponent<Text>();
            UI_Scrollbar = UI_Scrollbar_Object.GetComponent<Scrollbar>();
            UI_Button_Minus10 = UI_Button_Minus10_Object.GetComponent<Scrollbar>();
            UI_Button_Minus1 = UI_Button_Minus1_Object.GetComponent<Scrollbar>();
            UI_Button_Plus1 = UI_Button_Plus1_Object.GetComponent<Scrollbar>();
            UI_Button_Plus10 = UI_Button_Plus10_Object.GetComponent<Scrollbar>();
            UI_Button_Refresh = UI_Button_Refresh_Object.GetComponent<Scrollbar>();

            UI_Scrollbar.onValueChanged.AddListener(UI_Scrollbar_OnValueChanged);
            UI_Button_Minus10.onValueChanged.AddListener(UI_Button_Minus10_OnClick);
            UI_Button_Minus1.onValueChanged.AddListener(UI_Button_Minus1_OnClick);
            UI_Button_Plus1.onValueChanged.AddListener(UI_Button_Plus1_OnClick);
            UI_Button_Plus10.onValueChanged.AddListener(UI_Button_Plus10_OnClick);
            UI_Button_Refresh.onValueChanged.AddListener(UI_Button_Refresh_OnClick);
        }

        private void UI_Refresh()
        {
            UI_Refresh_Countdown -= Time.fixedDeltaTime;

            if (UI_Refresh_Countdown < Time.fixedDeltaTime)
            {
                UI_Refresh_Countdown = UI_Refresh_Interval;

                if (UI_Button_Refresh_Enabled)
                {
                    if (Config_Value_ == Config_Value_ToRefresh)
                    {
                        UI_Text.text = $"{UI_Text_Prefix}: {Config_Value_ToRefresh}";
                    }
                    else
                    {
                        UI_Text.text = $"{UI_Text_Prefix} [*]: {Config_Value_ToRefresh}";
                    }
                }
                else
                {
                    UI_Text.text = $"{UI_Text_Prefix}: {Config_Value_}";
                }
                
                UI_Scrollbar.interactable = UI_Scrollbar_Enabled;
                UI_Button_Minus10.interactable = UI_Button_Minus10_Enabled;
                UI_Button_Minus1.interactable = UI_Button_Minus1_Enabled;
                UI_Button_Plus1.interactable = UI_Button_Plus1_Enabled;
                UI_Button_Plus10.interactable = UI_Button_Plus10_Enabled;
                UI_Button_Refresh.interactable = UI_Button_Refresh_Enabled;

                if (Config_Value_ScrollbarSync_Needed)
                {
                    Config_Value_ScrollbarSync_Needed = false;
                    UI_Scrollbar.SetValueWithoutNotify(Config_Value_Normalized);
                }
            }
        } // end method

        private void UI_TearDown()
        {
            UI_Scrollbar.onValueChanged.RemoveListener(UI_Scrollbar_OnValueChanged);
            UI_Button_Minus10.onValueChanged.RemoveListener(UI_Button_Minus10_OnClick);
            UI_Button_Minus1.onValueChanged.RemoveListener(UI_Button_Minus1_OnClick);
            UI_Button_Plus1.onValueChanged.RemoveListener(UI_Button_Plus1_OnClick);
            UI_Button_Plus10.onValueChanged.RemoveListener(UI_Button_Plus10_OnClick);
            UI_Button_Refresh.onValueChanged.RemoveListener(UI_Button_Refresh_OnClick);
        }

        private void UI_Scrollbar_OnValueChanged(float Value_)
        {
            Config_Value_ToRefresh_Normalized = UI_Scrollbar.value;
            Config_ToValues_FromValuesNormalized_Sync();
            Config_Value_OnChanged.Invoke(Config_Value_ToRefresh);

            if (!UI_Button_Refresh_Enabled)
            {
                Config_ToValues_FromValuesToRefresh_Sync();
                Config_Refresh_OnClicked.Invoke(Config_Value_ToRefresh);
            }
        }

        private void UI_Button_Minus10_OnClick(float Value_)
        {
            UI_Button_UpdateByAmount_OnClick(Value_, -10);
        }

        private void UI_Button_Minus1_OnClick(float Value_)
        {
            UI_Button_UpdateByAmount_OnClick(Value_, -1);
        }

        private void UI_Button_Plus1_OnClick(float Value_)
        {
            UI_Button_UpdateByAmount_OnClick(Value_, 1);
        }

        private void UI_Button_Plus10_OnClick(float Value_)
        {
            UI_Button_UpdateByAmount_OnClick(Value_, 10);
        }

        private void UI_Button_Refresh_OnClick(float Value_)
        {
            if (UI_Button_Refresh_Enabled)
            {
                Config_ToValues_FromValuesToRefresh_Sync();
                Config_Refresh_OnClicked.Invoke(Config_Value_ToRefresh);
            }
        }
        
        private void UI_Button_UpdateByAmount_OnClick(float Value_, int Amount)
        {
            Config_Value_ToRefresh += Amount;
            Config_Value_ToRefresh = Mathf.Clamp(Config_Value_ToRefresh, Config_Value_Range.x, Config_Value_Range.y);
            Config_ToValuesNormalized_FromValues_Sync();
            UI_Scrollbar.SetValueWithoutNotify(Config_Value_ToRefresh_Normalized);
            Config_Value_OnChanged.Invoke(Config_Value_ToRefresh);

            if (!UI_Button_Refresh_Enabled)
            {
                Config_ToValues_FromValuesToRefresh_Sync();
                Config_Refresh_OnClicked.Invoke(Config_Value_ToRefresh);
            }
        }
        // End helpers UI.
    }
}

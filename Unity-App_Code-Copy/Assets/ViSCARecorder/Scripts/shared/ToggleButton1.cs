using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ViSCARecorder
{
    public class ToggleButton1 : MonoBehaviour
    {
        public GameObject ToggleTargetObject;

        private Button Button_;

        void Start()
        {
            Button_ = GetComponent<Button>();

            if (Button_ != null)
            {
                Button_.onClick.AddListener(Toggle);
            }
        }
        
        void FixedUpdate()
        {
            // Do nothing.
        }

        void OnDestroy()
        {
            if (Button_ != null)
            {
                Button_.onClick.RemoveListener(Toggle);
            }
        }

        private void Toggle()
        {
            if (ToggleTargetObject != null)
            {
                ToggleTargetObject.SetActive(!ToggleTargetObject.activeInHierarchy);
            }
        }
    }
}

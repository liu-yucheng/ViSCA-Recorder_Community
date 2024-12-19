// Copyright (C) 2024 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ViSCARecorder
{
    public class AudioSource_Controller1 : MonoBehaviour
    {
        private AudioSource AudioSource_;
        void Start()
        {
            AudioSource_ = GetComponent<AudioSource>();
            AudioSource_.Play();
            AudioSource_.loop = true;
        }

        void Update()
        {
            // Do nothing.
        }

        void OnDestroy()
        {
            AudioSource_.Stop();
        }
    }
}

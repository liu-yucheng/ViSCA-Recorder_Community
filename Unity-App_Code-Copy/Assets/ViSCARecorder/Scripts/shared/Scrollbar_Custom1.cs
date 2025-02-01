// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

// Begin References.
//
// Reference 1: Package UnityEngine.UI, script Scrollbar.cs.
// Reference 2: Package UnityEngine.UI, script Button.cs.
//
// End References.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ViSCARecorder
{
    /// <summary>
    /// Defines a custom scrollbar that works like a button.
    /// Reacts to both drag and click events.
    /// This works in VR applications with moving "XR origin,"
    ///   in which classic Unity buttons does not work.
    /// </summary>
    public class Scrollbar_Custom1 : Scrollbar, IPointerClickHandler, ISubmitHandler
    {
        private bool Scrollbar_OnValueChanged_Needed = false;
        private bool Button_OnValueChanged_Needed = false;
        private float OnValueChanged_Interval_Minimum = 0.1f; // 10 Hz.
        private float OnValueChanged_InvokeTime_Last = 0f;

        // Begin Scrollbar callbacks.
        public override void OnBeginDrag(PointerEventData eventData) 
        {
            base.OnBeginDrag(eventData);
            Scroll();
        }

        public override void OnDrag(PointerEventData eventData)
        {
            base.OnDrag(eventData);
            Scroll();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            Scroll();
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);

            if (Scrollbar_OnValueChanged_Needed || Button_OnValueChanged_Needed)
            {
                Scrollbar_OnValueChanged_Needed = false;
                Button_OnValueChanged_Needed = false;
                OnValueChanged_Invoke();
            }
        }

        public override void OnMove(AxisEventData eventData)
        {
            base.OnMove(eventData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Scroll()
        {
            if (IsActive() && IsInteractable())
            {
                Scrollbar_OnValueChanged_Needed = true;
            }
        }
        // End Scrollbar callbacks.

        // Begin Button callbacks.
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Press();
            }
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            Press();

            // if we get set disabled during the press, don't run the coroutine.
            if (IsActive() && IsInteractable())
            {
                DoStateTransition(SelectionState.Pressed, false);
                StartCoroutine(OnFinishSubmit());
            }
        }

        private IEnumerator OnFinishSubmit()
        {
            var fadeTime = colors.fadeDuration;
            var elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            DoStateTransition(currentSelectionState, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Press()
        {
            if (IsActive() && IsInteractable())
            {
                Button_OnValueChanged_Needed = true;
            }
        }
        // End Button callbacks.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnValueChanged_Invoke()
        {
            float Time_Now = Time.time;
            float Invoke_Interval = Time_Now - OnValueChanged_InvokeTime_Last;

            if (Invoke_Interval >= OnValueChanged_Interval_Minimum)
            {
                OnValueChanged_InvokeTime_Last = Time_Now;
                UISystemProfilerApi.AddMarker("Scrollbar_Custom1.onValueChanged", this);
                onValueChanged.Invoke(value);
            }   
        }
    }
} // end namespace

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
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ViSCARecorder
{
    public class Scrollbar_Custom1 : Scrollbar, IPointerClickHandler, ISubmitHandler
    {
        public override void OnBeginDrag(PointerEventData eventData) 
        {
            base.OnBeginDrag(eventData);
            onValueChanged.Invoke(value);
        }
        public override void OnDrag(PointerEventData eventData)
        {
            base.OnDrag(eventData);
            onValueChanged.Invoke(value);
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            Press();
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            Press();

            // if we get set disabled during the press, don't run the coroutine.
            if (!IsActive() || !IsInteractable())
                return;

            DoStateTransition(SelectionState.Pressed, false);
            StartCoroutine(OnFinishSubmit());
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

        private void Press()
        {
            if (!IsActive() || !IsInteractable())
                return;

            UISystemProfilerApi.AddMarker("Button.onClick", this);
            onValueChanged.Invoke(value);
        }
    }
}

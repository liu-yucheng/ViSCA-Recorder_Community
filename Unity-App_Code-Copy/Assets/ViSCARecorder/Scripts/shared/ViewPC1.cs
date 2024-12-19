// Copyright (C) 2024 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using UnityEngine;
using UnityEngine.InputSystem;

namespace ViSCARecorder
{
    public class ViewPC1 : MonoBehaviour
    {
        public InputActionReference Action_Rotate;
        public InputActionReference Action_Translate;
        public InputActionReference Action_Reset;

        public List<GameObject> Reset_Objects = new();
        public List<GameObject> Sync_Objects = new();

        public Vector3 Input_Rotation = Vector3.zero;
        public Vector3 Input_Translation = Vector3.zero;
        public bool Input_Reset = false;


        private float Speed_Rotation_DegreesPerS = 120f;
        private float Speed_Translation_MPerS = 2.5f;

        private List<Vector3> Reset_Rotations = new();
        private List<Vector3> Reset_Translations = new();

        private List<Vector3> Sync_Rotations = new();
        private List<Vector3> Sync_Translations = new();

        private Vector3 Rotation_Current;
        private Vector3 Translation_Current;

        private Vector3 Rotation_Delta;
        private Vector3 Translation_Delta;


        void Start()
        {
            Reset_Objects.Add(gameObject);

            foreach (GameObject Object_ in Reset_Objects)
            {
                Reset_Rotations.Add(Object_.transform.rotation.eulerAngles);
                Reset_Translations.Add(Object_.transform.position);
            }

            foreach (GameObject Object_ in Sync_Objects)
            {
                Sync_Rotations.Add(Object_.transform.rotation.eulerAngles);
                Sync_Translations.Add(Object_.transform.position);
            }

            Rotation_Current = transform.rotation.eulerAngles;
            Translation_Current = transform.position;

            Rotation_Delta = Vector3.zero;
            Translation_Delta = Vector3.zero;
        }

        void FixedUpdate()
        {
            FindRotationInput();
            FindTranslationInput();
            FindResetInput();

            Rotation_Current = transform.rotation.eulerAngles;
            Translation_Current = transform.position;

            Rotation_Delta = Time.fixedDeltaTime * Speed_Rotation_DegreesPerS * Input_Rotation;
            Translation_Delta = Time.fixedDeltaTime * Speed_Translation_MPerS * Input_Translation;

            ProcessRotation();
            ProcessTranslation();
            ProcessReset();
            ProcessSync();

            transform.rotation = Quaternion.Euler(Rotation_Current);
            transform.position = Translation_Current;
        }

        void OnDestroy()
        {
            for (int Index_ = 0; Index_ < Sync_Objects.Count; Index_ += 1)
            {
                GameObject Object_ = Sync_Objects[Index_];
                Object_.transform.rotation = Quaternion.Euler(Sync_Rotations[Index_]);
                Object_.transform.position = Sync_Translations[Index_];
            }
        }

        private void FindRotationInput()
        {
            Input_Rotation = Action_Rotate.action.ReadValue<Vector3>();
        }

        private void FindTranslationInput()
        {
            Input_Translation = Action_Translate.action.ReadValue<Vector3>();
        }

        private void FindResetInput()
        {
            Input_Reset = Action_Reset.action.ReadValue<float>() > 0f;
        }

        private void ProcessRotation()
        {
            Quaternion Current_Rotation_ = Quaternion.Euler(Rotation_Current);
            Quaternion Delta_RotationYaw = Quaternion.AngleAxis(Rotation_Delta.y, Current_Rotation_ * Vector3.up);
            Quaternion Delta_RotationPitch = Quaternion.AngleAxis(Rotation_Delta.x, Current_Rotation_ * Vector3.left);
            Quaternion Delta_RotationRoll = Quaternion.AngleAxis(Rotation_Delta.z, Current_Rotation_ * Vector3.back);
            Current_Rotation_ = Delta_RotationRoll * Delta_RotationPitch * Delta_RotationYaw * Current_Rotation_;
            Rotation_Current = Current_Rotation_.eulerAngles;
        }

        private void ProcessTranslation()
        {
            Quaternion Current_Rotation_ = Quaternion.Euler(Rotation_Current);
            Vector3 Delta_TranslationForwardBackward = Translation_Delta.z * (Current_Rotation_ * Vector3.forward);
            Vector3 Delta_TranslationLeftRight = Translation_Delta.x * (Current_Rotation_ * Vector3.right);
            Vector3 Delta_TranslationUpDown = Translation_Delta.y * (Current_Rotation_ * Vector3.up);
            Translation_Current = Delta_TranslationUpDown + Delta_TranslationLeftRight + Delta_TranslationForwardBackward + Translation_Current;
        }

        private void ProcessReset()
        {
            if (Input_Reset)
            {
                for (int Index_ = 0; Index_ < Reset_Objects.Count; Index_ += 1)
                {
                    GameObject Object_ = Reset_Objects[Index_];
                    Object_.transform.rotation = Quaternion.Euler(Reset_Rotations[Index_]);
                    Object_.transform.position = Reset_Translations[Index_];
                }
                
                Rotation_Current = transform.rotation.eulerAngles;
                Translation_Current = transform.position;
            }
        }

        private void ProcessSync()
        {
            for (int Index_ = 0; Index_ < Sync_Objects.Count; Index_ += 1)
            {
                GameObject Object_ = Sync_Objects[Index_];
                Object_.transform.rotation = Quaternion.Euler(Rotation_Current);
                Object_.transform.position = Translation_Current;
            }
        }
    } // end class
} // end namespace

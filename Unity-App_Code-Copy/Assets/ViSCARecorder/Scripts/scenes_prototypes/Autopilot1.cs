// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

// begin References.
//
// Reference 1. https://github.com/Ahmedsaed/CarAI-Unity .
//
// end References.

using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using System;

namespace ViSCARecorder {
    // Based on https://github.com/Ahmedsaed/CarAI-Unity/blob/main/CarAI/Scripts/CarAI.cs .
    public class Autopilot1 : MonoBehaviour
    {
        public GameObject Vehicle_Rigidbody_Object;
        public Transform Vehicle_BoundaryFront;
        public List<string> NavMesh_Layers = new();
        public Transform Autopilot_DestinationCustom;
        public bool Autopilot_MoveEnabled = true;
        public bool Autopilot_PatrolEnabled = true;
        public bool Debug_Enabled = true;
        public bool Debug_Gizmos_Enabled = true;
        public Vector2 Output_Autopilot = Vector2.zero;

        private Rigidbody Vehicle_Rigidbody;
        private int NavMesh_LayerByte = 0;
        private Vector2 Autopilot_SpeedRange_MPerS = new(-25f, 25f);
        private float Autopilot_FieldOfView_Degrees = 60f;
        private bool Autopilot_CanMove = false;
        private bool Autopilot_CanPatrol = false;
        private float Waypoint_ReachThreshold_Meters = 2.5f;
        private List<Vector3> Waypoint_Positions = new();
        private Vector3 Waypoint_Current_Position = Vector3.zero;
        private int Waypoint_Current_Index = 0;
        private Vector3 PatrolAround_Position = Vector3.zero;
        private Vector2 ThrottleBrakeRange_Nominal = new(-1f, 1f);
        private Vector2 ThrottleBrakeRange_Idle = new(-0.5f, 0.5f);
        private Vector2 ThrottleBrakeRange_OverSteer = new(-0.667f, 0.667f);
        private Vector2 ThrottleBrakeRange_Current = new(-1f, 1f);
        private float Approach_IdleRadius_Meters = 20f;
        private Vector2 SteeringWheelRange_Nominal = new(-1f, 1f);
        private Vector2 SteeringWheelRange_Idle = new(-0.25f, 0.25f);
        private Vector2 SteeringWheelRange_OverSteer_Inverse = new(-0.333f, 0.333f);
        private Vector2 SteeringWheelRange_Current = new(-1f, 1f);
        private Vector2 Brake_IdleSpeedRange_MPerS = new(-0.25f, 0.25f);
        private float SpeedForward_MPerS = 0f;
        private float PathCreate_PatrolRadius_Meters = 150f;
        private float PathCreate_SamplePosition_MaxDistance_Meters = 300f;
        private int PathCreate_FailureCount = 0;

        void Awake()
        {
            // Do nothing.
        }

        void Start()
        {
            Vehicle_Rigidbody = Vehicle_Rigidbody_Object.GetComponent<Rigidbody>();

            if (Autopilot_DestinationCustom == null)
            {
                PatrolAround_Position = Vehicle_Rigidbody_Object.transform.position;
            }
            else
            {
                PatrolAround_Position = Autopilot_DestinationCustom.transform.position;
            }

            NavMash_LayerByte_Find();
        }

        void FixedUpdate()
        {
            Steer();
            Progress();
        }

        void OnDrawGizmos()
        {
            if (Debug_Gizmos_Enabled)
            {
                for (int Index_ = 0; Index_ < Waypoint_Positions.Count; Index_ += 1)
                {
                    if (Index_ < Waypoint_Current_Index)
                    {
                        Gizmos.color = Color.green;
                    }
                    else if (Index_ > Waypoint_Current_Index)
                    {
                        Gizmos.color = Color.red;
                    }
                    else // else if (Index_ == Waypoint_Current_Index)
                    {
                        Gizmos.color = Color.yellow;
                    }

                    Gizmos.DrawWireSphere(Waypoint_Positions[Index_], Waypoint_ReachThreshold_Meters);
                }

                for (int Index_ = 0; Index_ < Waypoint_Positions.Count - 1; Index_ += 1)
                {
                    if (Index_ < Waypoint_Current_Index - 1)
                    {
                        Gizmos.color = Color.green;
                    }
                    else if (Index_ > Waypoint_Current_Index - 1)
                    {
                        Gizmos.color = Color.red;
                    }
                    else // else if (Index_ == Waypoint_Current_Index)
                    {
                        Gizmos.color = Color.yellow;
                    }

                    if (Index_ + 1 < Waypoint_Positions.Count)
                    {
                        Gizmos.DrawLine(Waypoint_Positions[Index_], Waypoint_Positions[Index_ + 1]);
                    }
                }

                FieldOfView_Find();
            }

            void FieldOfView_Find()
            {
                Gizmos.color = Color.white;
                float FieldOfView_Degrees = Autopilot_FieldOfView_Degrees * 2f;
                float Ray_Range_Meters = 10f;
                float FieldOfView_Half_Degrees = FieldOfView_Degrees / 2f;
                Quaternion Ray_Left_Rotation = Quaternion.AngleAxis(-FieldOfView_Half_Degrees, Vector3.up);
                Quaternion Ray_Right_Rotation = Quaternion.AngleAxis(FieldOfView_Half_Degrees, Vector3.up);
                Vector3 Ray_Left_Direction = Ray_Left_Rotation * Vehicle_Rigidbody_Object.transform.forward;
                Vector3 Ray_Right_Direction = Ray_Right_Rotation * Vehicle_Rigidbody_Object.transform.forward;
                Gizmos.DrawRay(Vehicle_BoundaryFront.position, Ray_Left_Direction * Ray_Range_Meters);
                Gizmos.DrawRay(Vehicle_BoundaryFront.position, Ray_Right_Direction * Ray_Range_Meters);
            }
        }

        private void NavMash_LayerByte_Find()
        {
            if (NavMesh_Layers == null || NavMesh_Layers.Contains("AllAreas"))
            {
                NavMesh_LayerByte = NavMesh.AllAreas;
            }
            else if (NavMesh_Layers.Count == 1)
            {
                NavMesh_LayerByte |= 1 << NavMesh.GetAreaFromName(NavMesh_Layers[0]);
            }
            else
            {
                foreach (string Layer_ in NavMesh_Layers)
                {
                    int Index_ = 1 << NavMesh.GetAreaFromName(Layer_);
                    NavMesh_LayerByte |= Index_;
                }
            }
        }

        private void Steer()
        {
            SteeringWheelRange_Current = SteeringWheelRange_Nominal;
            Vector3 PositionRelative_WaypointCurrent = Vehicle_Rigidbody_Object.transform.InverseTransformPoint(Waypoint_Current_Position);
            float SteeringWheel_Raw = PositionRelative_WaypointCurrent.x / PositionRelative_WaypointCurrent.magnitude;
            
            if (!Vector3_IsFinite(PositionRelative_WaypointCurrent))
            {
                SteeringWheel_Raw = 0f;
            }

            if (
                PositionRelative_WaypointCurrent.x == 0f
                &&
                PositionRelative_WaypointCurrent.z < 0f
            )
            {
                SteeringWheel_Raw = MathF.Sign(UnityEngine.Random.value) * 1f;
            }

            SpeedForward_MPerS = Vector3.Dot(Vehicle_Rigidbody_Object.transform.forward, Vehicle_Rigidbody.velocity);

            if (
                SpeedForward_MPerS < Brake_IdleSpeedRange_MPerS.x
                || SpeedForward_MPerS > Brake_IdleSpeedRange_MPerS.y
            )
            {
                SteeringWheelRange_Current = SteeringWheelRange_Nominal;
            }
            else
            {
                SteeringWheelRange_Current = SteeringWheelRange_Idle;
            }

            float SteeringWheel_Processed =
                SteeringWheelRange_Current.x
                + 
                MathF.Abs(SteeringWheelRange_Current.y - SteeringWheelRange_Current.x)
                *
                (0.5f + SteeringWheel_Raw / 2)
            ;

            // Crucial statement. Steering-wheel command.
            Output_Autopilot.x = SteeringWheel_Processed;
        }

        private void Progress() 
        {
            Waypoint_Positions_Manage();
            Move();
            Waypoint_Positions_Optimize();
        }

        private void Waypoint_Positions_Manage()
        {
            if (Waypoint_Current_Index >= Waypoint_Positions.Count)
            {
                Autopilot_CanMove = false;
                Autopilot_CanPatrol = Autopilot_PatrolEnabled;
            }
            else
            {
                Autopilot_CanMove = true;
                Autopilot_CanPatrol = Autopilot_PatrolEnabled;
                Waypoint_Current_Position = Waypoint_Positions[Waypoint_Current_Index];
                
                if (Vector3.Distance(Vehicle_BoundaryFront.position, Waypoint_Current_Position) < Waypoint_ReachThreshold_Meters)
                {
                    Waypoint_Current_Index += 1;
                }
            }

            if (Waypoint_Current_Index >= Waypoint_Positions.Count - 3)
            {
                Path_Create();
            }
        }

        private void Move()
        {
            float ThrottleBrake_Value = 0f;

            if (Autopilot_MoveEnabled && Autopilot_CanMove)
            {
                Autopilot_CanMove = true;
            }
            else
            {
                Autopilot_CanMove = false;
            }

            if (Autopilot_CanMove)
            {
                SpeedForward_MPerS = Vector3.Dot(Vehicle_Rigidbody_Object.transform.forward, Vehicle_Rigidbody.velocity);
                float Speed_MPerS = MathF.Abs(SpeedForward_MPerS);

                if (
                    Vector3_IsFinite(Waypoint_Current_Position)
                    &&
                    Vector3.Distance(
                        Vehicle_Rigidbody_Object.transform.position,
                        Waypoint_Current_Position
                    )
                    <
                    Approach_IdleRadius_Meters
                )
                {
                    ThrottleBrakeRange_Current = ThrottleBrakeRange_Idle;
                }
                else if (
                    Output_Autopilot.x < SteeringWheelRange_OverSteer_Inverse.x
                    || Output_Autopilot.x > SteeringWheelRange_OverSteer_Inverse.y
                )
                {
                    ThrottleBrakeRange_Current = ThrottleBrakeRange_OverSteer;
                }
                else
                {
                    ThrottleBrakeRange_Current = ThrottleBrakeRange_Nominal;
                }

                if (!Vector3_IsFinite(Waypoint_Current_Position)) {
                    ThrottleBrake_Value = 0f;
                }
                else if (Speed_MPerS < 0.95f * Autopilot_SpeedRange_MPerS.y)
                {
                    ThrottleBrake_Value = ThrottleBrakeRange_Current.y;
                }
                else if (Speed_MPerS < 1.05f * Autopilot_SpeedRange_MPerS.y)
                {
                    ThrottleBrake_Value = 0f;
                }
                else
                {
                    Brake_ThrottleBrakeValue_Find(out ThrottleBrake_Value);
                }
            }
            else
            {
                Brake_ThrottleBrakeValue_Find(out ThrottleBrake_Value);
            }

            // Crucial statement. Throttle-brake command.
            Output_Autopilot.y = ThrottleBrake_Value;
        }

        private void Waypoint_Positions_Optimize()
        {
            while (Waypoint_Current_Index > 1 && Waypoint_Positions.Count > 16)
            {
                Waypoint_Positions.RemoveAt(0);
                Waypoint_Current_Index -= 1;
            }
        }

        private void Brake_ThrottleBrakeValue_Find(out float ThrottleBrake_Value)
        {
            float SpeedForward_MPerS = Vector3.Dot(Vehicle_Rigidbody_Object.transform.forward, Vehicle_Rigidbody.velocity);

            if (SpeedForward_MPerS > Brake_IdleSpeedRange_MPerS.y)
            {
                ThrottleBrake_Value = ThrottleBrakeRange_Current.x;
            }
            else if (SpeedForward_MPerS < Brake_IdleSpeedRange_MPerS.x)
            {
                ThrottleBrake_Value = ThrottleBrakeRange_Current.y;
            }
            else
            {
                ThrottleBrake_Value = 0f;
            }
        }

        private bool Vector3_IsFinite(Vector3 Vector3_)
        {
            bool result =
                float.IsFinite(Vector3_.x)
                &&
                float.IsFinite(Vector3_.y)
                &&
                float.IsFinite(Vector3_.z)
            ;

            return result;
        }

        void Path_Create()
        {
            if (Autopilot_DestinationCustom != null)
            {
                Path_Create_Custom(Autopilot_DestinationCustom);
                Autopilot_CanMove = true;
                Autopilot_CanPatrol = false;

                if (
                    Vector3.Distance(
                        Vehicle_Rigidbody_Object.transform.position,
                        Autopilot_DestinationCustom.position
                    )
                    <
                    Waypoint_ReachThreshold_Meters
                )
                {
                    PatrolAround_Position = Autopilot_DestinationCustom.transform.position;
                    Autopilot_DestinationCustom = null;
                }
            }
            else if (Autopilot_PatrolEnabled)
            {
                Path_Create_Random();
                Autopilot_CanMove = true;
                Autopilot_CanPatrol = true;
            }
            else
            {
                Debug_WriteLine(
                    $"Autopilot_DestinationCustom: {Autopilot_DestinationCustom}."
                    + $" Autopilot_MoveEnabled: {Autopilot_MoveEnabled}"
                    + $" Autopilot_PatrolEnabled: {Autopilot_PatrolEnabled}",

                    false
                );

                Autopilot_CanMove = false;
                Autopilot_CanPatrol = false;
            }
        }

        private void Path_Create_Random()
        {
            Vector3 Position_Source;
            Vector3 Position_Destination_Random = PathCreate_PatrolRadius_Meters * UnityEngine.Random.insideUnitSphere;
            Position_Destination_Random += PatrolAround_Position;
            Vector3 Direction_;

            if (Waypoint_Positions.Count < 1)
            {
                Position_Source = Vehicle_BoundaryFront.position;
                Direction_ = Vehicle_BoundaryFront.forward;
                Path_CreateWith(Position_Destination_Random, Position_Source, Direction_, NavMesh_LayerByte);
            }
            else
            {
                Position_Source = Waypoint_Positions[Waypoint_Positions.Count - 1];
                Direction_ = (PatrolAround_Position - Waypoint_Positions[Waypoint_Positions.Count - 1]).normalized;
                Path_CreateWith(Position_Destination_Random, Position_Source, Direction_, NavMesh_LayerByte);
            }
        }

        private void Path_Create_Custom(Transform Destination)
        {
            Vector3 Position_Source;
            Vector3 Direction_;

            if (Waypoint_Positions.Count < 1)
            {
                Position_Source = Vehicle_BoundaryFront.position;
                Direction_ = Vehicle_BoundaryFront.forward;
                Path_CreateWith(Destination.position, Position_Source, Direction_, NavMesh_LayerByte);
            }
            else
            {
                Position_Source = Waypoint_Positions[Waypoint_Positions.Count - 1];
                Direction_ = (Destination.position - Waypoint_Positions[Waypoint_Positions.Count - 1]).normalized;
                Path_CreateWith(Destination.position, Position_Source, Direction_, NavMesh_LayerByte);
            }
        }

        private void Debug_WriteLine(string Info, bool IsError)
        {
            if (Debug_Enabled)
            {
                if (IsError)
                {
                    Debug.LogError(Info);
                }
                else
                {
                    Debug.Log(Info);
                }
            }
        }

        private void Path_CreateWith(
            Vector3 Position_Destination,
            Vector3 Position_Source,
            Vector3 Direction_SourceToDestination,
            int NavMesh_LayerByte
        )
        {
            NavMeshPath Path_ = new();

            if (
                NavMesh.SamplePosition(
                    Position_Destination,
                    out NavMeshHit Hit_,
                    PathCreate_SamplePosition_MaxDistance_Meters,
                    NavMesh_LayerByte
                )
                &&
                NavMesh.CalculatePath(
                    Position_Source,
                    Hit_.position,
                    NavMesh_LayerByte,
                    Path_
                )
                &&
                Path_.corners.Length >= 2
            )
            {
                if (Path_Create_CornersCheck(Path_.corners[1], Position_Source, Direction_SourceToDestination))
                {
                    Waypoint_Positions.AddRange(Path_.corners.ToList());
                    Debug_WriteLine("Path_.corners[1] checked; Random path created", false);
                }
                else
                {
                    if (Path_Create_CornersCheck(Path_.corners[2], Position_Source, Direction_SourceToDestination))
                    {
                        Waypoint_Positions.AddRange(Path_.corners.ToList());
                        Debug_WriteLine("Path_.corners[2] checked; Random path created", false);
                    }
                    else
                    {
                        Debug_WriteLine(
                            "Path_.corners[2] checking failed."
                            + " Waypoints outside Autopilot_FieldOfView_Degrees."
                            + " Will use fallback path.",

                            false
                        );

                        PathCreate_FailureCount += 1;
                        Waypoint_Positions.Add(Hit_.position);
                    }
                }
            }
            else
            {
                bool Result_SamplePosition = NavMesh.SamplePosition(
                    Position_Destination,
                    out Hit_,
                    PathCreate_SamplePosition_MaxDistance_Meters,
                    NavMesh_LayerByte
                );

                bool Result_CalculatePath = NavMesh.CalculatePath(
                    Position_Source,
                    Hit_.position,
                    NavMesh_LayerByte,
                    Path_
                );

                int Path_Corners_Length = Path_.corners.Length;

                Debug_WriteLine(
                    $" NavMesh.SamplePosition: {Result_SamplePosition}."
                    + $" Hit_.position: {Hit_.position}."
                    + $" NavMesh.CalculatePath: {Result_CalculatePath}."
                    + $" Path_.corners.Length: {Path_Corners_Length}."
                    + $" Will use fallback path.",

                    false
                );

                PathCreate_FailureCount += 1;
                Waypoint_Positions.Add(Hit_.position);
            }
        }

        private bool Path_Create_CornersCheck(Vector3 Position_Destination, Vector3 Position_Source, Vector3 Direction_)
        {
            Vector3 Direction_DestinationToSource = (Position_Destination - Position_Source).normalized;
            float DotProduct_Cos = Vector3.Dot(Direction_DestinationToSource, Direction_);
            float Angle_ = Mathf.Acos(DotProduct_Cos) * Mathf.Rad2Deg;
            bool Result_;

            if (Angle_ < Autopilot_FieldOfView_Degrees)
            {
                Result_ = true;
            }
            else
            {
                Result_ = false;
            }

            return Result_;
        } // end method
    } // end class
} // end namespace

// Copyright (C) 2024 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

// begin References.
//
// Reference 1. https://github.com/Ahmedsaed/CarAI-Unity .
//
// end References.

using System;
using System.Collections.Generic;
using System.Linq;

using TMPro;

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

using Random = UnityEngine.Random;


namespace ViSCARecorder {
    // Based on https://github.com/Ahmedsaed/CarAI-Unity/blob/main/CarAI/Scripts/CarAI.cs .
    public class Autopilot2 : MonoBehaviour
    {
        // begin public fields
        public GameObject Vehicle_Rigidbody_Object;
        public Transform Vehicle_BoundaryFront;
        public List<string> NavMesh_Layers = new();
        
        public Transform Autopilot_DestinationCustom;
        public bool Autopilot_MoveEnabled = true;
        public bool Autopilot_PatrolEnabled = true;
        
        public bool Debug_Texts_Enabled = true;
        public bool Debug_Gizmos_Enabled = true;
        
        public GameObject Specials_Waypoint_Prefab;
        public Material Specials_Waypoint_Material;
        public GameObject Specials_Root_Object;
        
        public Vector2 Output_Autopilot = Vector2.zero;
        // end public fields

        // begin private fields
        private Rigidbody Vehicle_Rigidbody;
        
        private int Autopilot_NavMesh_LayerByte = 0;
        private float Autopilot_Refresh_Interval = 0.033_333_333f; // 30 Hz.
        private float Autopilot_Refresh_Countdown = 0f;
        private Vector2 Autopilot_SpeedRange_MPerS = new(-15f, 15f);
        private float Autopilot_FieldOfView_Degrees = 60f;
        private bool Autopilot_CanMove = false;
        private bool Autopilot_CanPatrol = false;
        private float Autopilot_SpeedForward_MPerS = 0f;

        private float Waypoint_ReachThreshold_Meters = 2.5f;
        private List<Vector3> Waypoint_Positions = new();
        private Vector3 Waypoint_Current_Position = Vector3.zero;
        private int Waypoint_Current_Index = 0;
        private Vector3 Waypoint_PatrolAround_Position = Vector3.zero;
        
        private Vector2 ThrottleBrake_Range_Nominal = new(-1f, 1f);
        private Vector2 ThrottleBrake_Range_Idle = new(-0.5f, 0.5f);
        private Vector2 ThrottleBrake_Range_OverSteer = new(-0.667f, 0.667f);
        private Vector2 ThrottleBrake_Range_Current = new(-1f, 1f);
        private float ThrottleBrake_WaypointApproach_IdleRadius_Meters = 20f;
        private Vector2 ThrottleBrake_IdleSpeedRange_MPerS = new(-0.25f, 0.25f);

        private Vector2 SteeringWheelRange_Nominal = new(-1f, 1f);
        private Vector2 SteeringWheelRange_Idle = new(-0.25f, 0.25f);
        private Vector2 SteeringWheelRange_OverSteer_Inverse = new(-0.333f, 0.333f);
        private Vector2 SteeringWheelRange_Current = new(-1f, 1f);
        
        private float PathCreate_PatrolRadius_Meters = 200f;
        private float PathCreate_SamplePosition_MaxDistance_Meters = 200f;
        private int PathCreate_FailureCount = 0;
        
        private float Gizmos_PositionY_AboveTerrain = 1 + 10f;
        
        private int Random_Autopilot_Seed = 0;
        private Random.State Random_Autopilot_State;
        private bool Random_Autopilot_Initialized = false;
        
        private int Specials_Index = 0;
        private Recorder24_.Record.Specials Specials_Instance;
        private float Specials_PositionY_AboveTerrain = 1 + 10f;
        private List<GameObject> Specials_Path_Objects = new();
        private float Specials_Refresh_Interval = 3f; // 0.333 Hz.
        private float Specials_Refresh_Countdown = 0f;
        // end private fields

        // begin MonoBehaviour callbacks
        void Awake()
        {
            // Do nothing.
        }

        void Start()
        {
            Vehicle_Rigidbody = Vehicle_Rigidbody_Object.GetComponent<Rigidbody>();

            if (Autopilot_DestinationCustom == null)
            {
                Waypoint_PatrolAround_Position = Vehicle_Rigidbody_Object.transform.position;
            }
            else
            {
                Waypoint_PatrolAround_Position = Autopilot_DestinationCustom.transform.position;
            }

            Random_Autopilot_Seed_Reset(Random_Autopilot_Seed);
            Specials_Index_Reset(Specials_Index);
            NavMash_LayerByte_Find();
            Specials_Setup();
        }

        void FixedUpdate()
        {
            Autopilot_Refresh_Countdown -= Time.fixedDeltaTime;

            if (Autopilot_Refresh_Countdown < Time.fixedDeltaTime)
            {
                Autopilot_Refresh_Countdown = Autopilot_Refresh_Interval;
                Steer();
                Progress();
            }

            Specials_Refresh();
        }

        void OnDrawGizmos()
        {
            if (Debug_Gizmos_Enabled)
            {
                Debug_Gizmos_Path_Spheres_Draw();
                Debug_Gizmos_Path_Lines_Draw();
                Debug_Gizmos_FieldOfView_Draw();
            }
        }
        // end MonoBehaviour callbacks

        // begin public methods
        public void Random_Autopilot_Seed_Reset(int Random_Autopilot_Seed_)
        {
            Random_Autopilot_Initialized = true;
            Random.InitState(Random_Autopilot_Seed_);
            Random_Autopilot_Seed = Random_Autopilot_Seed_;
            Random_Autopilot_State = Random.state;
        }

        public void Specials_Index_Reset(int Random_Specials_Index_)
        {
            Specials_Index = Random_Specials_Index_;

            Recorder24_.Record.SpecialsStandards.CreateSpecialsByIndex(
                Specials_Index,
                out Specials_Instance
            );
        }
        // end public methods

        // begin level 1 helpers
        private void NavMash_LayerByte_Find()
        {
            if (NavMesh_Layers == null || NavMesh_Layers.Contains("AllAreas"))
            {
                Autopilot_NavMesh_LayerByte = NavMesh.AllAreas;
            }
            else if (NavMesh_Layers.Count == 1)
            {
                Autopilot_NavMesh_LayerByte |= 1 << NavMesh.GetAreaFromName(NavMesh_Layers[0]);
            }
            else
            {
                foreach (string Layer_ in NavMesh_Layers)
                {
                    int Index_ = 1 << NavMesh.GetAreaFromName(Layer_);
                    Autopilot_NavMesh_LayerByte |= Index_;
                }
            }
        }

        private void Steer()
        {
            SteeringWheelRange_Current = SteeringWheelRange_Nominal;
            Vector3 PositionRelative_WaypointCurrent = Vehicle_Rigidbody_Object.transform.InverseTransformPoint(Waypoint_Current_Position);
            float SteeringWheel_Raw = PositionRelative_WaypointCurrent.x / PositionRelative_WaypointCurrent.magnitude;
            
            if (!Vector3_IsFinite(Waypoint_Current_Position))
            {
                SteeringWheel_Raw = 0f;
            }

            if (
                PositionRelative_WaypointCurrent.x == 0f
                &&
                PositionRelative_WaypointCurrent.z < 0f
            )
            {
                Random_Autopilot_State_Restore();
                SteeringWheel_Raw = MathF.Sign(Random.value) * 1f;
                Random_Autopilot_State_Backup();
            }

            Autopilot_SpeedForward_MPerS = Vector3.Dot(Vehicle_Rigidbody_Object.transform.forward, Vehicle_Rigidbody.velocity);

            if (
                Autopilot_SpeedForward_MPerS < ThrottleBrake_IdleSpeedRange_MPerS.x
                || Autopilot_SpeedForward_MPerS > ThrottleBrake_IdleSpeedRange_MPerS.y
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

        private void Specials_Setup()
        {
            Specials_Waypoint_Material = new(Specials_Waypoint_Material);
        }

        private void Specials_Refresh()
        {
            Specials_Refresh_Countdown -= Time.fixedDeltaTime;

            if (Specials_Refresh_Countdown < Time.fixedDeltaTime)
            {
                Specials_Refresh_Countdown = Specials_Refresh_Interval;
                Specials_Path_Clear();
                Specials_Path_Waypoints_Draw();
            }
        }

        private void Position_AtPositionAboveTerrain_OnSurface_Find(
            in Vector3 Position_AboveTerrain,
            out Vector3 Position_OnSurface
        )
        {
            Position_OnSurface = Position_AboveTerrain;
            RaycastHit RaycastHit_;

            if (
                Physics.Raycast(Position_AboveTerrain, Vector3.down, out RaycastHit_)
                && (
                    RaycastHit_.collider.CompareTag("terrain")
                    || RaycastHit_.collider.CompareTag("water")
                    || RaycastHit_.collider.CompareTag("walls")
                    || RaycastHit_.collider.CompareTag("ground-base")
                )
            )
            {
                Position_OnSurface = RaycastHit_.point;
            }
        }

        private void Debug_Gizmos_Path_Spheres_Draw()
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

                Vector3 Position_ = Waypoint_Positions[Index_];
                Position_.y = Gizmos_PositionY_AboveTerrain;
                Position_AtPositionAboveTerrain_OnSurface_Find(Position_, out Position_);
                Position_.y -= Waypoint_ReachThreshold_Meters / 2f;
                Gizmos.DrawWireSphere(Position_, Waypoint_ReachThreshold_Meters);
            }
        }

        private void Debug_Gizmos_Path_Lines_Draw()
        {
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
                else // else if (Index == Waypoint_Current_Index - 1)
                {
                    Gizmos.color = Color.yellow;
                }

                if (Index_ + 1 < Waypoint_Positions.Count)
                {
                    Vector3 Position_Start = Waypoint_Positions[Index_];
                    Position_Start.y = Gizmos_PositionY_AboveTerrain;
                    Position_AtPositionAboveTerrain_OnSurface_Find(Position_Start, out Position_Start);

                    Vector3 Position_End = Waypoint_Positions[Index_ + 1];
                    Position_End.y = Gizmos_PositionY_AboveTerrain;
                    Position_AtPositionAboveTerrain_OnSurface_Find(Position_End, out Position_End);

                    Gizmos.DrawLine(Position_Start, Position_End);
                }
            }
        } // end method

        private void Debug_Gizmos_FieldOfView_Draw()
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
        // end level 1 helpers

        // begin level 2 helpers
        private void Random_Autopilot_State_Restore()
        {
            if (!Random_Autopilot_Initialized)
            {
                Random_Autopilot_Initialized = true;
                Random.InitState(Random_Autopilot_Seed);
                Random_Autopilot_State = Random.state;
            }
            else
            {
                Random.state = Random_Autopilot_State;
            }
        }

        private void Random_Autopilot_State_Backup()
        {
            Random_Autopilot_State = Random.state;
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

                if (!Vector3_IsFinite(Waypoint_Current_Position))
                {
                    Waypoint_Current_Position = Vector3.zero;
                }

                Vector3 Position_Vehicle_XZ = Vehicle_BoundaryFront.position;
                Position_Vehicle_XZ.y = 0f;
                Vector3 Position_Waypoint_XZ = Waypoint_Current_Position;
                Position_Waypoint_XZ.y = 0f;

                if (Vector3.Distance(Position_Vehicle_XZ, Position_Waypoint_XZ) < Waypoint_ReachThreshold_Meters)
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
                Autopilot_SpeedForward_MPerS = Vector3.Dot(Vehicle_Rigidbody_Object.transform.forward, Vehicle_Rigidbody.velocity);
                float Speed_MPerS = MathF.Abs(Autopilot_SpeedForward_MPerS);

                if (
                    Vector3_IsFinite(Waypoint_Current_Position)
                    &&
                    Vector3.Distance(
                        Vehicle_Rigidbody_Object.transform.position,
                        Waypoint_Current_Position
                    )
                    <
                    ThrottleBrake_WaypointApproach_IdleRadius_Meters
                )
                {
                    ThrottleBrake_Range_Current = ThrottleBrake_Range_Idle;
                }
                else if (
                    Output_Autopilot.x < SteeringWheelRange_OverSteer_Inverse.x
                    || Output_Autopilot.x > SteeringWheelRange_OverSteer_Inverse.y
                )
                {
                    ThrottleBrake_Range_Current = ThrottleBrake_Range_OverSteer;
                }
                else
                {
                    ThrottleBrake_Range_Current = ThrottleBrake_Range_Nominal;
                }

                if (!Vector3_IsFinite(Waypoint_Current_Position)) {
                    ThrottleBrake_Value = 0f;
                }
                else if (Speed_MPerS < 0.95f * Autopilot_SpeedRange_MPerS.y)
                {
                    ThrottleBrake_Value = ThrottleBrake_Range_Current.y;
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

        private void Specials_Path_Clear()
        {
            for (int Index_ = 0; Index_ < Specials_Path_Objects.Count; Index_ += 1)
            {
                GameObject GameObject_ = Specials_Path_Objects[Index_];
                Destroy(GameObject_);
            }

            Specials_Path_Objects.Clear();
        }

        private void Specials_Path_Waypoints_Draw()
        {
            for (int Index_ = 0; Index_ < Waypoint_Positions.Count; Index_ += 1)
            {
                Color Color_ = Specials_Instance.color1_path;
                Color_.a = 0.65f;
                Specials_Waypoint_Material.color = Color_;
                Vector3 Position_ = Waypoint_Positions[Index_];
                Position_.y = Gizmos_PositionY_AboveTerrain;
                Position_AtPositionAboveTerrain_OnSurface_Find(Position_, out Position_);
                Position_.y += 0.25f;
                Quaternion Rotation_ = Quaternion.Euler(0f, 0f, 0f);

                GameObject Waypoint = Instantiate(
                    Specials_Waypoint_Prefab,
                    Position_,
                    Rotation_,
                    Specials_Root_Object.transform
                );

                Waypoint.name = $"specials-sphere_index-{Index_}";
                Waypoint.transform.localScale = new(0.5f, 0.5f, 0.5f);
                MeshRenderer MeshRenderer_ = Waypoint.GetComponent<MeshRenderer>();
                MeshRenderer_.material = Specials_Waypoint_Material;
                Specials_Path_Objects.Add(Waypoint);
            } // end for
        } // end method
        // end level 2 helpers

        // begin level 3+ helpers
        private void Brake_ThrottleBrakeValue_Find(out float ThrottleBrake_Value)
        {
            float SpeedForward_MPerS = Vector3.Dot(Vehicle_Rigidbody_Object.transform.forward, Vehicle_Rigidbody.velocity);

            if (SpeedForward_MPerS > ThrottleBrake_IdleSpeedRange_MPerS.y)
            {
                ThrottleBrake_Value = ThrottleBrake_Range_Current.x;
            }
            else if (SpeedForward_MPerS < ThrottleBrake_IdleSpeedRange_MPerS.x)
            {
                ThrottleBrake_Value = ThrottleBrake_Range_Current.y;
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
                    Waypoint_PatrolAround_Position = Autopilot_DestinationCustom.transform.position;
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
                Debug_Texts_WriteLine(
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
            Random_Autopilot_State_Restore();
            Vector3 Position_Destination_Random = PathCreate_PatrolRadius_Meters * Random.insideUnitSphere;
            Random_Autopilot_State_Backup();
            Position_Destination_Random += Waypoint_PatrolAround_Position;
            Vector3 Direction_;

            if (Waypoint_Positions.Count < 1)
            {
                Position_Source = Vehicle_BoundaryFront.position;
                Direction_ = Vehicle_BoundaryFront.forward;
                Path_CreateWith(Position_Destination_Random, Position_Source, Direction_, Autopilot_NavMesh_LayerByte);
            }
            else
            {
                Position_Source = Waypoint_Positions[Waypoint_Positions.Count - 1];
                Direction_ = (Waypoint_PatrolAround_Position - Waypoint_Positions[Waypoint_Positions.Count - 1]).normalized;
                Path_CreateWith(Position_Destination_Random, Position_Source, Direction_, Autopilot_NavMesh_LayerByte);
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
                Path_CreateWith(Destination.position, Position_Source, Direction_, Autopilot_NavMesh_LayerByte);
            }
            else
            {
                Position_Source = Waypoint_Positions[Waypoint_Positions.Count - 1];
                Direction_ = (Destination.position - Waypoint_Positions[Waypoint_Positions.Count - 1]).normalized;
                Path_CreateWith(Destination.position, Position_Source, Direction_, Autopilot_NavMesh_LayerByte);
            }
        }

        private void Debug_Texts_WriteLine(string Info, bool IsError)
        {
            if (Debug_Texts_Enabled)
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
                Path_.corners.Length >= 3
            )
            {
                if (Path_Create_CornersCheck(Path_.corners[1], Position_Source, Direction_SourceToDestination))
                {
                    Waypoint_Positions.AddRange(Path_.corners.ToList());
                    Debug_Texts_WriteLine("Path_.corners[1] checked; Random path created", false);
                }
                else
                {
                    if (Path_Create_CornersCheck(Path_.corners[2], Position_Source, Direction_SourceToDestination))
                    {
                        Waypoint_Positions.AddRange(Path_.corners.ToList());
                        Debug_Texts_WriteLine("Path_.corners[2] checked; Random path created", false);
                    }
                    else
                    {
                        Debug_Texts_WriteLine(
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

                Debug_Texts_WriteLine(
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
        }
        // end level 3+ helpers
    } // end class
} // end namespace

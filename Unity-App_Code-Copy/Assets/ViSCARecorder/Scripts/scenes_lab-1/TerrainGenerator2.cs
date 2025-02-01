// Copyright (C) 2024-2025 Yucheng Liu. Under the GNU AGPL 3.0 License.
// GNU AGPL 3.0 License: https://www.gnu.org/licenses/agpl-3.0.txt .

// Begin References.
// Reference 1: https://github.com/Syomus/ProceduralToolkit/blob/master/Samples/LowPolyTerrain/LowPolyTerrainExample.cs .
// Reference 2: https://github.com/Syomus/ProceduralToolkit/blob/master/Samples/LowPolyTerrain/LowPolyTerrainGenerator.cs .
// Reference 3: https://github.com/Syomus/ProceduralToolkit/blob/master/Samples/Common/ConfiguratorBase.cs .
// Reference 4: https://github.com/liu-yucheng/ViSCARecorder.GitHub-Repo/blob/main/Assets/InfiniteTerrain/Advanced/PlaceObjects.cs .
// End References.

using System;
using System.Collections.Generic;
using UnityEngine;
using ProceduralToolkit;
using ProceduralToolkit.FastNoiseLib;
using ViSCARecorder.TerrainGenerator2_;

using Random = UnityEngine.Random;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;


namespace ViSCARecorder
{
    public class TerrainGenerator2 : MonoBehaviour
    {
        // begin public fields
        public GameObject Terrain_Center_Object;
        public GameObject Terrain_Object;
        
        public GameObject Water_Object;
        
        public GameObject Nature_Root_Object;
        public List<GameObject> Nature_Prefab_Objects = new();
        public List<int> Nature_Prefab_Counts_Max = new();
        // end public fields

        // begin private fields
        private float TerrainGenerator_Refresh_Interval_Seconds = 0.033_333_333f; // 30 Hz.
        private float TerrainGenerator_Refresh_Countdown_Seconds = 0f;

        private MeshFilter Terrain_MeshFilter;
        private MeshCollider Terrain_MeshCollider;
        private Mesh Terrain_Mesh;
        private Gradient Terrain_Color_Gradient = ColorE.Gradient(Color.black, Color.white);
        private Tuple<float, float> Terrain_Color_Remap_Nodes = new(0.2f, 0.8f);
        private Vector3 Terrain_Size_Meters = new(500f, 20f, 500f);
        private Vector3 Terrain_Center_Position;
        private float Terrain_CellSize_Meters = 5f;
        private float Terrain_Noise_Frequency = 4f;
        
        private MeshRenderer Water_MeshRenderer;
        private float Water_Depth_Meters = 0.5f;
        
        private Palette_HSV Palette_HSV_ = new();
        private Palette Palette_ = new();
        private Vector2 Palette_OffsetRange_Hue = new(-18f / 360f, 18f / 360f);
        private Vector2 Palette_OffsetRange_Saturation = new(-0.05f, 0.05f);
        private Vector2 Palette_OffsetRange_Value = new(-0.05f, 0.05f);
        private Vector2 Palette_OffsetRange_Alpha = new(-0.001f, 0.001f);
        private float Palette_Offset_Hue = 0f;
        private float Palette_Offset_Saturation = 0f;
        private float Palette_Offset_Value = 0f;
        private float Palette_Offset_Alpha = 0f;
        
        private Random.State Random_State;
        private bool Random_Initialized = false;
        private int Random_Seed = 0;
        
        private int Nature_Prefab_Count_Max_Default = 1;
        private List<List<GameObject>> Nature_Objects_ByPrefabTypes = new();
        // end private fields

        // begin MonoBehaviour callbacks
        void Awake()
        {
            // Do nothing.
        }

        void Start()
        {
            Random_Seed_Reset(Random_Seed);
            Skybox_AndWater_Setup();
            Terrain_Setup();
            Nature_Setup();
            Terrain_AndNature_Generate();
        }

        void FixedUpdate()
        {
            TerrainGenerator_Refresh_Countdown_Seconds -= Time.fixedDeltaTime;

            if (TerrainGenerator_Refresh_Countdown_Seconds < Time.fixedDeltaTime) {
                TerrainGenerator_Refresh_Countdown_Seconds = TerrainGenerator_Refresh_Interval_Seconds;
                Palette.FromPaletteHSV_Create(in Palette_HSV_, out Palette_);
                Skybox_AndWater_Refresh();
            }
        }
        // end MonoBehaviour callbacks

        // begin public methods
        public void Random_Seed_Reset(int Random_Seed_)
        {
            Random_Initialized = true;
            Random.InitState(Random_Seed_);
            Random_Seed = Random_Seed_;
            Random_State = Random.state;
        }

        public void Terrain_AndNature_Generate()
        {
            Terrain_Generate();
            Nature_Generate();
        }
        // end public methods

        // begin level 1 helpers
        private void Skybox_AndWater_Setup()
        {
            RenderSettings.skybox = new(RenderSettings.skybox);
            Water_MeshRenderer = Water_Object.GetComponent<MeshRenderer>();
            Water_MeshRenderer.material = new(Water_MeshRenderer.material);
        }

        private void Skybox_AndWater_Refresh()
        {
            RenderSettings.skybox.SetColor("_SkyColor", Palette_HSV_.Color_Skybox_Sky.ToColor());
            RenderSettings.skybox.SetColor("_HorizonColor", Palette_HSV_.Color_Skybox_Horizon.ToColor());
            RenderSettings.skybox.SetColor("_GroundColor", Palette_HSV_.Color_Skybox_Ground.ToColor());
            Water_MeshRenderer.material.SetColor("_ShallowColor", Palette_HSV_.Color_Water_Shallow.ToColor());
            Water_MeshRenderer.material.SetColor("_DeepColor", Palette_HSV_.Color_Water_Deep.ToColor());
        }

        private void Palette_Create()
        {
            Random_State_Restore();
            Palette_Offset_Hue = Random.Range(Palette_OffsetRange_Hue.x, Palette_OffsetRange_Hue.y);
            Palette_Offset_Saturation = Random.Range(Palette_OffsetRange_Saturation.x, Palette_OffsetRange_Saturation.y);
            Palette_Offset_Value = Random.Range(Palette_OffsetRange_Value.x, Palette_OffsetRange_Value.y);
            Palette_Offset_Alpha = Random.Range(Palette_OffsetRange_Alpha.x, Palette_OffsetRange_Alpha.y);
            Random_State_Backup();
            ColorHSV Offset_ColorHSV = new(Palette_Offset_Hue, Palette_Offset_Saturation, Palette_Offset_Value, Palette_Offset_Alpha);
            PaletteHSV_Standards.PaletteHSV_OffsetByHSV_Create(Offset_ColorHSV, out Palette_HSV_);
        }

        private void Terrain_Setup()
        {
            Terrain_Center_Position = Terrain_Center_Object.transform.position;
            Terrain_MeshFilter = Terrain_Object.GetComponent<MeshFilter>();
            Terrain_MeshCollider = Terrain_Object.GetComponent<MeshCollider>();
            Terrain_Mesh = Terrain_MeshFilter.mesh;
            Terrain_MeshFilter.sharedMesh = Terrain_Mesh;
            Terrain_MeshCollider.sharedMesh = Terrain_Mesh;

            Terrain_MeshCollider.cookingOptions =
                MeshColliderCookingOptions.CookForFasterSimulation
                // | MeshColliderCookingOptions.EnableMeshCleaning
                | MeshColliderCookingOptions.UseFastMidphase
                // | MeshColliderCookingOptions.WeldColocatedVertices
            ;
        }

        private void Terrain_Generate()
        {
            Palette_Create();

            Terrain_Color_Gradient = ColorE.Gradient(
                Palette_HSV_.Color_Terrain_Shore, 
                Palette_HSV_.Color_Terrain_Ground
            );

            Terrain_MeshDraft_Generate(out MeshDraft MeshDraft_);

            MeshDraft_.Move(
                Vector3.left * Terrain_Size_Meters.x / 2f
                + Vector3.down * Terrain_Size_Meters.y / 2f
                + Vector3.back * Terrain_Size_Meters.z / 2f
            );

            Terrain_Mesh = MeshDraft_.ToMesh();

            Physics.BakeMesh(
                Terrain_Mesh.GetInstanceID(), 
                false, 
                Terrain_MeshCollider.cookingOptions
            );

            Terrain_MeshFilter.sharedMesh = Terrain_Mesh;
            Terrain_MeshCollider.sharedMesh = Terrain_Mesh;
        }

        private void Nature_Setup()
        {
            for (int Index_ = 0; Index_ < Nature_Prefab_Objects.Count; Index_ += 1)
            {
                if (Index_ > Nature_Prefab_Counts_Max.Count - 1)
                {
                    Nature_Prefab_Counts_Max.Add(Nature_Prefab_Count_Max_Default);
                }

                List<GameObject> GameObjects = new();
                Nature_Objects_ByPrefabTypes.Add(GameObjects);
            }
        }

        private void Nature_Generate()
        {
            for (int Index_ = 0; Index_ < Nature_Objects_ByPrefabTypes.Count; Index_ += 1)
            {
                for (int Index_2 = 0; Index_2 < Nature_Objects_ByPrefabTypes[Index_].Count; Index_2 += 1)
                {
                    GameObject GameObject_ = Nature_Objects_ByPrefabTypes[Index_][Index_2];
                    Destroy(GameObject_);
                }

                Nature_Objects_ByPrefabTypes[Index_].Clear();
            }

            for (int Index_ = 0; Index_ < Nature_Prefab_Objects.Count; Index_ += 1)
            {
                for (int Index_2 = 0; Index_2 < Nature_Prefab_Counts_Max[Index_]; Index_2 += 1)
                {
                    Position_AndRotation_Random_AboveTerrain_Generate(
                        out Vector3 Position_Random_AboveTerrain,
                        out Quaternion Rotation_Random
                    );

                    Position_AtPositionAboveTerrain_OnTerrain_Find(
                        Position_Random_AboveTerrain,
                        out Vector3 Position_Random_OnTerrain,
                        out bool Position_Random_OnTerrain_CanFind
                    );

                    if (Position_Random_OnTerrain_CanFind)
                    {
                        GameObject GameObject_ = Instantiate(
                            Nature_Prefab_Objects[Index_],
                            Position_Random_OnTerrain,
                            Rotation_Random,
                            Nature_Root_Object.transform
                        );

                        GameObject_.name = $"nature-object_type-{Index_}_copy-{Index_2}";
                        Nature_Objects_ByPrefabTypes[Index_].Add(GameObject_);
                    }
                } // end for
            } // end for
        } // end method
        // end level 1 helpers

        // begin level 2 helpers
        private void Random_State_Restore()
        {
            if (!Random_Initialized)
            {
                Random_Initialized = true;
                Random.InitState(Random_Seed);
                Random_State = Random.state;
            }
            else
            {
                Random.state = Random_State;
            }
        }

        private void Random_State_Backup()
        {
            Random_State = Random.state;
        }

        private void Terrain_Size_MakeValid()
        {
            if (Terrain_Size_Meters.x < 0f)
            {
                Terrain_Size_Meters.x = -Terrain_Size_Meters.x;
            }
            else if (Terrain_Size_Meters.x == 0f)
            {
                Terrain_Size_Meters.x = 10f;
            }

            if (Terrain_Size_Meters.z < 0f)
            {
                Terrain_Size_Meters.z = -Terrain_Size_Meters.z;
            }
            else if (Terrain_Size_Meters.x == 0f)
            {
                Terrain_Size_Meters.z = 10f;
            }
        }

        private void Terrain_CellSize_MakeValid()
        {
            if (Terrain_CellSize_Meters < 0f)
            {
                Terrain_CellSize_Meters *= -Terrain_CellSize_Meters;
            }
            else if (Terrain_CellSize_Meters == 0f)
            {
                Terrain_CellSize_Meters = 0.5f;
            }
        }

        private void Terrain_MeshDraft_Generate(out MeshDraft MeshDraft_)
        {
            Terrain_Size_MakeValid();
            Terrain_CellSize_MakeValid();
            Random_State_Restore();

            Vector2 Noise_Offset = new(
                Random.Range(0f, 100f),
                Random.Range(0f, 100f)
            );

            Random_State_Backup();

            int Segment_Count_X = Mathf.FloorToInt(Terrain_Size_Meters.x / Terrain_CellSize_Meters);
            int Segment_Count_Z = Mathf.FloorToInt(Terrain_Size_Meters.z / Terrain_CellSize_Meters);
            float Segment_StepSize_X = Terrain_Size_Meters.x / Segment_Count_X;
            float Segment_StepSize_Z = Terrain_Size_Meters.z / Segment_Count_Z;
            int Vertex_Count = 6 * Segment_Count_X * Segment_Count_Z;
            __.Terrain_MeshDraft_Create(Vertex_Count, out MeshDraft_);
            FastNoise Noise_ = new();
            Noise_.SetNoiseType(FastNoise.NoiseType.PerlinFractal);
            Noise_.SetFrequency(Terrain_Noise_Frequency);

            for (int Index_X = 0; Index_X < Segment_Count_X; Index_X += 1)
            {
                for (int Index_Z = 0; Index_Z < Segment_Count_Z; Index_Z += 1)
                {
                    int Index_XZ0 = 6 * (Index_X + Index_Z * Segment_Count_X);
                    int Index_XZ1 = Index_XZ0 + 1;
                    int Index_XZ2 = Index_XZ0 + 2;
                    int Index_XZ3 = Index_XZ0 + 3;
                    int Index_XZ4 = Index_XZ0 + 4;
                    int Index_XZ5 = Index_XZ0 + 5;

                    __.Terrain_NoiseHeight_Find(
                        Index_X + 0,
                        Index_Z + 0,
                        Segment_Count_X,
                        Segment_Count_Z,
                        Noise_Offset,
                        Noise_,
                        out float NoiseHeight_X0_Z0
                    );

                    __.Terrain_NoiseHeight_Find(
                        Index_X + 0,
                        Index_Z + 1,
                        Segment_Count_X,
                        Segment_Count_Z,
                        Noise_Offset,
                        Noise_,
                        out float NoiseHeight_X0_Z1
                    );

                    __.Terrain_NoiseHeight_Find(
                        Index_X + 1,
                        Index_Z + 0,
                        Segment_Count_X,
                        Segment_Count_Z,
                        Noise_Offset,
                        Noise_,
                        out float NoiseHeight_X1_Z0
                    );

                    __.Terrain_NoiseHeight_Find(
                        Index_X + 1,
                        Index_Z + 1,
                        Segment_Count_X,
                        Segment_Count_Z,
                        Noise_Offset,
                        Noise_,
                        out float NoiseHeight_X1_Z1
                    );

                    float Height_X0_Z0 = Terrain_Size_Meters.y * NoiseHeight_X0_Z0;
                    float Height_X0_Z1 = Terrain_Size_Meters.y * NoiseHeight_X0_Z1;
                    float Height_X1_Z0 = Terrain_Size_Meters.y * NoiseHeight_X1_Z0;
                    float Height_X1_Z1 = Terrain_Size_Meters.y * NoiseHeight_X1_Z1;

                    Terrain_Height_WithWaterDepth_Find(Height_X0_Z0, out Height_X0_Z0);
                    Terrain_Height_WithWaterDepth_Find(Height_X0_Z1, out Height_X0_Z1);
                    Terrain_Height_WithWaterDepth_Find(Height_X1_Z0, out Height_X1_Z0);
                    Terrain_Height_WithWaterDepth_Find(Height_X1_Z1, out Height_X1_Z1);

                    Vector3 Vertex_Position_X0_Z0 = new(
                        (Index_X + 0) * Segment_StepSize_X,
                        Height_X0_Z0,
                        (Index_Z + 0) * Segment_StepSize_Z
                    );

                    Vector3 Vertex_Position_X0_Z1 = new(
                        (Index_X + 0) * Segment_StepSize_X,
                        Height_X0_Z1,
                        (Index_Z + 1) * Segment_StepSize_Z
                    );

                    Vector3 Vertex_Position_X1_Z0 = new(
                        (Index_X + 1) * Segment_StepSize_X,
                        Height_X1_Z0,
                        (Index_Z + 0) * Segment_StepSize_Z
                    );

                    Vector3 Vertex_Position_X1_Z1 = new(
                        (Index_X + 1) * Segment_StepSize_X,
                        Height_X1_Z1,
                        (Index_Z + 1) * Segment_StepSize_Z
                    );

                    MeshDraft_.vertices[Index_XZ0] = Vertex_Position_X0_Z0;
                    MeshDraft_.vertices[Index_XZ1] = Vertex_Position_X0_Z1;
                    MeshDraft_.vertices[Index_XZ2] = Vertex_Position_X1_Z1;
                    MeshDraft_.vertices[Index_XZ3] = Vertex_Position_X0_Z0;
                    MeshDraft_.vertices[Index_XZ4] = Vertex_Position_X1_Z1;
                    MeshDraft_.vertices[Index_XZ5] = Vertex_Position_X1_Z0;

                    MeshDraft_.colors[Index_XZ0] = Terrain_Color_Gradient.Evaluate(
                        __.TerrainHeight_GradientAmount_Find(
                            NoiseHeight_X0_Z0,
                            Terrain_Color_Remap_Nodes.Item1,
                            Terrain_Color_Remap_Nodes.Item2
                        )
                    );

                    MeshDraft_.colors[Index_XZ1] = Terrain_Color_Gradient.Evaluate(
                        __.TerrainHeight_GradientAmount_Find(
                            NoiseHeight_X0_Z1,
                            Terrain_Color_Remap_Nodes.Item1,
                            Terrain_Color_Remap_Nodes.Item2
                        )
                    );

                    MeshDraft_.colors[Index_XZ2] = Terrain_Color_Gradient.Evaluate(
                        __.TerrainHeight_GradientAmount_Find(
                            NoiseHeight_X1_Z1,
                            Terrain_Color_Remap_Nodes.Item1,
                            Terrain_Color_Remap_Nodes.Item2
                        )
                    );

                    MeshDraft_.colors[Index_XZ3] = Terrain_Color_Gradient.Evaluate(
                        __.TerrainHeight_GradientAmount_Find(
                            NoiseHeight_X0_Z0,
                            Terrain_Color_Remap_Nodes.Item1,
                            Terrain_Color_Remap_Nodes.Item2
                        )
                    );

                    MeshDraft_.colors[Index_XZ4] = Terrain_Color_Gradient.Evaluate(
                        __.TerrainHeight_GradientAmount_Find(
                            NoiseHeight_X1_Z1,
                            Terrain_Color_Remap_Nodes.Item1,
                            Terrain_Color_Remap_Nodes.Item2
                        )
                    );

                    MeshDraft_.colors[Index_XZ5] = Terrain_Color_Gradient.Evaluate(
                        __.TerrainHeight_GradientAmount_Find(
                            NoiseHeight_X1_Z0,
                            Terrain_Color_Remap_Nodes.Item1,
                            Terrain_Color_Remap_Nodes.Item2
                        )
                    );

                    Vector3 NormalVector_X0Z0_X0Z1_X1Z1 = Vector3.Cross(
                        Vertex_Position_X0_Z1 - Vertex_Position_X0_Z0,
                        Vertex_Position_X1_Z1 - Vertex_Position_X0_Z0
                    ).normalized;

                    Vector3 NormalVector_X0Z0_X1Z0_X1Z1 = Vector3.Cross(
                        Vertex_Position_X1_Z1 - Vertex_Position_X0_Z0,
                        Vertex_Position_X1_Z0 - Vertex_Position_X0_Z0
                    ).normalized;

                    MeshDraft_.normals[Index_XZ0] = NormalVector_X0Z0_X0Z1_X1Z1;
                    MeshDraft_.normals[Index_XZ1] = NormalVector_X0Z0_X0Z1_X1Z1;
                    MeshDraft_.normals[Index_XZ2] = NormalVector_X0Z0_X0Z1_X1Z1;
                    MeshDraft_.normals[Index_XZ3] = NormalVector_X0Z0_X1Z0_X1Z1;
                    MeshDraft_.normals[Index_XZ4] = NormalVector_X0Z0_X1Z0_X1Z1;
                    MeshDraft_.normals[Index_XZ5] = NormalVector_X0Z0_X1Z0_X1Z1;

                    MeshDraft_.triangles[Index_XZ0] = Index_XZ0;
                    MeshDraft_.triangles[Index_XZ1] = Index_XZ1;
                    MeshDraft_.triangles[Index_XZ2] = Index_XZ2;
                    MeshDraft_.triangles[Index_XZ3] = Index_XZ3;
                    MeshDraft_.triangles[Index_XZ4] = Index_XZ4;
                    MeshDraft_.triangles[Index_XZ5] = Index_XZ5;

                    Vector2 UV_X0_Z0 = __.TerrainVertex_UV_Cube_Find(
                        Vertex_Position_X0_Z0,
                        Terrain_Center_Position,
                        Terrain_Size_Meters
                    );

                    Vector2 UV_X0_Z1 = __.TerrainVertex_UV_Cube_Find(
                        Vertex_Position_X0_Z1,
                        Terrain_Center_Position,
                        Terrain_Size_Meters
                    );

                    Vector2 UV_X1_Z0 = __.TerrainVertex_UV_Cube_Find(
                        Vertex_Position_X1_Z0,
                        Terrain_Center_Position,
                        Terrain_Size_Meters
                    );

                    Vector2 UV_X1_Z1 = __.TerrainVertex_UV_Cube_Find(
                        Vertex_Position_X1_Z1,
                        Terrain_Center_Position,
                        Terrain_Size_Meters
                    );

                    MeshDraft_.uv[Index_XZ0] = UV_X0_Z0;
                    MeshDraft_.uv[Index_XZ1] = UV_X0_Z1;
                    MeshDraft_.uv[Index_XZ2] = UV_X1_Z1;
                    MeshDraft_.uv[Index_XZ3] = UV_X0_Z0;
                    MeshDraft_.uv[Index_XZ4] = UV_X1_Z1;
                    MeshDraft_.uv[Index_XZ5] = UV_X1_Z0;

                    MeshDraft_.uv2[Index_XZ0] = MeshDraft_.uv[Index_XZ0];
                    MeshDraft_.uv2[Index_XZ1] = MeshDraft_.uv[Index_XZ1];
                    MeshDraft_.uv2[Index_XZ2] = MeshDraft_.uv[Index_XZ2];
                    MeshDraft_.uv2[Index_XZ3] = MeshDraft_.uv[Index_XZ3];
                    MeshDraft_.uv2[Index_XZ4] = MeshDraft_.uv[Index_XZ4];
                    MeshDraft_.uv2[Index_XZ5] = MeshDraft_.uv[Index_XZ5];

                    MeshDraft_.uv3[Index_XZ0] = MeshDraft_.uv[Index_XZ0];
                    MeshDraft_.uv3[Index_XZ1] = MeshDraft_.uv[Index_XZ1];
                    MeshDraft_.uv3[Index_XZ2] = MeshDraft_.uv[Index_XZ2];
                    MeshDraft_.uv3[Index_XZ3] = MeshDraft_.uv[Index_XZ3];
                    MeshDraft_.uv3[Index_XZ4] = MeshDraft_.uv[Index_XZ4];
                    MeshDraft_.uv3[Index_XZ5] = MeshDraft_.uv[Index_XZ5];

                    MeshDraft_.uv4[Index_XZ0] = MeshDraft_.uv[Index_XZ0];
                    MeshDraft_.uv4[Index_XZ1] = MeshDraft_.uv[Index_XZ1];
                    MeshDraft_.uv4[Index_XZ2] = MeshDraft_.uv[Index_XZ2];
                    MeshDraft_.uv4[Index_XZ3] = MeshDraft_.uv[Index_XZ3];
                    MeshDraft_.uv4[Index_XZ4] = MeshDraft_.uv[Index_XZ4];
                    MeshDraft_.uv4[Index_XZ5] = MeshDraft_.uv[Index_XZ5];
                }
            }
        } // end method

        private void Position_AndRotation_Random_AboveTerrain_Generate(
            out Vector3 Position,
            out Quaternion Rotation
        )
        {
            Random_State_Restore();

            Position = new(
                Random.Range(-Terrain_Size_Meters.x / 2f, Terrain_Size_Meters.x / 2f),
                1f + Terrain_Size_Meters.y,
                Random.Range(-Terrain_Size_Meters.z / 2f, Terrain_Size_Meters.z / 2f)
            );

            Rotation = Quaternion.Euler(
                0,
                Random.Range(-180f, 180f),
                0
            );

            Random_State_Backup();
        }

        private void Position_AtPositionAboveTerrain_OnTerrain_Find(
            in Vector3 Position_AboveTerrain,
            out Vector3 Position_OnTerrain,
            out bool Position_OnTerrain_CanFind
        )
        {
            Position_OnTerrain = Position_AboveTerrain;
            Position_OnTerrain_CanFind = false;
            RaycastHit RaycastHit_;

            if (
                Physics.Raycast(Position_AboveTerrain, Vector3.down, out RaycastHit_)
                && RaycastHit_.collider.CompareTag("terrain")
            )
            {
                Position_OnTerrain = RaycastHit_.point;
                Position_OnTerrain_CanFind = true;
            }
        }
        // end level 2 helpers

        // begin level 3+ helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Terrain_Height_WithWaterDepth_Find(in float Height, out float Height_WithWaterDepth)
        {
            Height_WithWaterDepth = Height;
            float Height_WaterBottom = -Water_Depth_Meters + Terrain_Size_Meters.y / 2f;

            if (Height_WithWaterDepth < Height_WaterBottom)
            {
                Height_WithWaterDepth = Height_WaterBottom;
            }
        }
        // end level 3+ helpers
    } // end class

    namespace TerrainGenerator2_
    {
        [Serializable]
        public class Palette_HSV
        {
            public ColorHSV Color_Terrain_Shore;
            public ColorHSV Color_Terrain_Ground;
            public ColorHSV Color_Water_Shallow;
            public ColorHSV Color_Water_Deep;
            public ColorHSV Color_Skybox_Sky;
            public ColorHSV Color_Skybox_Horizon;
            public ColorHSV Color_Skybox_Ground;

            public Palette_HSV()
            {
                // Do nothing.
            }

            public Palette_HSV(Palette_HSV Original)
            {
                Color_Terrain_Shore = Original.Color_Terrain_Shore;
                Color_Terrain_Ground = Original.Color_Terrain_Ground;
                Color_Water_Shallow = Original.Color_Water_Shallow;
                Color_Water_Deep = Original.Color_Water_Deep;
                Color_Skybox_Sky = Original.Color_Skybox_Sky;
                Color_Skybox_Horizon = Original.Color_Skybox_Horizon;
                Color_Skybox_Ground = Original.Color_Skybox_Ground;
            }
        }

        [Serializable]
        public class Palette
        {
            public static void FromPaletteHSV_Create(in Palette_HSV Palette_HSV_, out Palette Palette_)
            {
                Palette_ = new(Palette_HSV_);
            }

            public Color Color_Terrain_Shore;
            public Color Color_Terrain_Ground;
            public Color Color_Water_Shallow;
            public Color Color_Water_Deep;
            public Color Color_Skybox_Sky;
            public Color Color_Skybox_Horizon;
            public Color Color_Skybox_Ground;

            public Palette()
            {
                // Do nothing.
            }

            public Palette(Palette Original)
            {
                Color_Terrain_Shore = Original.Color_Terrain_Shore;
                Color_Terrain_Ground = Original.Color_Terrain_Ground;
                Color_Water_Shallow = Original.Color_Water_Shallow;
                Color_Water_Deep = Original.Color_Water_Deep;
                Color_Skybox_Sky = Original.Color_Skybox_Sky;
                Color_Skybox_Horizon = Original.Color_Skybox_Horizon;
                Color_Skybox_Ground = Original.Color_Skybox_Ground;
            }

            public Palette(Palette_HSV Palette_HSV_)
            {
                Color_Terrain_Shore = Palette_HSV_.Color_Terrain_Shore.ToColor();
                Color_Terrain_Ground = Palette_HSV_.Color_Terrain_Ground.ToColor();
                Color_Water_Shallow = Palette_HSV_.Color_Water_Shallow.ToColor();
                Color_Water_Deep = Palette_HSV_.Color_Water_Deep.ToColor();
                Color_Skybox_Sky = Palette_HSV_.Color_Skybox_Sky.ToColor();
                Color_Skybox_Horizon = Palette_HSV_.Color_Skybox_Horizon.ToColor();
                Color_Skybox_Ground = Palette_HSV_.Color_Skybox_Ground.ToColor();
            }
        }

        [Serializable]
        public class PaletteHSV_Standards
        {
            private static Palette_HSV Standard = new()
            {
                Color_Terrain_Shore = new(30f / 360f, 0.75f, 0.35f, 1f),
                Color_Terrain_Ground = new(75f / 360f, 0.65f, 0.65f, 1f),
                Color_Water_Shallow = new(180f / 360f, 0.5f, 0.65f, 0.75f),
                Color_Water_Deep = new(210f / 360f, 0.55f, 0.6f, 0.75f),
                Color_Skybox_Sky = new(210f / 360f, 0.55f, 1f, 1f),
                Color_Skybox_Horizon = new(180f / 360f, 0.05f, 0.8f, 1f),
                Color_Skybox_Ground = new(90f / 360f, 0.05f, 0.4f, 1f)
            };

            public static void PaletteHSV_Create(out Palette_HSV Palette_HSV_)
            {
                Palette_HSV_ = new(Standard);
            }

            public static void PaletteHSV_OffsetByHSV_Create(in ColorHSV Offset, out Palette_HSV Palette_HSV_)
            {
                PaletteHSV_Create(out Palette_HSV_);
                __.ColorHSV_OffsetBy_Create(Palette_HSV_.Color_Terrain_Shore, Offset, out Palette_HSV_.Color_Terrain_Shore);
                __.ColorHSV_OffsetBy_Create(Palette_HSV_.Color_Terrain_Ground, Offset, out Palette_HSV_.Color_Terrain_Ground);
                __.ColorHSV_OffsetBy_Create(Palette_HSV_.Color_Water_Shallow, Offset, out Palette_HSV_.Color_Water_Shallow);
                __.ColorHSV_OffsetBy_Create(Palette_HSV_.Color_Water_Deep, Offset, out Palette_HSV_.Color_Water_Deep);
                __.ColorHSV_OffsetBy_Create(Palette_HSV_.Color_Skybox_Sky, Offset, out Palette_HSV_.Color_Skybox_Sky);
                __.ColorHSV_OffsetBy_Create(Palette_HSV_.Color_Skybox_Horizon, Offset, out Palette_HSV_.Color_Skybox_Horizon);
                __.ColorHSV_OffsetBy_Create(Palette_HSV_.Color_Skybox_Ground, Offset, out Palette_HSV_.Color_Skybox_Ground);
            }
        }

        public class __
        {
            public static void Terrain_MeshDraft_Create(in int Vertex_Count, out MeshDraft MeshDraft_)
            {
                MeshDraft_ = new()
                {
                    name = "Terrain",
                    vertices = new(Vertex_Count),
                    triangles = new(Vertex_Count),
                    normals = new(Vertex_Count),
                    colors = new(Vertex_Count),
                    uv = new(Vertex_Count),
                    uv2 = new(Vertex_Count),
                    uv3 = new(Vertex_Count),
                    uv4 = new(Vertex_Count),
                };

                for (int Index_ = 0; Index_ < Vertex_Count; Index_ += 1)
                {
                    MeshDraft_.vertices.Add(Vector3.zero);
                    MeshDraft_.triangles.Add(0);
                    MeshDraft_.normals.Add(Vector3.zero);
                    MeshDraft_.colors.Add(Color.black);
                    MeshDraft_.uv.Add(Vector2.zero);
                    MeshDraft_.uv2.Add(Vector2.zero);
                    MeshDraft_.uv3.Add(Vector2.zero);
                    MeshDraft_.uv4.Add(Vector2.zero);
                }
            }

            public static void Terrain_NoiseHeight_Find(
                in int Coordinate_X,
                in int Coordinate_Z,
                in int SegmentCount_X,
                in int SegmentCount_Z,
                in Vector2 Noise_Offsets,
                in FastNoise Noise_,
                out float Height
            )
            {
                float Noise_Index_X = Noise_Offsets.x + Coordinate_X / (float)SegmentCount_X;
                float Noise_Index_Z = Noise_Offsets.y + Coordinate_Z / (float)SegmentCount_Z;
                Height = Noise_.GetNoise01(Noise_Index_X, Noise_Index_Z);
            }

            public static void Mesh_AndCompoundDraft_AndMeshFilter_Link(
                ref Mesh Mesh_,
                ref CompoundMeshDraft CompoundMeshDraft_,
                ref MeshFilter MeshFilter_
            )
            {
                if (Mesh_ == null)
                {
                    Mesh_ = CompoundMeshDraft_.ToMeshWithSubMeshes();
                }
                else
                {
                    CompoundMeshDraft_.ToMeshWithSubMeshes(Mesh_);
                }

                MeshFilter_.sharedMesh = Mesh_;
            }

            public static void Platform_MeshDraft_Generate(
                in float Radius_Meters,
                in float Height_Meters,
                in int Segment_Count,
                out MeshDraft MeshDraft_
            )
            {
                float Angle_PerSegment_Degrees = 360f / Segment_Count;
                float Angle_Current_Degrees = 0f;

                List<Vector3> Ring_Lower = new(Segment_Count);
                List<Vector3> Ring_Upper = new(Segment_Count);

                for (int _ = 0; _ < Segment_Count; _ += 1)
                {
                    Ring_Lower.Add(
                        Geometry.PointOnCircle3XZ(
                            Radius_Meters + Height_Meters,
                            Angle_Current_Degrees
                        )
                        + Vector3.down * Height_Meters
                    );

                    Ring_Upper.Add(
                        Geometry.PointOnCircle3XZ(
                            Radius_Meters,
                            Angle_Current_Degrees
                        )
                    );

                    Angle_Current_Degrees += Angle_PerSegment_Degrees;
                }

                MeshDraft_ = new MeshDraft()
                {
                    name = "Platform"
                }
                    .AddFlatQuadBand(Ring_Lower, Ring_Upper, false)
                ;

                List<Vector3> Ring_Lower_Reversed = new(Ring_Lower);
                Ring_Lower_Reversed.Reverse();
                Color Ring_Lower_Color = new(0.5f, 0.5f, 0.5f, 1f);
                Color Ring_Upper_Color = new(0.8f, 0.8f, 0.8f, 1f);

                MeshDraft_
                    .AddTriangleFan(Ring_Lower_Reversed, Vector3.down)
                    .Paint(Ring_Lower_Color)
                ;

                MeshDraft_
                    .AddTriangleFan(Ring_Upper, Vector3.up)
                    .Paint(Ring_Upper_Color)
                ;
            }

            public static void ColorHSV_OffsetBy_Create(in ColorHSV ColorHSV_, in ColorHSV Offset, out ColorHSV ColorHSV_WithOffset)
            {
                float Hue = ColorHSV_.h;
                float Saturation = ColorHSV_.s;
                float Value = ColorHSV_.v;
                float Alpha = ColorHSV_.a;
                float Hue_Offset = Offset.h;
                float Saturation_Offset = Offset.s;
                float Value_Offset = Offset.v;
                float Alpha_Offset = Offset.a;
                Hue += Hue_Offset;
                Hue %= 1f;
                Saturation += Saturation_Offset;
                Saturation = Mathf.Clamp01(Saturation);
                Value += Value_Offset;
                Value = Mathf.Clamp01(Value);
                Alpha += Alpha_Offset;
                Alpha = Mathf.Clamp01(Alpha);
                ColorHSV_WithOffset = new(Hue, Saturation, Value, Alpha);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float TerrainHeight_GradientAmount_Find(float Height, float Remap_Node1, float Remap_Node2)
            {
                Height = Mathf.Clamp01(Height);
                Remap_Node1 = Mathf.Clamp01(Remap_Node1);
                Remap_Node2 = Mathf.Clamp01(Remap_Node2);

                if (Remap_Node1 > Remap_Node2)
                {
                    (Remap_Node2, Remap_Node1) = (Remap_Node1, Remap_Node2);
                }

                float Result_ = Height;

                if (Result_ < Remap_Node1)
                {
                    Result_ = 0f;
                }
                else if (Result_ > Remap_Node2)
                {
                    Result_ = 1f;
                }
                else
                {
                    Result_ =
                        - Remap_Node1 * (Remap_Node2 - Remap_Node1)
                        + Result_ / (Remap_Node2 - Remap_Node1)
                    ;
                }

                return Result_;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector2 TerrainVertex_UV_Sphere_Find(
                Vector3 Vertex_Position,
                Vector3 Terrain_Center_Position_,
                Vector3 Terrain_Size_Meters_
            )
            {
                Vector3 Vertex_Position_Recentered =
                    Vertex_Position
                    + Vector3.left * Terrain_Size_Meters_.x / 2f
                    + Vector3.down * Terrain_Size_Meters_.y / 2f
                    + Vector3.back * Terrain_Size_Meters_.z / 2f
                ;

                Vector3 Vertex_Position_FromCenter = Vertex_Position_Recentered - Terrain_Center_Position_;

                Vector2 Result_ = new(
                    0.5f
                    + Mathf.Atan2(
                        Vertex_Position_FromCenter.z,
                        Vertex_Position_FromCenter.x
                    )
                    / (2 * Mathf.PI),

                    0.5f
                    + Mathf.Asin(Vertex_Position_FromCenter.y)
                    / Mathf.PI
                );

                return Result_;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector2 TerrainVertex_UV_Cube_Find(
                Vector3 Vertex_Position,
                Vector3 Terrain_Center_Position_,
                Vector3 Terrain_Size_Meters_
            )
            {
                Vector3 Vertex_Position_Recentered =
                    Vertex_Position
                    + Vector3.left * Terrain_Size_Meters_.x / 2f
                    + Vector3.down * Terrain_Size_Meters_.y / 2f
                    + Vector3.back * Terrain_Size_Meters_.z / 2f
                ;

                Vector3 Vertex_Position_FromCenter = Vertex_Position_Recentered - Terrain_Center_Position_;
                Vector2 Result_ = new();

                if (
                    Vertex_Position_FromCenter.x <= -Terrain_Size_Meters_.x / 2f
                    || Vertex_Position_FromCenter.x >= Terrain_Size_Meters_.x / 2f
                )
                {
                    Result_.x = 0.5f + Vertex_Position_FromCenter.y / (Terrain_Size_Meters_.y / 2f);
                    Result_.y = 0.5f + Vertex_Position_FromCenter.z / (Terrain_Size_Meters_.z / 2f);
                }
                else if (
                    Vertex_Position_FromCenter.y <= -Terrain_Size_Meters_.y / 2f
                    || Vertex_Position_FromCenter.y >= Terrain_Size_Meters_.y / 2f
                )
                {
                    Result_.x = 0.5f + Vertex_Position_FromCenter.x / (Terrain_Size_Meters_.x / 2f);
                    Result_.y = 0.5f + Vertex_Position_FromCenter.z / (Terrain_Size_Meters_.z / 2f);
                }
                else if (
                    Vertex_Position_FromCenter.z <= -Terrain_Size_Meters_.z / 2f
                    || Vertex_Position_FromCenter.z >= Terrain_Size_Meters_.z / 2f
                )
                {
                    Result_.x = 0.5f + Vertex_Position_FromCenter.x / (Terrain_Size_Meters_.x / 2f);
                    Result_.y = 0.5f + Vertex_Position_FromCenter.y / (Terrain_Size_Meters_.y / 2f);
                }
                else
                {
                    Result_.x = 0.5f
                    + (Vertex_Position_FromCenter.x + Vertex_Position_FromCenter.z)
                    / ((Terrain_Size_Meters_.x + Terrain_Size_Meters_.z) / 2f)
                    ;

                    Result_.y = 0.5f
                    + (Vertex_Position_FromCenter.y + Vertex_Position_FromCenter.z)
                    / ((Terrain_Size_Meters_.y + Terrain_Size_Meters_.z) / 2f)
                    ;
                }

                return Result_;
            } // end method
        } // end class
    } // end namespace
} // end namespace

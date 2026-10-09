using System.Collections.Generic;
using System.IO;
using System.Linq;
using BumperRoyale.Car;
using UnityEditor;
using UnityEngine;

namespace BumperRoyale.Setup
{
    /// <summary>
    /// One-off setup steps that the MCP tools can't do directly: creating ScriptableObject and
    /// physics material assets, and editing Build Settings. Each step is safe to re-run.
    /// </summary>
    public static class BumperRoyaleSetup
    {
        private const string DataFolder = "Assets/Data";
        public const string PlayerStatsPath = DataFolder + "/PlayerCarStats.asset";
        public const string BotStatsPath = DataFolder + "/BotCarStats.asset";
        public const string CarPhysicsMaterialPath = DataFolder + "/CarSlippery.asset";
        public const string ArenaScenePath = "Assets/Scenes/Arena.unity";
        public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string MeshFolder = "Assets/Art/Meshes";
        public const string ArenaDiscMeshPath = MeshFolder + "/ArenaDisc.asset";

        // Enough sides that the arena edge reads as a true circle (max deviation ~5 mm at radius 15).
        private const int ArenaDiscSegments = 128;
        private const int CarWheelSegments = 28;

        [MenuItem("Bumper Royale/Create CarStats Assets")]
        public static void CreateCarStatsAssets()
        {
            EnsureFolder(DataFolder);

            if (AssetDatabase.LoadAssetAtPath<CarStats>(PlayerStatsPath) == null)
            {
                CarStats player = ScriptableObject.CreateInstance<CarStats>();
                AssetDatabase.CreateAsset(player, PlayerStatsPath);
            }

            if (AssetDatabase.LoadAssetAtPath<CarStats>(BotStatsPath) == null)
            {
                CarStats bot = ScriptableObject.CreateInstance<CarStats>();
                // Bots are a touch slower than the player so the player has an edge.
                bot.maxSpeed = 12.5f;
                bot.acceleration = 16f;
                bot.aggression = 0.6f;
                bot.edgeAvoidMargin = 5f;
                bot.edgeAvoidStrength = 2f;
                AssetDatabase.CreateAsset(bot, BotStatsPath);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Bumper Royale: CarStats assets ready in " + DataFolder);
        }

        [MenuItem("Bumper Royale/Create Car Physics Material")]
        public static void CreateCarPhysicsMaterial()
        {
            EnsureFolder(DataFolder);

            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(CarPhysicsMaterialPath);
            if (material == null)
            {
                material = new PhysicsMaterial("CarSlippery");
                AssetDatabase.CreateAsset(material, CarPhysicsMaterialPath);
            }

            material.staticFriction = 0.05f;
            material.dynamicFriction = 0.05f;
            material.bounciness = 0.15f;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
            material.bounceCombine = PhysicsMaterialCombine.Average;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            Debug.Log("Bumper Royale: car physics material ready at " + CarPhysicsMaterialPath);
        }

        [MenuItem("Bumper Royale/Configure Build Settings")]
        public static void ConfigureBuildSettings()
        {
            string[] ordered = { MainMenuScenePath, ArenaScenePath };
            foreach (string path in ordered)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError("Bumper Royale: " + path + " does not exist yet.");
                    return;
                }
            }

            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(s => !ordered.Contains(s.path) && s.path != "Assets/Scenes/SampleScene.unity")
                .ToList();
            scenes.InsertRange(0, ordered.Select(p => new EditorBuildSettingsScene(p, true)));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Bumper Royale: Build Settings are MainMenu (0), Arena (1).");
        }

        /// <summary>
        /// Smooth replacement for Unity's 20-sided built-in Cylinder, with the same dimensions
        /// (radius 0.5, height 2, centered), so it can be swapped in without changing transforms.
        /// </summary>
        [MenuItem("Bumper Royale/Create Arena Disc Mesh")]
        public static void CreateArenaDiscMesh()
        {
            SaveMesh(ArenaDiscMeshPath, "ArenaDisc", mesh => BuildCylinder(mesh, ArenaDiscSegments));
        }

        /// <summary>
        /// Rounded-box and wheel meshes for the car prefab. Each rounded box is built at its real size
        /// so the corner radius stays uniform (scaling a rounded box would squash the corners).
        /// </summary>
        [MenuItem("Bumper Royale/Create Car Meshes")]
        public static void CreateCarMeshes()
        {
            const int corner = 4;
            SaveMesh(MeshFolder + "/CarBody.asset", "CarBody",
                mesh => BuildRoundedBox(mesh, new Vector3(1.66f, 0.46f, 2.6f), 0.16f, corner));
            // Glass cabin with a body-colored roof slab on top, so the cabin sides read as windows.
            SaveMesh(MeshFolder + "/CarCabin.asset", "CarCabin",
                mesh => BuildRoundedBox(mesh, new Vector3(1.3f, 0.42f, 1.28f), 0.14f, corner));
            SaveMesh(MeshFolder + "/CarRoof.asset", "CarRoof",
                mesh => BuildRoundedBox(mesh, new Vector3(1.36f, 0.12f, 1.34f), 0.06f, corner));
            SaveMesh(MeshFolder + "/CarBumper.asset", "CarBumper",
                mesh => BuildRoundedBox(mesh, new Vector3(1.72f, 0.16f, 2.72f), 0.08f, corner));
            SaveMesh(MeshFolder + "/CarWheel.asset", "CarWheel", mesh => BuildCylinder(mesh, CarWheelSegments));
        }

        private static void SaveMesh(string path, string name, System.Action<Mesh> build)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));

            // Rebuild in place when the asset exists so its GUID (and every reference to it) is kept.
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;
            if (isNew)
                mesh = new Mesh { name = name };
            mesh.Clear();
            build(mesh);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            if (isNew)
                AssetDatabase.CreateAsset(mesh, path);
            else
                EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssets();
            Debug.Log("Bumper Royale: mesh ready at " + path);
        }

        /// <summary>Cylinder along Y: radius 0.5, height 2, centered, like the built-in Cylinder.</summary>
        private static void BuildCylinder(Mesh mesh, int n)
        {
            const float radius = 0.5f;
            const float halfHeight = 1f;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            // Caps: a center vertex plus a ring, with planar UVs.
            for (int cap = 0; cap < 2; cap++)
            {
                bool top = cap == 0;
                float y = top ? halfHeight : -halfHeight;
                Vector3 normal = top ? Vector3.up : Vector3.down;
                int center = vertices.Count;
                vertices.Add(new Vector3(0f, y, 0f));
                normals.Add(normal);
                uvs.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i < n; i++)
                {
                    float angle = 2f * Mathf.PI * i / n;
                    float x = Mathf.Cos(angle) * radius;
                    float z = Mathf.Sin(angle) * radius;
                    vertices.Add(new Vector3(x, y, z));
                    normals.Add(normal);
                    uvs.Add(new Vector2(x + 0.5f, z + 0.5f));
                }
                for (int i = 0; i < n; i++)
                {
                    int a = center + 1 + i;
                    int b = center + 1 + (i + 1) % n;
                    if (top)
                        triangles.AddRange(new[] { center, b, a });
                    else
                        triangles.AddRange(new[] { center, a, b });
                }
            }

            // Side: its own vertices so the rim edge stays sharp while the wall shades smoothly.
            int side = vertices.Count;
            for (int i = 0; i <= n; i++)
            {
                float angle = 2f * Mathf.PI * i / n;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                float u = (float)i / n;
                vertices.Add(outward * radius + Vector3.down * halfHeight);
                normals.Add(outward);
                uvs.Add(new Vector2(u, 0f));
                vertices.Add(outward * radius + Vector3.up * halfHeight);
                normals.Add(outward);
                uvs.Add(new Vector2(u, 1f));
            }
            for (int i = 0; i < n; i++)
            {
                int b0 = side + i * 2;
                int t0 = b0 + 1;
                int b1 = b0 + 2;
                int t1 = b0 + 3;
                triangles.AddRange(new[] { b0, t0, t1, b0, t1, b1 });
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
        }

        /// <summary>
        /// Box of the given size whose edges and corners are rounded with the given radius.
        /// Each face is a grid that is dense near the edges; every grid point is pushed onto the
        /// rounded surface, with <paramref name="cornerSegments"/> steps per 45 degrees of arc.
        /// </summary>
        private static void BuildRoundedBox(Mesh mesh, Vector3 size, float radius, int cornerSegments)
        {
            Vector3 half = size * 0.5f;
            radius = Mathf.Min(radius, half.x, half.y, half.z);
            Vector3 innerHalf = half - Vector3.one * radius;
            float[][] coords =
            {
                EdgeDenseCoords(half.x, radius, cornerSegments),
                EdgeDenseCoords(half.y, radius, cornerSegments),
                EdgeDenseCoords(half.z, radius, cornerSegments),
            };

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            for (int axis = 0; axis < 3; axis++)
            {
                int uAxis = (axis + 1) % 3;
                int vAxis = (axis + 2) % 3;
                float[] us = coords[uAxis];
                float[] vs = coords[vAxis];

                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 outward = Vector3.zero;
                    outward[axis] = sign;
                    Vector3 uDir = Vector3.zero;
                    uDir[uAxis] = 1f;
                    Vector3 vDir = Vector3.zero;
                    vDir[vAxis] = 1f;
                    // Unity treats a triangle as front-facing when Cross(b - a, c - a) points outward.
                    bool flip = Vector3.Dot(Vector3.Cross(vDir, uDir), outward) < 0f;

                    int start = vertices.Count;
                    for (int j = 0; j < vs.Length; j++)
                    {
                        for (int i = 0; i < us.Length; i++)
                        {
                            Vector3 p = Vector3.zero;
                            p[axis] = sign * half[axis];
                            p[uAxis] = us[i];
                            p[vAxis] = vs[j];
                            Vector3 inner = new Vector3(
                                Mathf.Clamp(p.x, -innerHalf.x, innerHalf.x),
                                Mathf.Clamp(p.y, -innerHalf.y, innerHalf.y),
                                Mathf.Clamp(p.z, -innerHalf.z, innerHalf.z));
                            Vector3 normal = (p - inner).normalized;
                            vertices.Add(inner + normal * radius);
                            normals.Add(normal);
                            uvs.Add(new Vector2((float)i / (us.Length - 1), (float)j / (vs.Length - 1)));
                        }
                    }

                    for (int j = 0; j < vs.Length - 1; j++)
                    {
                        for (int i = 0; i < us.Length - 1; i++)
                        {
                            int a = start + j * us.Length + i;
                            int b = a + 1;
                            int c = a + us.Length;
                            int d = c + 1;
                            if (flip)
                                triangles.AddRange(new[] { a, d, c, a, b, d });
                            else
                                triangles.AddRange(new[] { a, c, d, a, d, b });
                        }
                    }
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
        }

        /// <summary>
        /// Face-grid coordinates from -half to half: even steps of arc angle in each rounded band,
        /// and no extra points across the flat middle.
        /// </summary>
        private static float[] EdgeDenseCoords(float half, float radius, int segments)
        {
            var result = new List<float>();
            float flat = half - radius;
            // A face point radius * tan(angle) beyond the flat part lands at that angle on the arc.
            // Each face covers 0-45 degrees of an edge's arc; the neighbouring face covers the rest.
            for (int k = 0; k <= segments; k++)
            {
                float angle = (segments - k) / (float)segments * 45f * Mathf.Deg2Rad;
                result.Add(-flat - radius * Mathf.Tan(angle));
            }
            for (int k = 0; k <= segments; k++)
            {
                float angle = k / (float)segments * 45f * Mathf.Deg2Rad;
                float value = flat + radius * Mathf.Tan(angle);
                if (value > result[result.Count - 1] + 1e-5f)
                    result.Add(value);
            }
            return result.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}

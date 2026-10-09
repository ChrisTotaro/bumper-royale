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
        public const string ArenaDiscMeshPath = "Assets/Art/Meshes/ArenaDisc.asset";

        // Enough sides that the arena edge reads as a true circle (max deviation ~5 mm at radius 15).
        private const int ArenaDiscSegments = 128;

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
            EnsureFolder(Path.GetDirectoryName(ArenaDiscMeshPath).Replace('\\', '/'));

            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ArenaDiscMeshPath);
            bool isNew = mesh == null;
            if (isNew)
                mesh = new Mesh { name = "ArenaDisc" };

            const float radius = 0.5f;
            const float halfHeight = 1f;
            int n = ArenaDiscSegments;

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

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            if (isNew)
                AssetDatabase.CreateAsset(mesh, ArenaDiscMeshPath);
            else
                EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssets();
            Debug.Log("Bumper Royale: arena disc mesh ready at " + ArenaDiscMeshPath);
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

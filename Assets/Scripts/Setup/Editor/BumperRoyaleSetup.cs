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

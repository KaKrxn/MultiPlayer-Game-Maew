using UnityEngine;
using UnityEditor;
using Blocks.Gameplay.Core;
using System.IO;
using FileGame.Core.UI;
using UnityEngine.UIElements;

namespace FileGame.Core.EditorTools
{
    [InitializeOnLoad]
    public class GeneratePlayerStatsAssets
    {
        static GeneratePlayerStatsAssets()
        {
            EditorApplication.delayCall += GenerateAssets;
        }

        private static void GenerateAssets()
        {
            string folderPath = "Assets/FileGame/Core/Data/Stats";
            if (!AssetDatabase.IsValidFolder("Assets/FileGame/Core/Data"))
            {
                AssetDatabase.CreateFolder("Assets/FileGame/Core", "Data");
            }
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/FileGame/Core/Data", "Stats");
                AssetDatabase.Refresh();
            }

            string configPath = $"{folderPath}/StaminaDrivenStatusConfig.asset";
            bool changed = false;
            changed |= CreateStat(folderPath, "Vitality", 100f, 100f, 0f, 0f, 0f); // Hidden life stat
            changed |= CreateStat(folderPath, "Health", 100f, 100f, 0f, 15f, 1.5f); // Main Energy Bar (sprint/jump)
            changed |= CreateStat(folderPath, "Energy", 100f, 100f, 0f, 0f, 0f); // Fast Regen Action Bar
            changed |= CreateStat(folderPath, "Hunger", 0f, 100f, 0f, 0f, 0f);
            changed |= CreateStat(folderPath, "Pain", 0f, 100f, 0f, 0f, 0f);
            changed |= CreateStat(folderPath, "Weight", 0f, 100f, 0f, 0f, 0f);

            if (changed || !AssetDatabase.LoadAssetAtPath<StatsConfig>(configPath))
            {
                if (AssetDatabase.LoadAssetAtPath<StatsConfig>(configPath) == null)
                {
                    StatsConfig config = ScriptableObject.CreateInstance<StatsConfig>();
                    config.primaryStatName = "Vitality";
                    config.stats = new System.Collections.Generic.List<StatDefinition>();
                    config.stats.Add(AssetDatabase.LoadAssetAtPath<StatDefinition>($"{folderPath}/Vitality.asset"));
                    config.stats.Add(AssetDatabase.LoadAssetAtPath<StatDefinition>($"{folderPath}/Health.asset"));
                    config.stats.Add(AssetDatabase.LoadAssetAtPath<StatDefinition>($"{folderPath}/Energy.asset"));
                    config.stats.Add(AssetDatabase.LoadAssetAtPath<StatDefinition>($"{folderPath}/Hunger.asset"));
                    config.stats.Add(AssetDatabase.LoadAssetAtPath<StatDefinition>($"{folderPath}/Pain.asset"));
                    config.stats.Add(AssetDatabase.LoadAssetAtPath<StatDefinition>($"{folderPath}/Weight.asset"));

                    AssetDatabase.CreateAsset(config, configPath);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[PlayerStatusUI] Created StaminaDrivenStatusConfig!");
                }
                else
                {
                    // Force update existing config
                    StatsConfig config = AssetDatabase.LoadAssetAtPath<StatsConfig>(configPath);
                    config.primaryStatName = "Vitality";
                    config.stats = new System.Collections.Generic.List<StatDefinition>();
                    config.stats.Add(AssetDatabase.LoadAssetAtPath<StatDefinition>($"{folderPath}/Vitality.asset"));
                    config.stats.Add(AssetDatabase.LoadAssetAtPath<StatDefinition>($"{folderPath}/Health.asset"));
                    config.stats.Add(AssetDatabase.LoadAssetAtPath<StatDefinition>($"{folderPath}/Energy.asset"));
                    config.stats.Add(AssetDatabase.LoadAssetAtPath<StatDefinition>($"{folderPath}/Hunger.asset"));
                    config.stats.Add(AssetDatabase.LoadAssetAtPath<StatDefinition>($"{folderPath}/Pain.asset"));
                    config.stats.Add(AssetDatabase.LoadAssetAtPath<StatDefinition>($"{folderPath}/Weight.asset"));
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[PlayerStatusUI] Updated existing StaminaDrivenStatusConfig!");
                }
            }
        }

        private static bool CreateStat(string path, string statName, float start, float max, float min, float regen, float delay)
        {
            string assetPath = $"{path}/{statName}.asset";
            StatDefinition stat = AssetDatabase.LoadAssetAtPath<StatDefinition>(assetPath);
            bool createdOrUpdated = false;

            if (stat == null)
            {
                stat = ScriptableObject.CreateInstance<StatDefinition>();
                stat.statName = statName;
                AssetDatabase.CreateAsset(stat, assetPath);
                createdOrUpdated = true;
            }

            // Force update values to ensure correct defaults (like 0 for debuffs, 100 for Vitality)
            if (stat.startingValue != start || stat.maxValue != max || stat.minValue != min || stat.regenRate != regen || stat.regenDelay != delay)
            {
                stat.startingValue = start;
                stat.maxValue = max;
                stat.minValue = min;
                stat.regenRate = regen;
                stat.regenDelay = delay;
                EditorUtility.SetDirty(stat);
                createdOrUpdated = true;
            }

            return createdOrUpdated;
        }

        [MenuItem("Tools/Clean CorePlayer Prefab (Remove Per-Player UI)")]
        public static void CleanPrefab()
        {
            string prefabPath = "Assets/FileGame/Core/Prefab/Player/[Dew] CorePlayer.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var prefabRoot = editingScope.prefabContentsRoot;
                var staminaUIObj = prefabRoot.transform.Find("StaminaUI");
                if (staminaUIObj != null)
                {
                    Object.DestroyImmediate(staminaUIObj.gameObject);
                }

                var survivalSystem = prefabRoot.GetComponentInChildren<PlayerSurvivalSystem>();
                if (survivalSystem == null)
                {
                    prefabRoot.AddComponent<PlayerSurvivalSystem>();
                    Debug.Log("Added PlayerSurvivalSystem to CorePlayer prefab.");
                }

                Debug.Log("Cleaned prefab and ensured PlayerSurvivalSystem is present.");
            }
        }

        [MenuItem("Tools/Setup Global Stamina HUD in Scene")]
        public static void SetupGlobalHUD()
        {
            GameObject hudObj = GameObject.Find("GlobalStaminaHUD");
            if (hudObj == null)
            {
                hudObj = new GameObject("GlobalStaminaHUD");
            }

            var staminaDoc = hudObj.GetComponent<UIDocument>();
            if (staminaDoc == null) staminaDoc = hudObj.AddComponent<UIDocument>();

            VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/FileGame/Core/Scripts/UI/Status/PlayerStatusBar.uxml");
            staminaDoc.visualTreeAsset = uxml;

            // Use SerializedObject to set PanelSettings to avoid property setter assertions
            SerializedObject soDoc = new SerializedObject(staminaDoc);
            soDoc.Update();
            
            string[] settingsGuids = AssetDatabase.FindAssets("BlocksPanelSettings t:PanelSettings");
            if (settingsGuids.Length == 0) settingsGuids = AssetDatabase.FindAssets("t:PanelSettings");

            if (settingsGuids.Length > 0)
            {
                string settingsPath = AssetDatabase.GUIDToAssetPath(settingsGuids[0]);
                PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(settingsPath);
                soDoc.FindProperty("m_PanelSettings").objectReferenceValue = settings;
                Debug.Log($"Assigned PanelSettings: {settingsPath}");
            }
            
            soDoc.FindProperty("m_SortingOrder").floatValue = 10f;
            soDoc.ApplyModifiedProperties();

            if (hudObj.GetComponent<PlayerStatusUIController>() == null)
            {
                hudObj.AddComponent<PlayerStatusUIController>();
            }

            Debug.Log("Successfully setup Global Stamina HUD in current scene!");
        }

        [MenuItem("Tools/Legacy - Patch CorePlayer Prefab for Stamina UI")]
        public static void PatchPrefab()
        {
            // Keeping it but emphasizing it's legacy/per-player
            Debug.LogWarning("Per-player UI is deprecated in favor of Global Stamina HUD (Tools -> Setup Global Stamina HUD in Scene).");
        }
    }
}

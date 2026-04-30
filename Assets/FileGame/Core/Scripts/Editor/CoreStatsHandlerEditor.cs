using UnityEditor;
using UnityEngine;
using Blocks.Gameplay.Core;
using Unity.Netcode;
using System.Reflection;

namespace FileGame.Core.Editor
{
    /// <summary>
    /// Custom editor for CoreStatsHandler that exposes the runtime values of the NetworkList.
    /// This fixes the "no CurrentValue in Inspector" issue by providing a dedicated debug view.
    /// </summary>
    [CustomEditor(typeof(CoreStatsHandler))]
    public class CoreStatsHandlerEditor : UnityEditor.Editor
    {
        private bool showRuntimeStats = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CoreStatsHandler handler = (CoreStatsHandler)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime Debug Info", EditorStyles.boldLabel);
            
            showRuntimeStats = EditorGUILayout.BeginFoldoutHeaderGroup(showRuntimeStats, "Runtime Stat Values (Configurable)");
            
            if (showRuntimeStats)
            {
                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Runtime stats are only available and configurable in Play Mode.", MessageType.Info);
                    
                    if (GUILayout.Button("Open Stats Config Folder"))
                    {
                        var config = serializedObject.FindProperty("statsConfig").objectReferenceValue;
                        if (config != null)
                        {
                            string path = AssetDatabase.GetAssetPath(config);
                            path = path.Substring(0, path.LastIndexOf('/'));
                            EditorUtility.RevealInFinder(path);
                        }
                    }
                    EditorGUILayout.HelpBox("^ Click here to find where to configure Regen Rate, Max Values, etc.", MessageType.Warning);
                }
                else
                {
                    // Use reflection to access the private m_RuntimeStats field
                    // if it's not public. In this project, it's likely private or protected.
                    var field = typeof(CoreStatsHandler).GetField("m_RuntimeStats", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null)
                    {
                        var runtimeStats = field.GetValue(handler) as NetworkList<RuntimeStat>;
                        if (runtimeStats != null && runtimeStats.Count > 0)
                        {
                            EditorGUI.indentLevel++;
                            foreach (var stat in runtimeStats)
                            {
                                string statName = GetStatName(stat.StatHash);
                                float maxVal = handler.GetMaxValue(stat.StatHash);
                                
                                EditorGUI.BeginChangeCheck();
                                float newVal = EditorGUILayout.Slider(statName, stat.CurrentValue, 0, maxVal);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    if (handler.IsServer)
                                    {
                                        float diff = newVal - stat.CurrentValue;
                                        handler.ModifyStat(stat.StatHash, diff, 0, ModificationSource.Direct);
                                    }
                                    else
                                    {
                                        Debug.LogWarning("You can only modify runtime stats on the Server/Host.");
                                    }
                                }
                            }
                            EditorGUI.indentLevel--;
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("No runtime stats initialized yet.", MessageType.Warning);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Could not access m_RuntimeStats field via reflection.", MessageType.Error);
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private string GetStatName(int hash)
        {
            // Try to match common hashes from StatKeys or search project assets
            if (hash == Animator.StringToHash("Vitality")) return "Vitality (LIFE)";
            if (hash == StatKeys.Health) return "Health (Main Energy)";
            if (hash == Animator.StringToHash("Energy")) return "Extra Energy";
            if (hash == Animator.StringToHash("Stamina")) return "Stamina";
            if (hash == Animator.StringToHash("Pain")) return "Pain";
            if (hash == Animator.StringToHash("Hunger")) return "Hunger";
            if (hash == Animator.StringToHash("Weight")) return "Weight";
            
            return $"Unknown ({hash})";
        }
    }
}

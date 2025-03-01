using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RoofTops
{
    /// <summary>
    /// Manages goal values for the game. Attach to any GameObject to configure goal values.
    /// </summary>
    public class GoalValuesManager : MonoBehaviour
    {
        // Direct arrays for each category
        [Header("Distance Goals")]
        public float[] distanceGoals = new float[] { 50f, 100f, 250f, 500f, 750f, 1000f, 1300f, 1600f, 2000f, 3000f };

        [Header("Tridots Goals")]
        public int[] tridotsGoals = new int[] { 1, 3, 15, 20 };

        [Header("Memcard Goals")]
        public int[] memcardGoals = new int[] { 1, 3, 5, 10, 15, 20, 25, 30, 40, 50 };

        // Singleton pattern
        public static GoalValuesManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            ApplyToGoalAchievementManager();
        }

        // Simple getters
        public float[] GetDistanceGoals() => distanceGoals;
        public int[] GetTridotsGoals() => tridotsGoals;
        public int[] GetMemcardGoals() => memcardGoals;

        // Get all enabled categories
        public List<string> GetEnabledCategories()
        {
            List<string> categories = new List<string>();
            categories.Add("Distance");
            categories.Add("Tridots");
            categories.Add("Memcard");
            return categories;
        }

        // Get goal values for a category
        public object GetGoalValues(string categoryName)
        {
            switch (categoryName)
            {
                case "Distance": return distanceGoals;
                case "Tridots": return tridotsGoals;
                case "Memcard": return memcardGoals;
                default: return null;
            }
        }

        // Apply to GoalAchievementManager
        public void ApplyToGoalAchievementManager()
        {
            GoalAchievementManager goalManager = FindFirstObjectByType<GoalAchievementManager>();
            if (goalManager != null)
            {
                goalManager.InitializeGoals();
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GoalValuesManager))]
    public class GoalValuesManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("Edit goal values below. Use the Array size field to add or remove values.", MessageType.Info);
            EditorGUILayout.Space(5);
            
            DrawDefaultInspector();
            
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
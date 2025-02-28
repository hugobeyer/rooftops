using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RoofTops;
using System;

namespace RoofTops
{
    [System.Serializable]
    public class GoalCategory
    {
        public string categoryName;
        public string displayName;
        public object currentGoalValue;
        public int goalIndex;
        public int currentProgressIndex;
        public bool isEnabled = true;
        public string unitSuffix = "";
        
        // Message IDs
        public string startMessageID;
        public string ownMessageID;
        
        // PlayerPrefs keys
        public string goalIndexKey;
        public string currentProgressIndexKey;
        
        // Cached goal values array
        public Array goalValues;
        
        // Function to get current progress value
        public Func<object> getCurrentValueFunc;
        
        public GoalCategory(string name, string display, string startMsg, string ownMsg, string suffix = "")
        {
            categoryName = name;
            displayName = display;
            startMessageID = startMsg;
            ownMessageID = ownMsg;
            unitSuffix = suffix;
            
            goalIndexKey = name + "GoalIndex";
            currentProgressIndexKey = "Current" + name + "GoalIndex";
            
            goalIndex = 0;
            currentProgressIndex = 0;
        }
        
        public bool IsGoalReached()
        {
            if (!isEnabled || getCurrentValueFunc == null || goalValues == null || 
                currentProgressIndex >= goalValues.Length)
                return false;
                
            object currentValue = getCurrentValueFunc();
            object goalValue = goalValues.GetValue(currentProgressIndex);
            
            // Debug log to help diagnose issues
            Debug.Log($"[{displayName}] Checking goal: Current={currentValue}, Goal={goalValue}, ProgressIndex={currentProgressIndex}, GoalIndex={goalIndex}");
            
            // Make sure we're comparing the correct types
            if (currentValue is float currentFloat && goalValue is float goalFloat)
            {
                // For distance goals, ensure we're strictly comparing
                if (categoryName == "Distance")
                {
                    return currentFloat >= goalFloat;
                }
                return currentFloat >= goalFloat;
            }
            else if (currentValue is int currentInt && goalValue is int goalInt)
            {
                return currentInt >= goalInt;
            }
            
            return false;
        }
        
        public object GetCurrentGoalValue()
        {
            if (goalValues != null && goalIndex < goalValues.Length)
            {
                return goalValues.GetValue(goalIndex);
            }
            return null;
        }
        
        public void AdvanceGoal()
        {
            if (goalValues != null && goalValues.Length > 0)
            {
                goalIndex = (goalIndex + 1) % goalValues.Length;
                currentGoalValue = goalValues.GetValue(goalIndex);
                
                PlayerPrefs.SetInt(goalIndexKey, goalIndex);
                PlayerPrefs.Save();
            }
        }
        
        public void LoadGoalIndex()
        {
            goalIndex = PlayerPrefs.GetInt(goalIndexKey, 0);
            if (goalValues != null && goalValues.Length > 0)
            {
                goalIndex = goalIndex % goalValues.Length;
                currentGoalValue = goalValues.GetValue(goalIndex);
            }
        }
        
        public void LoadProgressIndex()
        {
            currentProgressIndex = PlayerPrefs.GetInt(currentProgressIndexKey, 0);
        }
        
        public void SaveProgressIndex()
        {
            PlayerPrefs.SetInt(currentProgressIndexKey, currentProgressIndex);
        }
        
        public void ResetProgressIndex()
        {
            currentProgressIndex = 0;
            SaveProgressIndex();
        }
        
        public void ShowStartMessage()
        {
            Debug.Log($"ShowStartMessage called for {categoryName}, messageID: {startMessageID}, currentGoalValue: {currentGoalValue}");
            
            if (!isEnabled)
            {
                Debug.Log($"Category {categoryName} is not enabled");
                return;
            }
            
            if (string.IsNullOrEmpty(startMessageID))
            {
                Debug.Log($"Category {categoryName} has empty startMessageID");
                return;
            }
            
            if (GameMessageDisplay.Instance == null)
            {
                Debug.Log("GameMessageDisplay.Instance is null");
                return;
            }
            
            if (currentGoalValue == null)
            {
                Debug.Log($"Category {categoryName} has null currentGoalValue");
                return;
            }
            
            Debug.Log($"Showing message for {categoryName} with ID {startMessageID} and value {currentGoalValue}");
            GameMessageDisplay.Instance.ShowMessageByID(startMessageID, currentGoalValue);
        }
        
        public void ShowOwnMessage()
        {
            if (!isEnabled || string.IsNullOrEmpty(ownMessageID) || 
                GameMessageDisplay.Instance == null || currentGoalValue == null)
                return;
                
            GameMessageDisplay.Instance.ShowMessageByID(ownMessageID, currentGoalValue);
        }
    }

    public class GoalAchievementManager : MonoBehaviour
    {
        [Header("Message Delays")]
        public float startMessageDelay = 1.0f;
        public float betweenStartMessagesDelay = 0.5f;
        public float ownMessageDelay = 0.2f;
        
        [Header("Goal Message Settings")]
        [SerializeField] private bool showGoalOnStart = true;
        [SerializeField] private float initialGoalMessageDelay = 8.0f; // Delay before showing goal after game starts
        [SerializeField] private float goalAchievedMessageDelay = 0.1f; // Delay before showing the goal achieved message
        
        [Header("Goal Categories")]
        public List<string> enabledCategories = new List<string>();

        public GoalValuesManager goalValuesManager;
        
        private Dictionary<string, GoalCategory> goalCategories = new Dictionary<string, GoalCategory>();
        
        public static GoalAchievementManager Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            RegisterDefaultGoalCategories();
        }

        void Start()
        {
            if (goalValuesManager == null)
                goalValuesManager = FindObjectOfType<GoalValuesManager>();

            InitializeGoals();
            LoadProgress();
        }

        void Update()
        {
            CheckAllGoals();
        }
        
        private void RegisterDefaultGoalCategories()
        {
            RegisterGoalCategory(
                new GoalCategory("Distance", "Distance", "START_DISTANCE", "OWN_DISTANCE", "m")
                {
                    getCurrentValueFunc = () => GetCurrentDistance()
                }
            );
            
            RegisterGoalCategory(
                new GoalCategory("Tridots", "Tridots", "START_TRIDOT", "OWN_TRIDOT")
                {
                    getCurrentValueFunc = () => GetCurrentTridots()
                }
            );
            
            RegisterGoalCategory(
                new GoalCategory("Memcard", "Memcards", "START_MEMCARD", "OWN_MEMCARD")
                {
                    getCurrentValueFunc = () => GetCurrentMemcards()
                }
            );
        }
        
        public void RegisterGoalCategory(GoalCategory category)
        {
            if (!goalCategories.ContainsKey(category.categoryName))
            {
                goalCategories.Add(category.categoryName, category);
            }
        }
        
        private void CheckAllGoals()
        {
            foreach (var categoryName in enabledCategories)
            {
                if (goalCategories.TryGetValue(categoryName, out GoalCategory category) && category.isEnabled)
                {
                    CheckGoal(category);
                }
            }
        }
        
        private void CheckGoal(GoalCategory category)
        {
            // Get the current value and goal value
            object currentValue = category.getCurrentValueFunc?.Invoke();
            object goalValue = null;
            
            if (category.goalValues != null && category.currentProgressIndex < category.goalValues.Length)
            {
                goalValue = category.goalValues.GetValue(category.currentProgressIndex);
            }
            
            // Debug log to help diagnose issues
            Debug.Log($"[CheckGoal] {category.displayName}: Current={currentValue}, Goal={goalValue}, ProgressIndex={category.currentProgressIndex}, GoalIndex={category.goalIndex}");
            
            // Only check if the goal is reached if we have valid values
            if (category.IsGoalReached())
            {
                Debug.Log($"[GOAL REACHED] {category.displayName}: Current={currentValue}, Goal={goalValue}");
                
                // Store the current progress index before advancing
                int previousProgressIndex = category.currentProgressIndex;
                
                // Increment progress index
                category.currentProgressIndex++;
                
                // Show message for the goal that was just reached
                ShowOwnMessage(category);
                
                // Advance the goal index
                AdvanceGoal(category);
                
                // Save progress
                SaveProgress();
                
                // Only check the next goal if we're in a new game session
                // This prevents cascading through multiple goals in a single frame
                if (previousProgressIndex == category.currentProgressIndex - 1)
                {
                    // Don't check again until next frame
                    return;
                }
            }
        }
        
        private float GetCurrentDistance()
        {
            if (EconomyManager.Instance != null)
            {
                return EconomyManager.Instance.GetCurrentDistance();
            }
            return GameManager.Instance.CurrentDistance;
        }

        private int GetCurrentTridots()
        {
            if (EconomyManager.Instance != null)
            {
                return EconomyManager.Instance.GetCurrentTridots();
            }
            return GameManager.Instance.gameData.lastRunTridotCollected;
        }

        private int GetCurrentMemcards()
        {
            if (EconomyManager.Instance != null)
            {
                return EconomyManager.Instance.GetCurrentMemcards();
            }
            return GameManager.Instance.gameData.lastRunMemcardsCollected;
        }

        public void InitializeGoals()
        {
            if (goalValuesManager == null)
            {
                goalValuesManager = FindObjectOfType<GoalValuesManager>();
            }

            if (goalValuesManager != null)
            {
                List<string> enabledCategoriesFromManager = goalValuesManager.GetEnabledCategories();
                
                enabledCategories.Clear();
                foreach (var categoryName in enabledCategoriesFromManager)
                {
                    enabledCategories.Add(categoryName);
                }
                
                foreach (var categoryName in enabledCategories)
                {
                    if (goalCategories.TryGetValue(categoryName, out GoalCategory category))
                    {
                        object values = goalValuesManager.GetGoalValues(categoryName);
                        
                        if (values != null)
                        {
                            category.goalValues = (Array)values;
                            category.LoadGoalIndex();
                            category.currentGoalValue = category.GetCurrentGoalValue();
                        }
                    }
                }
            }
        }

        public void AdvanceGoal(GoalCategory category)
        {
            category.AdvanceGoal();
        }

        public void ShowStartGoalMessages()
        {
            StartCoroutine(ShowStartGoalMessagesSequence());
        }

        private IEnumerator ShowStartGoalMessagesSequence()
        {
            if (GameMessageDisplay.Instance != null)
            {
                // Show default message first
                Debug.Log("Showing START_DEFAULT message");
                GameMessageDisplay.Instance.ShowMessageByID("START_DEFAULT");
                
                // Wait before showing category messages
                yield return new WaitForSeconds(betweenStartMessagesDelay);
                
                Debug.Log($"Number of enabled categories: {enabledCategories.Count}");
                foreach (var categoryName in enabledCategories)
                {
                    Debug.Log($"Processing category: {categoryName}");
                    if (goalCategories.TryGetValue(categoryName, out GoalCategory category) && category.isEnabled)
                    {
                        Debug.Log($"Showing start message for category: {categoryName}");
                        category.ShowStartMessage();
                        
                        // Add delay between each category message
                        yield return new WaitForSeconds(betweenStartMessagesDelay);
                    }
                    else
                    {
                        Debug.Log($"Category not found or disabled: {categoryName}");
                    }
                }
            }
        }

        public void ShowOwnMessage(GoalCategory category)
        {
            // Capture the reached goal value using currentProgressIndex - 1
            object reachedGoalValue = null;
            if (category.goalValues != null && category.currentProgressIndex > 0 && category.currentProgressIndex - 1 < category.goalValues.Length)
            {
                reachedGoalValue = category.goalValues.GetValue(category.currentProgressIndex - 1);
            }
            
            // If reachedGoalValue is valid, show the own message with that value
            if (reachedGoalValue != null && !string.IsNullOrEmpty(category.ownMessageID) && GameMessageDisplay.Instance != null)
            {
                GameMessageDisplay.Instance.ShowMessageByID(category.ownMessageID, reachedGoalValue);
            }
            else
            {
                Debug.LogWarning($"[ShowOwnMessage] Could not display message for category {category.categoryName} due to missing data.");
            }
        }

        public void ResetCurrentGoalIndices()
        {
            foreach (var category in goalCategories.Values)
            {
                if (category.isEnabled)
                {
                    category.ResetProgressIndex();
                }
            }
        }

        private void LoadProgress()
        {
            foreach (var category in goalCategories.Values)
            {
                if (category.isEnabled)
                {
                    category.LoadProgressIndex();
                }
            }
        }

        private void SaveProgress()
        {
            foreach (var category in goalCategories.Values)
            {
                if (category.isEnabled)
                {
                    category.SaveProgressIndex();
                }
            }
            
            PlayerPrefs.Save();
        }

        private void OnApplicationQuit()
        {
            SaveProgress();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveProgress();
            }
        }

        public void PrepareForNewRun()
        {
            ResetCurrentGoalIndices();
            ShowStartGoalMessages();
        }
        
        public GoalCategory GetGoalCategory(string categoryName)
        {
            if (goalCategories.TryGetValue(categoryName, out GoalCategory category))
            {
                return category;
            }
            return null;
        }

        public void OnGameStart()
        {
            if (showGoalOnStart)
            {
                Invoke("ShowStartGoalMessages", initialGoalMessageDelay);
            }
        }
    }
}
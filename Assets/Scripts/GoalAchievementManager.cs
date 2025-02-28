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
            
            if (currentValue is float currentFloat && goalValue is float goalFloat)
            {
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
            if (!isEnabled || string.IsNullOrEmpty(startMessageID) || 
                GameMessageDisplay.Instance == null || currentGoalValue == null)
                return;
                
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
            Invoke("ShowStartGoalMessages", startMessageDelay);
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
            if (category.IsGoalReached())
            {
                category.currentProgressIndex++;
                
                ShowOwnMessage(category);
                AdvanceGoal(category);
                SaveProgress();
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
                // Show all messages in a row without waiting between them
                GameMessageDisplay.Instance.ShowMessageByID("START_DEFAULT");
                
                foreach (var categoryName in enabledCategories)
                {
                    if (goalCategories.TryGetValue(categoryName, out GoalCategory category) && category.isEnabled)
                    {
                        category.ShowStartMessage();
                    }
                }
            }
            
            yield return null;
        }

        public void ShowOwnMessage(GoalCategory category)
        {
            StartCoroutine(ShowOwnMessageDelayed(category));
        }

        private IEnumerator ShowOwnMessageDelayed(GoalCategory category)
        {
            yield return new WaitForSeconds(ownMessageDelay);
            category.ShowOwnMessage();
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
    }
}
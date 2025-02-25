using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RoofTops
{
    /// <summary>
    /// Displays achievements and notifications in the game UI
    /// </summary>
    public class AchievementUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] public Transform achievementListContainer;
        [SerializeField] public GameObject achievementItemPrefab;
        [SerializeField] public GameObject achievementNotificationPrefab;
        [SerializeField] public Transform notificationContainer;
        [SerializeField] public Button achievementPanelToggleButton;
        [SerializeField] public GameObject achievementPanel;
        
        [Header("UI Settings")]
        [SerializeField] private float notificationDuration = 3f;
        [SerializeField] private bool showPanelOnStart = false;
        
        // References
        private AchievementSystem achievementSystem;
        
        // State
        private List<GameObject> activeNotifications = new List<GameObject>();
        private Dictionary<string, GameObject> achievementItems = new Dictionary<string, GameObject>();
        
        private void Start()
        {
            // Find the achievement system
            achievementSystem = AchievementSystem.Instance;
            
            if (achievementSystem == null)
            {
                Debug.LogError("AchievementUI: AchievementSystem not found!");
                enabled = false;
                return;
            }
            
            // Subscribe to achievement events
            achievementSystem.onAchievementUnlocked.AddListener(OnAchievementUnlocked);
            achievementSystem.onAchievementProgress.AddListener(OnAchievementProgress);
            
            // Set up toggle button
            if (achievementPanelToggleButton != null)
            {
                achievementPanelToggleButton.onClick.AddListener(ToggleAchievementPanel);
            }
            
            // Initialize UI
            if (achievementPanel != null)
            {
                achievementPanel.SetActive(showPanelOnStart);
            }
            
            // Populate achievement list
            PopulateAchievementList();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (achievementSystem != null)
            {
                achievementSystem.onAchievementUnlocked.RemoveListener(OnAchievementUnlocked);
                achievementSystem.onAchievementProgress.RemoveListener(OnAchievementProgress);
            }
        }
        
        private void PopulateAchievementList()
        {
            if (achievementListContainer == null || achievementItemPrefab == null)
                return;
                
            // Clear existing items
            foreach (Transform child in achievementListContainer)
            {
                Destroy(child.gameObject);
            }
            achievementItems.Clear();
            
            // Add active achievements
            foreach (var achievement in achievementSystem.GetActiveAchievements())
            {
                AddAchievementToList(achievement);
            }
            
            // Add completed achievements
            foreach (var achievement in achievementSystem.GetCompletedAchievements())
            {
                AddAchievementToList(achievement);
            }
        }
        
        private void AddAchievementToList(Achievement achievement)
        {
            // Don't add hidden achievements that aren't completed
            if (achievement.Type == AchievementType.Hidden && !achievement.IsCompleted)
                return;
                
            GameObject item = Instantiate(achievementItemPrefab, achievementListContainer);
            
            // Find components in the prefab
            TextMeshProUGUI titleText = item.GetComponentInChildren<TextMeshProUGUI>();
            TextMeshProUGUI descriptionText = item.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
            Image progressBar = item.transform.Find("ProgressBar")?.GetComponent<Image>();
            Image completedIcon = item.transform.Find("CompletedIcon")?.GetComponent<Image>();
            
            // Set achievement data
            if (titleText != null)
                titleText.text = achievement.Title;
                
            if (descriptionText != null)
                descriptionText.text = achievement.Description;
                
            if (progressBar != null)
                progressBar.fillAmount = achievement.GetProgress();
                
            if (completedIcon != null)
                completedIcon.gameObject.SetActive(achievement.IsCompleted);
                
            // Add to our tracking dictionary
            achievementItems[achievement.Id] = item;
        }
        
        private void OnAchievementUnlocked(Achievement achievement)
        {
            // Show notification
            ShowAchievementNotification(achievement);
            
            // Update UI list
            if (achievementItems.TryGetValue(achievement.Id, out GameObject item))
            {
                // Update existing item
                Image progressBar = item.transform.Find("ProgressBar")?.GetComponent<Image>();
                Image completedIcon = item.transform.Find("CompletedIcon")?.GetComponent<Image>();
                
                if (progressBar != null)
                    progressBar.fillAmount = 1f;
                    
                if (completedIcon != null)
                    completedIcon.gameObject.SetActive(true);
            }
            else
            {
                // Create a new item
                AddAchievementToList(achievement);
            }
        }
        
        private void OnAchievementProgress(Achievement achievement, float progress)
        {
            // Update UI for this achievement
            if (achievementItems.TryGetValue(achievement.Id, out GameObject item))
            {
                Image progressBar = item.transform.Find("ProgressBar")?.GetComponent<Image>();
                
                if (progressBar != null)
                    progressBar.fillAmount = progress;
            }
        }
        
        private void ShowAchievementNotification(Achievement achievement)
        {
            if (notificationContainer == null || achievementNotificationPrefab == null)
                return;
                
            // Create notification
            GameObject notification = Instantiate(achievementNotificationPrefab, notificationContainer);
            
            // Set notification data
            TextMeshProUGUI titleText = notification.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descriptionText = notification.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
            
            if (titleText != null)
                titleText.text = achievement.Title;
                
            if (descriptionText != null)
                descriptionText.text = achievement.Description;
                
            // Track active notification
            activeNotifications.Add(notification);
            
            // Schedule removal
            StartCoroutine(RemoveNotificationAfterDelay(notification));
        }
        
        private IEnumerator RemoveNotificationAfterDelay(GameObject notification)
        {
            yield return new WaitForSeconds(notificationDuration);
            
            // Animate out (optional)
            CanvasGroup canvasGroup = notification.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                float startTime = Time.time;
                float duration = 0.5f;
                
                while (Time.time < startTime + duration)
                {
                    float t = (Time.time - startTime) / duration;
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                    yield return null;
                }
            }
            
            // Remove from tracking and destroy
            activeNotifications.Remove(notification);
            Destroy(notification);
        }
        
        private void ToggleAchievementPanel()
        {
            if (achievementPanel != null)
            {
                achievementPanel.SetActive(!achievementPanel.activeSelf);
                
                // Refresh the list if we're opening the panel
                if (achievementPanel.activeSelf)
                {
                    PopulateAchievementList();
                }
            }
        }
        
        // Public methods for external calls
        
        public void ShowAchievementPanel()
        {
            if (achievementPanel != null)
            {
                achievementPanel.SetActive(true);
                PopulateAchievementList();
            }
        }
        
        public void HideAchievementPanel()
        {
            if (achievementPanel != null)
            {
                achievementPanel.SetActive(false);
            }
        }
    }
} 
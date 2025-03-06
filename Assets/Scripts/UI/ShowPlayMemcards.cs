using UnityEngine;
using TMPro;

namespace RoofTops.UI
{
    /// <summary>
    /// Handles displaying the current memcards count in the UI
    /// </summary>
    public class ShowPlayMemcards : MonoBehaviour
    {
        [Tooltip("Text element to display the memcards count")]
        public TMP_Text memcardsText;
        
        private int currentMemcards = 0;
        
        void Start()
        {
            // Initialize with current value
            UpdateMemcardsCount();
        }
        
        void Update()
        {
            UpdateMemcardsCount();
        }
        
        private void UpdateMemcardsCount()
        {
            if (memcardsText == null) return;
            
            // Only update from EconomyManager if available
            if (EconomyManager.Instance != null)
            {
                currentMemcards = EconomyManager.Instance.GetCurrentMemcards();
            }
            // We're not using GameManager fallback anymore since we don't know the property name
            // If EconomyManager isn't available, we'll use the cached value
            
            // Update UI text
            memcardsText.text = currentMemcards.ToString();
        }
    }
} 
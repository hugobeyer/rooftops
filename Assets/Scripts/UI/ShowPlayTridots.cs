using UnityEngine;
using TMPro;

namespace RoofTops.UI
{
    /// <summary>
    /// Handles displaying the current tridots count in the UI
    /// </summary>
    public class ShowPlayTridots : MonoBehaviour
    {
        [Tooltip("Text element to display the tridots count")]
        public TMP_Text tridotsText;
        
        private int currentTridots = 0;
        
        void Start()
        {
            // Initialize with current value
            UpdateTridotCount();
        }
        
        void Update()
        {
            UpdateTridotCount();
        }
        
        private void UpdateTridotCount()
        {
            if (tridotsText == null) return;
            
            // Only update from EconomyManager if available
            if (EconomyManager.Instance != null)
            {
                currentTridots = EconomyManager.Instance.GetCurrentTridots();
            }
            // We're not using GameManager fallback anymore since we don't know the property name
            // If EconomyManager isn't available, we'll use the cached value
            
            // Update UI text
            tridotsText.text = currentTridots.ToString();
        }
    }
} 
using UnityEngine;
using TMPro;

namespace RoofTops.UI
{
    /// <summary>
    /// Handles displaying the current speed rate in the UI
    /// </summary>
    public class ShowPlaySpeed : MonoBehaviour
    {
        [Tooltip("Text element to display the speed rate")]
        public TMP_Text speedText;
        
        [Tooltip("Format string for speed (default: '{0:F2}x')")]
        public string formatString = "{0:F2}x";
        
        private float currentSpeed = 1.0f;
        
        void Start()
        {
            // Initialize with current value
            UpdateSpeedValue();
        }
        
        void Update()
        {
            UpdateSpeedValue();
        }
        
        private void UpdateSpeedValue()
        {
            if (speedText == null) return;
            
            // Get current speed from Module Pool (primary source)
            if (ModulePool.Instance != null)
            {
                currentSpeed = ModulePool.Instance.currentMoveSpeed;
            }
            // Not using GameManager fallback since we don't know the property name
            // If ModulePool isn't available, we'll use the cached value
            
            // Update UI text
            speedText.text = string.Format(formatString, currentSpeed);
        }
    }
} 
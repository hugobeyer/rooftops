using UnityEngine;
using TMPro;

namespace RoofTops.UI
{
    /// <summary>
    /// Handles displaying the current distance in the UI
    /// </summary>
    public class ShowPlayDistance : MonoBehaviour
    {
        [Tooltip("Text element to display the distance")]
        public TMP_Text distanceText;
        
        void Update()
        {
            if (distanceText == null) return;
            
            // Just use GameManager directly - simplest approach
            if (GameManager.Instance != null)
            {
                distanceText.text = $"{GameManager.Instance.CurrentDistance:F1}";
            }
        }
    }
} 
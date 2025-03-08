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
        
        void Update()
        {
            if (memcardsText == null) return;
            
            // PRIORITY ORDER FOR GETTING MEMCARD COUNT:
            // 1. GameManager.gameData.lastRunMemcardsCollected (persistent data)
            // 2. EconomyManager.GetCurrentMemcards() (transient data)
            
            if (GameManager.Instance != null && GameManager.Instance.gameData != null)
            {
                memcardsText.text = GameManager.Instance.gameData.lastRunMemcardsCollected.ToString();
            }
            else if (EconomyManager.Instance != null)
            {
                memcardsText.text = EconomyManager.Instance.GetCurrentMemcards().ToString();
            }
        }
    }
} 
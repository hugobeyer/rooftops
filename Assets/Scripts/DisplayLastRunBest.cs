using UnityEngine;
using TMPro;

namespace RoofTops
{
    public class DisplayLastRunBest : MonoBehaviour
    {
        public TMP_Text bestText;

        void Start()
        {
            if (bestText == null)
            {
                bestText = GetComponent<TMP_Text>();
            }

            float bestDistance = GameManager.Instance.gameData.bestDistance;
            bestText.text = $"{bestDistance:F1} m";
        }
        
    } 
} 
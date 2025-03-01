using UnityEngine;
using TMPro;

namespace RoofTops
{
    public class DisplayLastRunDistance : MonoBehaviour
    {
        public TMP_Text distanceText;
        void Start()
        {
            if (distanceText == null)
            {
                distanceText = GetComponent<TMP_Text>();
            }

            float lastRun = GameManager.Instance.gameData.lastRunDistance;
            distanceText.text = $"{lastRun:F1} m";
        }
    }
}
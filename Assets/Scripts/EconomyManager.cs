using UnityEngine;

namespace RoofTops
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        // Private fields to track resources
        private float currentDistance = 0f;
        private int currentTridots = 0;
        private int currentMemcards = 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Update distance value
        public void UpdateDistance(float distance)
        {
            currentDistance = distance;
        }

        public void AddTridots(int amount)
        {
            // Update our own tracking
            currentTridots += amount;
            
            // Track metrics
            IncrementMetric("tridot_collected_total", amount);

            bool isUsedForDash = amount < 0;
            if (!isUsedForDash)
            {
                IncrementMetric("tridot_collected", amount);
                IncrementMetric("last_run_tridot", amount);
                UpdateBestMetric("last_run_tridot", currentTridots);
            }
        }

        public void AddMemcards(int amount)
        {
            // Update our own tracking
            currentMemcards += amount;
            
            // Update metrics
            IncrementMetric("memcard_collected_total", amount);

            // If adding (not consuming), update additional metric
            if (amount > 0)
            {
                IncrementMetric("memcard_collected", amount);
            }
        }

        private void IncrementMetric(string metricName, float value)
        {
            Debug.Log($"Incrementing {metricName} by {value}");
            // TODO: Add your metric updating logic here
        }

        private void UpdateBestMetric(string metricName, float currentValue)
        {
            Debug.Log($"Updating best {metricName} with value {currentValue}");
            // TODO: Add your best metric updating logic here
        }

        // Methods to get current run values for goal tracking
        public float GetCurrentDistance()
        {
            return currentDistance;
        }
        
        public int GetCurrentTridots()
        {
            return currentTridots;
        }
        
        public int GetCurrentMemcards()
        {
            return currentMemcards;
        }
        
        // Reset current run values
        public void ResetCurrentRunValues()
        {
            currentDistance = 0f;
            currentTridots = 0;
            currentMemcards = 0;
        }
    }
} 
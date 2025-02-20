using UnityEngine;

namespace RoofTops
{
    public class DistanceTracker : MonoBehaviour
    {
        private float distanceTraveled;
        private Vector3 startPosition;

        void Start()
        {
            // Record the environment's initial position
            startPosition = transform.position;
        }

        void Update()
        {
            // Because ModulePool translates this object backward (negative Z),
            // we measure how far it has moved from its start position.
            distanceTraveled = Vector3.Distance(startPosition, transform.position);
        }

        // Call this when the "run" ends (e.g., upon player death)
        public void SaveDistance()
        {
            // Save distance to GameData (last run distance)
            GameManager.Instance.gameData.lastRunDistance = distanceTraveled;

            // Optionally update best distance
            if (distanceTraveled > GameManager.Instance.gameData.bestDistance)
            {
                GameManager.Instance.gameData.bestDistance = distanceTraveled;
            }
        }
    }
} 
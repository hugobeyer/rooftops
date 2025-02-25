using UnityEngine;

namespace RoofTops
{
    public class MemcardCollectible : MonoBehaviour
    {
        [Header("Collection Settings")]
        [Tooltip("Tag of the object that can collect this memcard (usually the player)")]
        public string collectorTag = "Player";

        [Tooltip("Points awarded when collected")]
        public int pointValue = 1;

        [Header("Effects")]
        [Tooltip("Audio clip to play when collected")]
        public AudioClip collectionSound;

        [Tooltip("Volume of the collection sound")]
        [Range(0f, 1f)]
        public float soundVolume = 0.7f;

        [Tooltip("Visual effect prefab to spawn when collected")]
        public GameObject collectionEffectPrefab;

        [Tooltip("How long the effect should last before being destroyed")]
        public float effectDuration = 1.0f;

        // Reference to game data
        private GameDataObject gameData;
        // Cache the game manager
        private GameManager gameManager;
        // Reference to text display
        private MemcardDisplay memcardDisplay;

        private void Start()
        {
            // Find the GameManager in the scene
            gameManager = FindObjectOfType<GameManager>();
            
            // Find the MemcardDisplay
            memcardDisplay = FindObjectOfType<MemcardDisplay>();
            
            // Try to get game data from GameManager first
            if (gameManager != null && gameManager.gameData != null)
            {
                gameData = gameManager.gameData;
            }
            else
            {
                // Fallback to loading from Resources
                gameData = Resources.Load<GameDataObject>("GameData");
                
                if (gameData == null)
                {
                    Debug.LogWarning("GameData not found. Memcard collection won't be tracked in game data.");
                }
            }
            
            // Ensure the collider is a trigger
            Collider collider = GetComponent<Collider>();
            if (collider != null && !collider.isTrigger)
            {
                collider.isTrigger = true;
                Debug.Log("Setting collider to trigger mode for memcard: " + gameObject.name);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if the object that entered the trigger has the collector tag
            if (other.CompareTag(collectorTag))
            {
                CollectMemcard();
            }
        }

        private void CollectMemcard()
        {
            // Notify GameManager first (if available)
            if (gameManager != null)
            {
                gameManager.OnMemcardCollected(pointValue);
            }
            else
            {
                // Direct update to gameData if no GameManager
                UpdateGameData();
                
                // Also update the display if we have one
                if (memcardDisplay != null)
                {
                    memcardDisplay.IncrementCount(pointValue);
                }
            }
            
            // Play sound effect
            PlayCollectionSound();
            
            // Spawn visual effect
            SpawnCollectionEffect();
            
            // Destroy the memcard
            Destroy(gameObject);
        }

        private void UpdateGameData()
        {
            if (gameData != null)
            {
                // Increment the total collected count
                gameData.totalBonusCollected += pointValue;
                
                // Increment the last run collected count
                gameData.lastRunBonusCollected += pointValue;
                
                // Update best run collected if current run is better
                if (gameData.lastRunBonusCollected > gameData.bestRunBonusCollected)
                {
                    gameData.bestRunBonusCollected = gameData.lastRunBonusCollected;
                }
            }
        }

        private void PlayCollectionSound()
        {
            // Play sound directly
            if (collectionSound != null)
            {
                // Play sound at position without creating a permanent AudioSource
                AudioSource.PlayClipAtPoint(collectionSound, transform.position, soundVolume);
            }
        }

        private void SpawnCollectionEffect()
        {
            if (collectionEffectPrefab != null)
            {
                // Instantiate the effect at the memcard's position
                GameObject effect = Instantiate(collectionEffectPrefab, transform.position, Quaternion.identity);
                
                // Destroy the effect after the specified duration
                Destroy(effect, effectDuration);
            }
        }
    }
} 
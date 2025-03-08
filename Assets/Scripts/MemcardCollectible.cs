using UnityEngine;
using System.Reflection;

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
        [Tooltip("Audio clips to play when collected (will cycle through them in sequence)")]
        public AudioClip[] collectionSounds;

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

        // Static track of which sound in the sequence to play next (shared across all memcards)
        private static int currentSoundIndex = 0;

        private void Start()
        {
            // Find the GameManager in the scene
            gameManager = FindFirstObjectByType<GameManager>();
            
            // Find the MemcardDisplay
            memcardDisplay = FindFirstObjectByType<MemcardDisplay>();
            
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
            // First update EconomyManager (primary system)
            if (EconomyManager.Instance != null) 
            {
                EconomyManager.Instance.AddMemcards(pointValue);
                Debug.Log($"EconomyManager: Added {pointValue} memcards. New total: {EconomyManager.Instance.GetCurrentMemcards()}");
            } 
            else
            {
                Debug.LogWarning("EconomyManager instance not found in MemcardCollectible.CollectMemcard.");
            }

            // Then update GameDataObject (persistence system)
            // Make sure we have the latest GameManager reference
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
            }
            
            if (gameManager != null && gameManager.gameData != null)
            {
                // Update GameDataObject directly
                gameManager.gameData.totalMemcardsCollected += pointValue;
                gameManager.gameData.lastRunMemcardsCollected += pointValue;
                
                if (gameManager.gameData.lastRunMemcardsCollected > gameManager.gameData.bestRunMemcardsCollected)
                {
                    gameManager.gameData.bestRunMemcardsCollected = gameManager.gameData.lastRunMemcardsCollected;
                }
                
                Debug.Log($"GameData: Updated memcards. Last run total: {gameManager.gameData.lastRunMemcardsCollected}");
                
                // Try to save game data
                try
                {
                    // Use reflection to check if the method exists and call it
                    MethodInfo saveMethod = gameManager.GetType().GetMethod("SaveGameData", 
                        BindingFlags.Public | BindingFlags.Instance);
                    
                    if (saveMethod != null)
                    {
                        saveMethod.Invoke(gameManager, null);
                        Debug.Log("GameData saved successfully");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("Could not save game data: " + ex.Message);
                }
            }
            
            // Update UI if available
            if (memcardDisplay != null)
            {
                memcardDisplay.IncrementCount(pointValue);
            }
            
            // Play sound effect
            PlayCollectionSound();
            
            // Spawn visual effect
            SpawnCollectionEffect();
            
            // Destroy the memcard
            Destroy(gameObject);
        }

        private void PlayCollectionSound()
        {
            // Play sound directly
            if (collectionSounds != null && collectionSounds.Length > 0)
            {
                // Create temporary audio source
                GameObject tempAudio = new GameObject("TempAudio");
                AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
                
                // Get the current clip in the sequence
                AudioClip currentClip = collectionSounds[currentSoundIndex];
                
                // Increment index for next collection and loop back to 0 after reaching max
                currentSoundIndex = (currentSoundIndex + 1) % collectionSounds.Length;
                
                // Set properties
                audioSource.clip = currentClip;
                audioSource.volume = soundVolume;
                audioSource.spatialBlend = 0.3f; // 30% 3D sound
                
                // Play sound
                audioSource.Play();
                
                // Destroy after sound is done
                Destroy(tempAudio, currentClip.length);
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
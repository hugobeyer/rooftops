using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using RoofTops;

/// <summary>
/// Controls the delayed activation of GameObjects based on different game states.
/// Attach this to any GameObject that needs delayed activation.
/// </summary>
public class DelayedActivation : MonoBehaviour
{
   [Header("Target Settings")]
   [Tooltip("The GameObjects to control. If empty, this script will control its own GameObject.")]
   public List<GameObject> targetObjects = new List<GameObject>();

   [Header("Activation Settings")]
   [Tooltip("Should these objects be active at the start of the scene?")]
   public bool activeOnStart = false;

   [Header("Title Screen Settings")]
   [Tooltip("Should these objects activate during the title screen?")]
   public bool activateOnTitleScreen = false;
   [Tooltip("Delay in seconds before activating on title screen")]
   public float titleScreenDelay = 1.0f;

   [Header("Game Start Settings")]
   [Tooltip("Should these objects activate when the game starts?")]
   public bool activateOnGameStart = false;
   [Tooltip("Delay in seconds after game start before activating")]
   public float gameStartDelay = 0.5f;

   [Header("Death Settings")]
   [Tooltip("Should these objects activate when the player dies?")]
   public bool activateOnDeath = false;
   [Tooltip("Delay in seconds after death before activating")]
   public float deathDelay = 0.3f;
   [Tooltip("Should these objects deactivate when the player dies?")]
   public bool deactivateOnDeath = false;
   [Tooltip("Delay in seconds after death before deactivating")]
   public float deathDeactivationDelay = 0.1f;

   [Header("Deactivation Settings")]
   [Tooltip("Should these objects automatically deactivate after being shown?")]
   public bool autoDeactivate = false;
   [Tooltip("How long the objects stay active before deactivating (in seconds)")]
   public float activeTime = 3.0f;

   [Header("Timeline Signal Settings")]
   [Tooltip("Should these objects respond to Timeline signals?")]
   public bool useTimelineSignals = false;
   [Tooltip("Signal name that will activate these objects")]
   public string activateSignalName = "Activate";
   [Tooltip("Signal name that will deactivate these objects")]
   public string deactivateSignalName = "Deactivate";

   [Header("Events")]
   [Tooltip("Event triggered when objects are activated")]
   public UnityEvent onActivated;
   [Tooltip("Event triggered when objects are deactivated")]
   public UnityEvent onDeactivated;

   private bool hasActivatedOnTitleScreen = false;
   private bool hasActivatedOnGameStart = false;
   private bool hasActivatedOnDeath = false;
   private bool hasDeactivatedOnDeath = false;
   private Coroutine deathDeactivationCoroutine = null;

   private void Awake()
   {
      // If no targets are specified, use this GameObject
      if (targetObjects.Count == 0)
      {
         targetObjects.Add(gameObject);
      }

      // Remove any null entries
      targetObjects.RemoveAll(item => item == null);

      // Set initial state
      SetTargetsActive(activeOnStart);
   }

   private void Start()
   {
      // Subscribe to game events
      if (GameManager.Instance != null)
      {
         // Listen for game start
         if (activateOnGameStart)
         {
            GameManager.Instance.onGameStarted.AddListener(OnGameStarted);
         }

         // Start title screen activation if needed
         if (activateOnTitleScreen && !hasActivatedOnTitleScreen && !GameManager.Instance.HasGameStarted)
         {
            StartCoroutine(ActivateAfterDelay(titleScreenDelay));
            hasActivatedOnTitleScreen = true;
         }
      }
      else
      {
         Debug.LogWarning("DelayedActivation: GameManager.Instance is null. Event-based activation won't work.");
      }

      // Find and subscribe to player death events if needed
      if (activateOnDeath || deactivateOnDeath)
      {
         PlayerController player = FindFirstObjectByType<PlayerController>();
         if (player != null)
         {
            // Use a custom approach to detect death since PlayerController doesn't have a direct death event
            StartCoroutine(CheckForPlayerDeath(player));
         }
      }
   }

   private void OnGameStarted()
   {
      if (!hasActivatedOnGameStart)
      {
         StartCoroutine(ActivateAfterDelay(gameStartDelay));
         hasActivatedOnGameStart = true;
      }
   }

   private IEnumerator CheckForPlayerDeath(PlayerController player)
   {
      bool wasAlive = !player.IsDead();

      while (true)
      {
         // Check if player just died (was alive but now is dead)
         if (wasAlive && player.IsDead())
         {
            // Handle activation on death
            if (activateOnDeath && !hasActivatedOnDeath)
            {
               StartCoroutine(ActivateAfterDelay(deathDelay));
               hasActivatedOnDeath = true;
            }

            // Handle deactivation on death
            if (deactivateOnDeath && !hasDeactivatedOnDeath)
            {
               // Cancel any existing deactivation coroutine
               if (deathDeactivationCoroutine != null)
               {
                  StopCoroutine(deathDeactivationCoroutine);
               }

               // Start new deactivation coroutine
               deathDeactivationCoroutine = StartCoroutine(DeactivateAfterDelay(deathDeactivationDelay));
               hasDeactivatedOnDeath = true;
            }
         }

         // Update previous state
         wasAlive = !player.IsDead();

         // Wait before checking again
         yield return new WaitForSeconds(0.1f);
      }
   }

   private IEnumerator ActivateAfterDelay(float delay)
   {
      // Wait for the specified delay
      yield return new WaitForSeconds(delay);

      // Activate the GameObjects
      SetTargetsActive(true);

      // Handle auto-deactivation if needed
      if (autoDeactivate)
      {
         yield return new WaitForSeconds(activeTime);
         SetTargetsActive(false);
      }
   }

   private IEnumerator DeactivateAfterDelay(float delay)
   {
      // Wait for the specified delay
      yield return new WaitForSeconds(delay);

      // Deactivate the GameObjects
      SetTargetsActive(false);

      // Clear the coroutine reference
      deathDeactivationCoroutine = null;
   }

   private void OnDestroy()
   {
      // Unsubscribe from events to prevent memory leaks
      if (GameManager.Instance != null && activateOnGameStart)
      {
         GameManager.Instance.onGameStarted.RemoveListener(OnGameStarted);
      }
   }

   // Helper method to set all targets active or inactive
   private void SetTargetsActive(bool active)
   {
      foreach (GameObject target in targetObjects)
      {
         if (target != null)
         {
            target.SetActive(active);
         }
      }

      // Trigger appropriate event
      if (active)
      {
         onActivated?.Invoke();
      }
      else
      {
         onDeactivated?.Invoke();
      }
   }

   // Public method to manually trigger activation with the configured delay
   public void TriggerActivation()
   {
      StartCoroutine(ActivateAfterDelay(gameStartDelay));
   }

   // Public method to manually trigger activation with a custom delay
   public void TriggerActivation(float customDelay)
   {
      StartCoroutine(ActivateAfterDelay(customDelay));
   }

   // Public method to manually deactivate
   public void Deactivate()
   {
      SetTargetsActive(false);
   }

   // Public method to manually deactivate with delay
   public void Deactivate(float delay)
   {
      StartCoroutine(DeactivateAfterDelay(delay));
   }

   // Reset activation flags (useful for game restart)
   public void ResetActivationState()
   {
      hasActivatedOnTitleScreen = false;
      hasActivatedOnGameStart = false;
      hasActivatedOnDeath = false;
      hasDeactivatedOnDeath = false;

      // Cancel any pending deactivation
      if (deathDeactivationCoroutine != null)
      {
         StopCoroutine(deathDeactivationCoroutine);
         deathDeactivationCoroutine = null;
      }

      // Reset to initial state
      SetTargetsActive(activeOnStart);
   }

   // Timeline signal receivers
   public void OnSignalReceived(string signalName)
   {
      if (!useTimelineSignals) return;

      if (signalName == activateSignalName)
      {
         SetTargetsActive(true);
      }
      else if (signalName == deactivateSignalName)
      {
         SetTargetsActive(false);
      }
   }

   // Direct Timeline signal receivers (for direct binding in Timeline)
   public void ActivateFromSignal()
   {
      if (useTimelineSignals)
      {
         SetTargetsActive(true);
      }
   }

   public void DeactivateFromSignal()
   {
      if (useTimelineSignals)
      {
         SetTargetsActive(false);
      }
   }
}
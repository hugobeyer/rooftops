using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using RoofTops;

/// <summary>
/// Controls the delayed activation of GameObjects based on different game states.
/// </summary>
public class DelayedActivation : MonoBehaviour
{
   [System.Serializable]
   public class GameStateItem
   {
      [Tooltip("Target")]
      public GameObject target;
      
      [Tooltip("Start active")]
      public bool startActive = false;
      
      [Tooltip("Activate (delay)")]
      public GameStates activateState;
      [Tooltip("Delay")]
      public float activateDelay = 0f;
      
      [Tooltip("Deactivate (delay)")]
      public GameStates deactivateState;
      [Tooltip("Delay")]
      public float deactivateDelay = 0f;
      
      [Tooltip("Destroy")]
      public bool destroy = false;

      [Tooltip("Affect children")]
      public bool affectChildren = false;
   }

   [Tooltip("Items")]
   public GameStateItem[] items = new GameStateItem[0];

   // State tracking
   private Dictionary<GameStateItem, bool> activeStatus = new Dictionary<GameStateItem, bool>();
   private Dictionary<GameStateItem, Coroutine> activateRoutines = new Dictionary<GameStateItem, Coroutine>();
   private Dictionary<GameStateItem, Coroutine> deactivateRoutines = new Dictionary<GameStateItem, Coroutine>();

   private void Awake()
   {
      // Set initial states
      foreach (var item in items)
      {
         if (item.target != null)
         {
            item.target.SetActive(item.startActive);
         }
      }
   }

   private void Start()
   {
      // Init tracking
      foreach (var item in items)
      {
         if (item.target != null)
         {
            activeStatus[item] = item.startActive;
         }
      }
      
      // Subscribe to state changes
      GameManager.OnGameStateChanged += OnGameStateChanged;
      
      // Log current configuration
      Debug.Log($"DelayedActivation on {gameObject.name} initialized with current game state: {GameManager.GamesState}");
      
      // Check current state
      CheckCurrentState();
   }
   
   private void CheckCurrentState()
   {
      GameStates state = GameManager.GamesState;
      
      foreach (var item in items)
      {
         if (item.target != null)
         {
            // Check activation
            if (item.activateState == state)
            {
               ActivateItem(item);
            }
            
            // Check deactivation
            if (item.deactivateState == state)
            {
               DeactivateItem(item);
            }
         }
      }
   }
   
   private void OnGameStateChanged(GameStates oldState, GameStates newState)
   {
      Debug.Log($"DelayedActivation: Game state changed from {oldState} to {newState}");
      
      foreach (var item in items)
      {
         if (item.target != null)
         {
            // Check activation
            if (item.activateState == newState)
            {
               Debug.Log($"DelayedActivation: Activating {item.target.name} for state {newState} with delay {item.activateDelay}s");
               ActivateItem(item);
            }
            
            // Check deactivation
            if (item.deactivateState == newState)
            {
               Debug.Log($"DelayedActivation: Deactivating {item.target.name} for state {newState} with delay {item.deactivateDelay}s");
               DeactivateItem(item);
            }
         }
      }
   }
   
   private void ActivateItem(GameStateItem item)
   {
      // Cancel existing routine
      if (activateRoutines.ContainsKey(item) && activateRoutines[item] != null)
      {
         StopCoroutine(activateRoutines[item]);
      }
      
      // Start new routine
      activateRoutines[item] = StartCoroutine(DoActivate(item));
   }
   
   private void DeactivateItem(GameStateItem item)
   {
      // Cancel existing routine
      if (deactivateRoutines.ContainsKey(item) && deactivateRoutines[item] != null)
      {
         StopCoroutine(deactivateRoutines[item]);
      }
      
      // Start new routine
      deactivateRoutines[item] = StartCoroutine(DoDeactivate(item));
   }
   
   private IEnumerator DoActivate(GameStateItem item)
   {
      yield return new WaitForSeconds(item.activateDelay);
      
      if (item.target != null)
      {
         // Activate the target without affecting children
         if (!item.affectChildren && !item.target.activeSelf)
         {
            // Store child active states
            Dictionary<GameObject, bool> childrenStates = new Dictionary<GameObject, bool>();
            foreach (Transform child in item.target.transform)
            {
               childrenStates[child.gameObject] = child.gameObject.activeSelf;
            }
            
            // Activate parent
            item.target.SetActive(true);
            
            // Restore child states
            foreach (Transform child in item.target.transform)
            {
               if (childrenStates.ContainsKey(child.gameObject))
               {
                  child.gameObject.SetActive(childrenStates[child.gameObject]);
               }
            }
         }
         else
         {
            // Standard activation (affects children)
            item.target.SetActive(true);
         }
         
         activeStatus[item] = true;
      }
   }
   
   private IEnumerator DoDeactivate(GameStateItem item)
   {
      yield return new WaitForSeconds(item.deactivateDelay);
      
      if (item.target != null)
      {
         if (item.destroy)
         {
            Destroy(item.target);
         }
         else
         {
            // Deactivate without affecting children
            if (!item.affectChildren && item.target.activeSelf)
            {
               // Store child active states
               Dictionary<GameObject, bool> childrenStates = new Dictionary<GameObject, bool>();
               foreach (Transform child in item.target.transform)
               {
                  childrenStates[child.gameObject] = child.gameObject.activeSelf;
               }
               
               // Deactivate parent
               item.target.SetActive(false);
               
               // Activate children that were active before
               foreach (var pair in childrenStates)
               {
                  if (pair.Value)
                  {
                     // Re-parent to maintain hierarchy
                     Transform originalParent = pair.Key.transform.parent;
                     pair.Key.transform.SetParent(null);
                     pair.Key.SetActive(true);
                     pair.Key.transform.SetParent(originalParent);
                  }
               }
            }
            else
            {
               // Standard deactivation (affects children)
               item.target.SetActive(false);
            }
         }
         
         activeStatus[item] = false;
      }
   }

   private void OnDestroy()
   {
      // Stop all routines
      StopAllCoroutines();
      
      // Unsubscribe
      GameManager.OnGameStateChanged -= OnGameStateChanged;
   }
   
   public void Reset()
   {
      // Stop all routines
      StopAllCoroutines();
      
      // Clear tracking
      activeStatus.Clear();
      activateRoutines.Clear();
      deactivateRoutines.Clear();
      
      // Reset to initial states
      foreach (var item in items)
      {
         if (item.target != null)
         {
            item.target.SetActive(item.startActive);
            activeStatus[item] = item.startActive;
         }
      }
   }

   // Add a public method to check the current configuration
   public void LogConfiguration()
   {
      Debug.Log($"DelayedActivation Configuration on {gameObject.name}:");
      
      foreach (var item in items)
      {
         if (item.target != null)
         {
            Debug.Log($"  Item: {item.target.name}");
            Debug.Log($"    Start Active: {item.startActive}");
            Debug.Log($"    Activate on State: {item.activateState} with delay {item.activateDelay}s");
            Debug.Log($"    Deactivate on State: {item.deactivateState} with delay {item.deactivateDelay}s");
            Debug.Log($"    Destroy on Deactivate: {item.destroy}");
            Debug.Log($"    Affect Children: {item.affectChildren}");
            Debug.Log($"    Current Status: {(activeStatus.ContainsKey(item) ? activeStatus[item] : "Unknown")}");
         }
         else
         {
            Debug.Log("  Item: NULL TARGET");
         }
      }
   }
}
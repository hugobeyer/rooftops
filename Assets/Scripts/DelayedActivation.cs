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
   }

   [System.Serializable]
   public class InputDelaySettings
   {
      [Tooltip("Enable input delay for this state")]
      public bool enableInputDelay = false;
      
      [Tooltip("Game state to control input for")]
      public GameStates gameState;
      
      [Tooltip("Delay before enabling input (seconds)")]
      public float inputEnableDelay = 0f;
      
      [Tooltip("Delay before disabling input (seconds)")]
      public float disableInputDelay = 0f;
      
      [Tooltip("Should input be enabled or disabled for this state")]
      public bool enableInput = true;
   }

   [Tooltip("Items")]
   public GameStateItem[] items = new GameStateItem[0];
   
   [Header("Input Delay Settings")]
   [Tooltip("Control input activation/deactivation per game state")]
   public InputDelaySettings[] inputDelaySettings = new InputDelaySettings[0];

   // State tracking
   private Dictionary<GameStateItem, bool> activeStatus = new Dictionary<GameStateItem, bool>();
   private Dictionary<GameStateItem, Coroutine> activateRoutines = new Dictionary<GameStateItem, Coroutine>();
   private Dictionary<GameStateItem, Coroutine> deactivateRoutines = new Dictionary<GameStateItem, Coroutine>();
   
   // Input delay tracking
   private Dictionary<InputDelaySettings, Coroutine> inputRoutines = new Dictionary<InputDelaySettings, Coroutine>();

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
      
      // Check input delay settings for current state
      CheckInputDelayForState(state);
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
      
      // Handle input delay for the new state
      CheckInputDelayForState(newState);
   }
   
   private void CheckInputDelayForState(GameStates state)
   {
      foreach (var setting in inputDelaySettings)
      {
         if (setting.enableInputDelay && setting.gameState == state)
         {
            // Cancel any existing routines for this setting
            if (inputRoutines.ContainsKey(setting) && inputRoutines[setting] != null)
            {
               StopCoroutine(inputRoutines[setting]);
            }
            
            // Start new routine
            if (setting.enableInput)
            {
               inputRoutines[setting] = StartCoroutine(EnableInputAfterDelay(setting));
            }
            else
            {
               inputRoutines[setting] = StartCoroutine(DisableInputAfterDelay(setting));
            }
         }
      }
   }
   
   private IEnumerator EnableInputAfterDelay(InputDelaySettings setting)
   {
      // Disable input immediately if we're going to delay enabling it
      if (setting.inputEnableDelay > 0 && InputActionManager.Exists())
      {
         InputActionManager.Instance.InputActionsDeactivate();
      }
      
      yield return new WaitForSeconds(setting.inputEnableDelay);
      
      // Enable input after delay
      if (InputActionManager.Exists())
      {
         InputActionManager.Instance.InputActionsActivate();
      }
   }
   
   private IEnumerator DisableInputAfterDelay(InputDelaySettings setting)
   {
      yield return new WaitForSeconds(setting.disableInputDelay);
      
      // Disable input after delay
      if (InputActionManager.Exists())
      {
         InputActionManager.Instance.InputActionsDeactivate();
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
         item.target.SetActive(true);
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
            item.target.SetActive(false);
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
      inputRoutines.Clear();
      
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
            Debug.Log($"    Current Status: {(activeStatus.ContainsKey(item) ? activeStatus[item] : "Unknown")}");
         }
         else
         {
            Debug.Log("  Item: NULL TARGET");
         }
      }
      
      foreach (var setting in inputDelaySettings)
      {
         Debug.Log($"  Input Setting for State: {setting.gameState}");
         Debug.Log($"    Enable Input Delay: {setting.enableInputDelay}");
         Debug.Log($"    Enable Input: {setting.enableInput}");
         Debug.Log($"    Enable Delay: {setting.inputEnableDelay}s");
         Debug.Log($"    Disable Delay: {setting.disableInputDelay}s");
      }
   }
}
using UnityEngine;
using System.Collections;

namespace RoofTops
{
    // We use the existing GameStates enum from the RoofTops namespace
    // GameStates: StartingUp, MainMenu, Playing, Paused, GameOver, ShuttingDown

    [System.Serializable]
    public class InputActivationSettings
    {
        [Tooltip("The game state the setting applies to")]
        public GameStates gameState;
        
        [Tooltip("Delay (in seconds) before applying input state for this state")]
        public float activationDelay = 0f;
        
        [Tooltip("Should the input be active (true) or disabled (false) for this state")]
        public bool inputActive = true;
    }

    public class InputActivationController : MonoBehaviour
    {
        [Header("Input Settings per State")]
        [Tooltip("Settings to control input activation for each game state")]
        public InputActivationSettings[] inputSettings;

        private void OnEnable()
        {
            // Subscribe to game state change events
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        // This method is called whenever the game state changes
        private void HandleGameStateChanged(GameStates oldState, GameStates newState)
        {
            foreach (var setting in inputSettings)
            {
                if (setting.gameState == newState)
                {
                    StartCoroutine(ApplyInputState(setting));
                    break;
                }
            }
        }

        private IEnumerator ApplyInputState(InputActivationSettings setting)
        {
            // Wait for the specified delay, if any
            if (setting.activationDelay > 0f)
                yield return new WaitForSeconds(setting.activationDelay);

            // Activate or deactivate input. Adjust these method calls to your input manager's API.
            if (InputActionManager.Exists())
            {
                if (setting.inputActive)
                {
                    InputActionManager.Instance.InputActionsActivate();
                    Debug.Log($"[InputActivationController] Input activated for state: {setting.gameState}");
                }
                else
                {
                    InputActionManager.Instance.InputActionsDeactivate();
                    Debug.Log($"[InputActivationController] Input deactivated for state: {setting.gameState}");
                }
            }
            else
            {
                Debug.LogWarning("[InputActivationController] InputActionManager not found.");
            }
        }
    }
} 
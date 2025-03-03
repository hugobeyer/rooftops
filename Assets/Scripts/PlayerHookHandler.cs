using UnityEngine;
using RoofTops; // We need this for GameManager

public class PlayerHookHandler : MonoBehaviour
{
    private CharacterController characterController; // Store the CharacterController

    private void Start()
    {
        // Get the CharacterController component.
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("PlayerHookHandler: CharacterController not found on this GameObject!");
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Check if the collided object is on the "Hook" layer
        if (hit.gameObject.layer == LayerMask.NameToLayer("Hook"))
        {
            // Call HandlePlayerDeath.
            GameManager.Instance.HandlePlayerDeath(GameManager.Instance.CurrentDistance);
        }
    }
} 
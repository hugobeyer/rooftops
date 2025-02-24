using UnityEngine;

namespace RoofTops
{
    [RequireComponent(typeof(Collider))]
    public class BreakableProp : MonoBehaviour
    {
        [Tooltip("Distance from the top of the prop to allow stomping.")]
        public float topTolerance = 0.2f;

        private void OnTriggerEnter(Collider other)
        {
            // Only handle collisions from the player
            if (!other.CompareTag("Player")) return;

            // Get the player controller for dash/death/velocity checks
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null) return;

            // Find where the player is relative to this prop
            float playerBottomY = other.bounds.min.y;
            float propTopY = GetComponent<Collider>().bounds.max.y;

            // Check if the player's feet are comfortably above the prop's top (a stomp)
            bool fromAbove = playerBottomY >= propTopY - topTolerance;

            // Also check if the player is dashing
            bool isPlayerDashing = IsPlayerDashing(player);

            if (fromAbove)
            {
                // The player is on top; do nothing (or optionally break the prop here if you want).
                // For example, to kill the prop on stomp, uncomment:
                // Destroy(gameObject);
            }
            else
            {
                // Side/front collision
                if (isPlayerDashing)
                {
                    // Destroy the prop if dashing
                    Destroy(gameObject);
                }
                else
                {
                    // Kill the player otherwise
                    player.HandleDeath();
                }
            }
        }

        // A helper method to check if the player is currently dashing
        private bool IsPlayerDashing(PlayerController player)
        {
            // The PlayerController has a bool `isDashing`, so just return that.
            // If you keep it private in PlayerController, add a public property or method for it.
            // Example:
            //   public bool IsDashing => isDashing;
            //
            // Then here:
            //   return player.IsDashing;
            // For now, assume we can access player.isDashing directly:
            var dashingField = typeof(PlayerController)
                                .GetField("isDashing", 
                                   System.Reflection.BindingFlags.Instance |
                                   System.Reflection.BindingFlags.NonPublic);
            if (dashingField != null)
            {
                return (bool)dashingField.GetValue(player);
            }
            return false;
        }
    }
} 
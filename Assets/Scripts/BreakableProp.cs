using UnityEngine;
using System.Collections;

namespace RoofTops
{
    [RequireComponent(typeof(Collider))]
    public class BreakableProp : MonoBehaviour
    {
        [Tooltip("Distance from the top of the prop to allow stomping.")]
        public float topTolerance = 0.2f;
        
        [Tooltip("Reference to the Animator component for explosion animation.")]
        public Animator animator;
        
        [Tooltip("Name of the trigger parameter in the Animator to start the explosion.")]
        public string explodeTriggerName = "explodeTrigger";
        
        [Tooltip("Time to wait before destroying the object after animation starts.")]
        public float destroyDelay = 1.0f;
        
        [Tooltip("Material with dissolve shader to animate during explosion")]
        public Material dissolveMaterial;
        
        private bool isExploding = false;
        private Material instanceMaterial;
        private float dissolveProgress = 0f;

        private void Start()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            
            // Create material instance if assigned (without Renderer)
            if (dissolveMaterial != null)
            {
                instanceMaterial = Instantiate(dissolveMaterial);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Don't process new collisions if already exploding
            if (isExploding) return;
            
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
                // TriggerExplosion();
            }
            else
            {
                // Side/front collision
                if (isPlayerDashing)
                {
                    // Trigger explosion animation and delayed destruction
                    TriggerExplosion();
                }
                else
                {
                    // Kill the player otherwise
                    player.HandleDeath();
                }
            }
        }
        
        // Trigger the explosion animation and schedule destruction
        private void TriggerExplosion()
        {
            if (isExploding) return;
            
            isExploding = true;
            
            // Disable the collider to prevent further interactions
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
            
            // Start dissolve animation coroutine
            if (dissolveMaterial != null)
            {
                StartCoroutine(AnimateDissolve());
            }
            
            // Trigger the animation if animator exists
            if (animator != null)
            {
                animator.SetTrigger(explodeTriggerName);
                Invoke("DestroyObject", destroyDelay);
            }
            else
            {
                DestroyObject();
            }
        }
        
        // Helper method to destroy the object
        private void DestroyObject()
        {
            Destroy(gameObject);
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

        private IEnumerator AnimateDissolve()
        {
            float elapsedTime = 0f;
            while (elapsedTime < destroyDelay)
            {
                dissolveProgress = Mathf.Lerp(0f, 1f, elapsedTime / destroyDelay);
                instanceMaterial.SetFloat("_Dissolve", dissolveProgress);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            instanceMaterial.SetFloat("_Dissolve", 1f);
        }
    }
} 
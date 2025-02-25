using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RoofTops
{
    [RequireComponent(typeof(Collider))]
    public class BreakableProp : MonoBehaviour
    {
        [Tooltip("Distance from the top of the prop to allow stomping.")]
        public float topTolerance = 0.2f;
        
        [Header("Animation")]
        [Tooltip("Reference to the Animator component for explosion animation.")]
        public Animator animator;
        
        [Tooltip("Name of the trigger parameter in the Animator to start the explosion.")]
        public string explodeTriggerName = "explodeTrigger";
        
        [Tooltip("Time to wait before destroying the object after animation starts.")]
        public float destroyDelay = 1.0f;
        
        [Header("Dissolve Effect")]
        [Tooltip("Material with dissolve shader to animate during explosion")]
        public Material dissolveMaterial;
        
        [Tooltip("Name of the dissolve property in the shader")]
        public string dissolvePropertyName = "_Dissolve";
        
        [Tooltip("Starting value for the dissolve effect")]
        [Range(0f, 1f)]
        public float dissolveStartValue = 0f;
        
        [Tooltip("Final value for the dissolve effect")]
        [Range(0f, 1f)]
        public float dissolveEndValue = 1f;
        
        [Header("Audio")]
        [Tooltip("Reference to the AudioSource that will play when the prop is destroyed")]
        public AudioSource destroyAudioSource;
        
        [Tooltip("Should the pitch be slightly randomized?")]
        public bool randomizePitch = true;
        
        [Tooltip("Range for pitch randomization")]
        [Range(0f, 1f)]
        public float pitchVariation = 0.2f;
        
        private bool isExploding = false;
        private Material instanceMaterial;
        private float dissolveProgress = 0f;
        private List<Renderer> renderers = new List<Renderer>();
        private List<Material> originalMaterials = new List<Material>();

        private void Start()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            
            // Store all renderers for later use
            renderers.AddRange(GetComponentsInChildren<Renderer>());
            
            // Create material instance if assigned and set up renderers
            if (dissolveMaterial != null && renderers.Count > 0)
            {
                // Create a single instance of the dissolve material
                instanceMaterial = Instantiate(dissolveMaterial);
                instanceMaterial.SetFloat(dissolvePropertyName, dissolveStartValue);
                
                // Store original materials and apply the dissolve material
                foreach (Renderer rend in renderers)
                {
                    // Store original materials
                    Material[] mats = rend.materials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        originalMaterials.Add(mats[i]);
                    }
                    
                    Debug.Log($"Setting up dissolve material on {rend.name}");
                }
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
            
            // Play destruction sound
            PlayDestructionSound();
            
            // Disable the collider to prevent further interactions
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
            
            // Apply the dissolve material to all renderers
            if (dissolveMaterial != null && instanceMaterial != null && renderers.Count > 0)
            {
                // Apply dissolve material to all renderers
                foreach (Renderer rend in renderers)
                {
                    Material[] newMaterials = new Material[rend.materials.Length];
                    for (int i = 0; i < newMaterials.Length; i++)
                    {
                        newMaterials[i] = instanceMaterial;
                    }
                    rend.materials = newMaterials;
                }
                
                // Start the dissolve animation
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
        
        // Play the destruction sound effect
        private void PlayDestructionSound()
        {
            if (destroyAudioSource != null)
            {
                // Apply random pitch if enabled
                if (randomizePitch)
                {
                    destroyAudioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
                }
                
                // Play the assigned audio source
                destroyAudioSource.Play();
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
            Debug.Log("Starting dissolve animation!");
            
            float elapsedTime = 0f;
            while (elapsedTime < destroyDelay)
            {
                dissolveProgress = Mathf.Lerp(dissolveStartValue, dissolveEndValue, elapsedTime / destroyDelay);
                instanceMaterial.SetFloat(dissolvePropertyName, dissolveProgress);
                
                if (elapsedTime % 0.2f < 0.01f)
                {
                    Debug.Log($"Dissolve progress: {dissolveProgress}");
                }
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            instanceMaterial.SetFloat(dissolvePropertyName, dissolveEndValue);
            Debug.Log("Dissolve animation complete!");
        }
    }
} 
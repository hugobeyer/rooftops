using UnityEngine;

namespace RoofTops
{
    public class JumpPad : MonoBehaviour
    {
        [Header("Jump Settings")]
        public float baseJumpForce = 15f;
        public float forceIncreaseRate = 0.1f; // Increases by 10% per second
        private float currentJumpForce;
        
        [Header("Audio")]
        public AudioSource jumpPadSource;
        public AudioClip jumpPadSound;
        [Range(0.0f, 1.0f)] public float jumpPadVolume = 1f;
        
        [Header("Effect Settings")]
        public float effectDuration = 1.5f;     // Duration of pitch/filter effect
        public float effectPitch = 0.7f;        // Pitch during effect (optional)
        public GameObject jumpPadEffectPrefab;  // Add this field
        
        // Animation parameter hashes
        private readonly int jumpSpeedMultiplierHash = Animator.StringToHash("jumpSpeedMultiplier");
        private readonly int jumpTriggerHash = Animator.StringToHash("jumpTrigger");

        private JumpPadCameraController cameraController;

        void Start()
        {
            // Setup audio source
            if (jumpPadSource == null)
            {
                jumpPadSource = GetComponent<AudioSource>();
                if (jumpPadSource == null)
                {
                    jumpPadSource = gameObject.AddComponent<AudioSource>();
                }
            }
            jumpPadSource.clip = jumpPadSound;
            jumpPadSource.playOnAwake = false;
            jumpPadSource.volume = jumpPadVolume;

            currentJumpForce = baseJumpForce;
            cameraController = FindAnyObjectByType<JumpPadCameraController>();
        }

        void Update()
        {
            // Increase force over time
            currentJumpForce += baseJumpForce * forceIncreaseRate * Time.deltaTime;
        }

        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"Something hit jump pad: {other.gameObject.name}");  // Log any collision
            
            if (other.CompareTag("Player"))
            {
                Debug.Log("It was the player!");  // Log player collision
                
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    Debug.Log("Found PlayerController");  // Log component found
                    
                    // Debug to check if we're hitting the trigger
                    Debug.Log("JumpPad triggered");

                    // Spawn effect directly
                    if (jumpPadEffectPrefab != null)
                    {
                        Debug.Log("Have effect prefab");  // Log prefab exists
                        Vector3 effectPosition = transform.position + Vector3.up * 0.5f;
                        GameObject effect = Instantiate(jumpPadEffectPrefab, effectPosition, Quaternion.identity);
                        effect.transform.SetParent(transform);  // Parent to the jump pad
                        effect.SetActive(true);
                    }
                    else
                    {
                        Debug.LogWarning("No jump pad effect prefab assigned!");
                    }

                    // Notify player they're on a jump pad
                    player.OnJumpPadActivated();

                    // Play jump pad sound
                    if (jumpPadSource != null && jumpPadSound != null)
                    {
                        jumpPadSource.Play();
                    }
                    
                    // Add audio filter effect with configurable duration and pitch
                    GameManager.Instance?.ActivateJumpPadAudioEffect(effectDuration, effectPitch);
                    
                    // Trigger camera effect
                    cameraController?.OnJumpPadTriggered(true);

                    // Set velocity directly through reflection
                    var velocityField = typeof(PlayerController).GetField("_velocity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Vector3 velocity = (Vector3)velocityField.GetValue(player);
                    velocity.y = currentJumpForce;
                    velocityField.SetValue(player, velocity);
                    
                    // Get animator and set jump speed
                    Animator animator = player.GetComponent<Animator>();
                    if(animator != null)
                    {
                        float timeToApex = currentJumpForce / -Physics.gravity.y;  // Time to reach highest point
                        float totalAirTime = timeToApex * 2;  // Total time in air (up + down)
                        float animationLength = 1f;
                        float jumpSpeedRatio = animationLength / totalAirTime;
                        
                        animator.SetFloat(jumpSpeedMultiplierHash, jumpSpeedRatio);
                        animator.SetTrigger(jumpTriggerHash);
                    }

                    // Calculate blend amount based on current jump force
                    float blendAmount = (currentJumpForce - baseJumpForce) / baseJumpForce;  // This gives 0 at base force, 1 at 2x base force
                    blendAmount = Mathf.Clamp01(blendAmount);  // Ensure it stays between 0 and 1
                    
                    // Trigger camera effect with player transform and jump force
                    if (CameraZoomEffect.Instance != null)
                    {
                        CameraZoomEffect.Instance.TriggerJumpPadEffect(player.transform, currentJumpForce);
                    }

                    // Add time scale effect with much longer duration
                    GameManager.Instance.SlowTimeForJump(0.3f, 1.5f);  // 0.3x speed for 1.5 seconds
                }
            }
        }
    }
} 
using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [Header("Jump Settings")]
    public float baseJumpForce = 15f;
    public float forceIncreaseRate = 0.1f; // Increases by 10% per second
    private float currentJumpForce;

    void Start()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if(renderer != null)
        {
            renderer.material.color = Color.magenta;
        }
        else
        {
            Debug.LogWarning("No MeshRenderer found on JumpPad. Please add one, or adjust the script accordingly.");
        }

        currentJumpForce = baseJumpForce;
    }

    void Update()
    {
        // Increase force over time
        currentJumpForce += baseJumpForce * forceIncreaseRate * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // Set velocity directly through reflection
                var velocityField = typeof(PlayerController).GetField("_velocity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Vector3 velocity = (Vector3)velocityField.GetValue(player);
                velocity.y = currentJumpForce;
                velocityField.SetValue(player, velocity);
                
                // Trigger the full jump animation sequence
                PlayerAnimatorController animController = player.GetComponent<PlayerAnimatorController>();
                if(animController != null)
                {
                    animController.TriggerJumpAnimation(currentJumpForce);
                }

                // Add time scale effect with much longer duration
                GameManager.Instance.SlowTimeForJump(0.3f, 1.5f);  // 0.3x speed for 1.5 seconds
            }
        }
    }
} 
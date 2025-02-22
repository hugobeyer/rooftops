using UnityEngine;

public class RagdollCollisionHandler : MonoBehaviour
{
    private float pushForce;
    private float upForce;
    private Rigidbody rb;
    private float lastCollisionTime;
    private const float MIN_COLLISION_INTERVAL = 0.2f; // Limit how often we apply forces

    public void Initialize(float push, float up)
    {
        pushForce = push;
        upForce = up;
        rb = GetComponent<Rigidbody>();
        lastCollisionTime = -MIN_COLLISION_INTERVAL; // Allow first collision immediately
    }

    void OnCollisionEnter(Collision collision)
    {
        // Only process collision if enough time has passed
        if (Time.time - lastCollisionTime < MIN_COLLISION_INTERVAL) return;
        
        // Only process if we're moving fast enough to need correction
        if (rb.linearVelocity.magnitude < 1f) return;

        // Use just the first contact point instead of averaging all
        if (collision.contacts.Length > 0)
        {
            Vector3 pushDirection = (collision.contacts[0].normal + Vector3.up * upForce).normalized;
            rb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
            lastCollisionTime = Time.time;
        }
    }
} 
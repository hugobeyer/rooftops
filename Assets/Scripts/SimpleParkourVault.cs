using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class SimpleParkourVault : MonoBehaviour
{
    [Header("Vault Settings")]
    [Tooltip("Distance for detecting a wall in front of the player.")]
    public float detectionDistance = 1.5f;
    
    [Tooltip("Maximum wall height that can be vaulted (in world units).")]
    public float maxWallHeight = 2f;
    
    [Tooltip("Additional height offset above the wall for vaulting.")]
    public float vaultHeightOffset = 1f;
    
    [Tooltip("If true, vault triggers automatically when a vaultable wall is detected.")]
    public bool autoVault = true;
    
    [Tooltip("Duration for vault lerp movement (in seconds).")]
    public float vaultLerpTime = 0.5f;
    
    private Rigidbody rb;
    private bool isVaulting = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Do nothing if currently vaulting.
        if (isVaulting) return;
        
        // Define a ray at chest height.
        Vector3 origin = transform.position + Vector3.up * 1f;
        // For visual debugging in the Game view.
        Debug.DrawRay(origin, transform.forward * detectionDistance, Color.yellow, 0.1f);
        Ray ray = new Ray(origin, transform.forward);
        
        // Perform the raycast.
        if (Physics.Raycast(ray, out RaycastHit hit, detectionDistance))
        {
            // Only proceed if the hit collider is tagged as "GroundCollider".
            if (!hit.collider.CompareTag("GroundCollider"))
            {
                Debug.Log("Ray hit object not tagged 'GroundCollider': " + hit.collider.name);
            }
            else
            {
                // Debug: log the wall's collider height.
                float wallHeight = hit.collider.bounds.size.y;
                Debug.Log("Detected wall height: " + wallHeight);

                // Check if the wall's height qualifies as vaultable.
                if (wallHeight <= maxWallHeight && autoVault)
                {
                    // Compute target height by adding the vaultHeightOffset to the wall's top.
                    float targetY = hit.collider.bounds.max.y + vaultHeightOffset;
                    Debug.Log("Vault triggered! TargetY = " + targetY);
                    StartCoroutine(VaultCoroutine(targetY));
                }
                else
                {
                    Debug.Log("Wall not vaultable (wallHeight > maxWallHeight). maxWallHeight set to " + maxWallHeight);
                }
            }
        }
    }

    private IEnumerator VaultCoroutine(float targetY)
    {
        isVaulting = true;
        
        // Temporarily disable physics by setting the Rigidbody to kinematic.
        bool originalKinematic = rb.isKinematic;
        rb.isKinematic = true;

        // Lerp player's position from current to target height.
        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3(startPos.x, targetY, startPos.z);
        float elapsed = 0f;

        while (elapsed < vaultLerpTime)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / vaultLerpTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = targetPos;
        rb.isKinematic = originalKinematic;
        isVaulting = false;
    }

    // Optional gizmo for visual debugging.
    void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up * 1f;
        Ray ray = new Ray(origin, transform.forward);
        Color gizmoColor = isVaulting ? Color.green : Color.red;
        
        // Draw the detection ray.
        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(origin, origin + transform.forward * detectionDistance);
        
        // If a wall is hit, draw its collider bounds.
        if (Physics.Raycast(ray, out RaycastHit hit, detectionDistance))
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(hit.collider.bounds.center, hit.collider.bounds.size);
        }
    }
} 
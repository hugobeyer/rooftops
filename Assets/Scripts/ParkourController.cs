using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class ParkourController : MonoBehaviour
{
    [Header("Parkour Settings")]
    [Tooltip("Maximum wall height that can be vaulted (in world units).")]
    public float maxWallHeight = 1.5f;
    
    // [Tooltip("The upward force applied when vaulting.")] // No force needed now.
    // public float vaultForce = 8f;
    
    [Tooltip("Distance from the player for wall detection.")]
    public float detectionDistance = 1f;
    
    [Tooltip("Buffer time to detect jump input before reaching the wall.")]
    public float jumpBufferTime = 0.2f;
    [Tooltip("If true, vault triggers automatically without needing jump input.")]
    public bool autoVault = false;

    private bool bufferedJump = false;
    private float jumpBufferTimer = 0f;
    // New cooldown timer to prevent repeated vaults.
    private float vaultCooldownTimer = 0f;
    [Tooltip("Cooldown time after a vault is triggered (in seconds).")]
    public float vaultCooldown = 0.5f;
    private Rigidbody rb;

    // New public parameter: Duration for vault lerp movement (in seconds).
    [Tooltip("Duration for vault lerp movement (in seconds).")]
    public float vaultLerpTime = 0.5f;
    private bool isVaulting = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Buffer the jump input
        if (Input.GetButtonDown("Jump"))
        {
            bufferedJump = true;
            jumpBufferTimer = jumpBufferTime;
        }
        if (bufferedJump)
        {
            jumpBufferTimer -= Time.deltaTime;
            if (jumpBufferTimer <= 0f)
            {
                bufferedJump = false;
            }
        }
    }

    void FixedUpdate()
    {
        // First update the cooldown timer.
        if (vaultCooldownTimer > 0f)
        {
            vaultCooldownTimer -= Time.fixedDeltaTime;
        }

        Vector3 rayOrigin = transform.position + Vector3.up * 1.0f; 
        if (Physics.Raycast(rayOrigin, transform.forward, out RaycastHit hit, detectionDistance))
        {
            float wallHeight = hit.collider.bounds.size.y;
            // Only vault if the wall's height is vaultable, and either a jump is buffered or autoVault is enabled, and we're not on cooldown.
            if (wallHeight <= maxWallHeight && (bufferedJump || autoVault) && vaultCooldownTimer <= 0f)
            {
                // Use the top of the collider as the target height and pass the wall's collider.
                VaultOverWall(hit.collider.bounds.max.y, hit.collider);
                // Only consume buffered input if autoVault is not enabled.
                if (!autoVault)
                {
                    bufferedJump = false;
                }
                vaultCooldownTimer = vaultCooldown; // Start cooldown
            }
        }
    }
    
    // The vault action now performs a smooth vertical transition (lerp) to the top of the vaultable wall.
    // targetY is taken from the wall collider's bounds (hit.collider.bounds.max.y)
    // The wallCollider parameter is used to temporarily disable collisions between the wall and the player during the vault.
    private void VaultOverWall(float targetY, Collider wallCollider)
    {
        StartCoroutine(VaultLerp(targetY, wallCollider));
    }

    private IEnumerator VaultLerp(float targetY, Collider wallCollider)
    {
        // Set flag and print debug signal.
        isVaulting = true;
        Debug.Log("Vaulting triggered, lerping to target height: " + targetY);

        // Store the original kinematic state, then disable physics so we can control position manually.
        bool originalKinematic = rb.isKinematic;
        rb.isKinematic = true;
        
        // Retrieve the player's collider (assumes the player has one attached).
        Collider playerCol = GetComponent<Collider>();
        if(playerCol != null)
        {
            // Temporarily ignore collisions between the player and the wall.
            Physics.IgnoreCollision(playerCol, wallCollider, true);
        }

        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3(startPos.x, targetY, startPos.z);
        while (elapsed < vaultLerpTime)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / vaultLerpTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;

        // Restore the original physics simulation setting.
        rb.isKinematic = originalKinematic;
        
        if(playerCol != null)
        {
            // Re-enable collisions between the player and the wall.
            Physics.IgnoreCollision(playerCol, wallCollider, false);
        }

        // End of vault; clear the vaulting flag.
        isVaulting = false;
    }

    // Optional: Visualize the detection ray in the Scene view.
    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up * 1.0f; 
        Ray ray = new Ray(origin, transform.forward);
        Color gizmoColor = Color.red; // Default color
        if (Physics.Raycast(ray, out RaycastHit hit, detectionDistance))
        {
            // If vaulting is happening, force the ray color to green.
            if (isVaulting || (hit.collider.bounds.size.y <= maxWallHeight && (bufferedJump || autoVault)))
            {
                gizmoColor = Color.green;
            }

            // Draw a wire cube representing the wall's collider bounds.
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(hit.collider.bounds.center, hit.collider.bounds.size);
        }
        // Reset Gizmos.color to the detection ray color.
        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(origin, origin + transform.forward * detectionDistance);
    }
} 
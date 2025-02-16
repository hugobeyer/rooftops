using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class SimpleCharacterVault : MonoBehaviour
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
    
    private CharacterController cc;
    private bool isVaulting = false;

    void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    void FixedUpdate()
    {
        if (isVaulting)
            return;  // Skip processing while vaulting is in progress

        // Cast a ray from chest height.
        Vector3 origin = transform.position + Vector3.up * 1f;
        Debug.DrawRay(origin, transform.forward * detectionDistance, Color.yellow, 0.1f);
        Ray ray = new Ray(origin, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, detectionDistance))
        {
            // Only consider colliders tagged as "GroundCollider" (adjust if needed).
            if (!hit.collider.CompareTag("GroundCollider"))
            {
                Debug.Log("Ray hit object not tagged 'GroundCollider': " + hit.collider.name);
                return;
            }
            
            // Log detected wall info.
            float wallHeight = hit.collider.bounds.size.y;
            Debug.Log("Detected wall height: " + wallHeight);
            
            // If the wall qualifies as vaultable and autoVault is enabled...
            if (wallHeight <= maxWallHeight && autoVault)
            {
                // Calculate the target height: the wall's top plus an offset.
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

    private IEnumerator VaultCoroutine(float targetY)
    {
        isVaulting = true;
        // Disable the CharacterController temporarily so manual transform changes aren't interfered with.
        cc.enabled = false;
        
        // Lerp the character's position from its current position to the target height.
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
        cc.enabled = true;
        isVaulting = false;
    }

    // Optional: Visual debug gizmos to help you see the detection ray and hit bounds in the Scene view.
    void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up * 1f;
        Ray ray = new Ray(origin, transform.forward);
        Color gizmoColor = isVaulting ? Color.green : Color.red;
        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(origin, origin + transform.forward * detectionDistance);
        
        if (Physics.Raycast(ray, out RaycastHit hit, detectionDistance))
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(hit.collider.bounds.center, hit.collider.bounds.size);
        }
    }
} 
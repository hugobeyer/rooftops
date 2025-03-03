using UnityEngine;
using RoofTops;
using System.Collections;

public class HookDropper : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private float spawnInterval = 1f; // Time between spawns
    [SerializeField] private float dropDelay = 2f; // Delay before the first drop
    [SerializeField] private int maxDrops = 9; // Maximum number of hooks to drop before exiting

    public int groupSize = 3;
    public float groupSpacing = 0.5f;
    
    [Header("Drop State")]
    [SerializeField] private bool isEnabled = true;
    
    [Header("Player Reference")]
    [Tooltip("Reference to the player transform. The dropper will be destroyed if the player dies.")]
    public Transform playerReference;
    
    [Header("Drop Tracking")]
    [Tooltip("Current number of drops performed")]
    public int dropCount = 0;
    
    private float nextDropTime;
    private DroneMovement droneMovement;

    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            isEnabled = value;
            nextDropTime = Time.time + dropDelay; // Reset next drop time when enabled/disabled
        }
    }
    
    public void ToggleDropper()
    {
        IsEnabled = !IsEnabled;
    }
    
    void Start()
    {
        // Reset drop count at start
        dropCount = 0;
        
        // If no player reference has been assigned, try to find one using the "Player" tag.
        if (playerReference == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                playerReference = player.transform;
            }
        }

        // Get reference to the drone movement component
        droneMovement = GetComponentInParent<DroneMovement>();
        if (droneMovement == null)
        {
            droneMovement = GetComponent<DroneMovement>();
        }

        // Initialize the next drop time, adding the delay
        nextDropTime = Time.time + dropDelay;
    }

    void Update()
    {
        // Check if the player is still alive; if not, destroy the dropper.
        if (playerReference == null)
        {
            Destroy(this);
            return;
        }

        if (!isEnabled || dropPrefab == null) return;

        // Check if we've reached the maximum number of drops
        if (dropCount >= maxDrops)
        {
            // If we have a drone movement component, trigger its exit
            if (droneMovement != null && !droneMovement.enableExitBehavior)
            {
                Debug.Log($"HookDropper: Reached maximum drops ({maxDrops}). Triggering drone exit.");
                droneMovement.TriggerExit();
                isEnabled = false; // Disable the dropper
            }
            return;
        }

        if (Time.time >= nextDropTime)
        {
            DropObject();
            dropCount++; // Increment drop count
            
            nextDropTime = Time.time + spawnInterval;

            // After every groupSize drops, add extra group spacing.
            if (groupSize > 0 && dropCount % groupSize == 0)
            {
                nextDropTime += groupSpacing;
            }
        }
    }

    private void DropObject()
    {
        // Store the drop position with a downward offset to avoid interfering with the drone's raycast
        Vector3 dropPosition = transform.position + new Vector3(0, -0.5f, 0);
        
        // Instantiate at the offset position with identity rotation
        GameObject currentDrop = Instantiate(dropPrefab, dropPosition, Quaternion.identity);

        // Ensure it's not parented to anything (in world space)
        currentDrop.transform.SetParent(null);

        // No need to apply velocity - gravity will handle it
        // Just make sure the Rigidbody is not kinematic if it exists
        Rigidbody rb = currentDrop.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            // Zero out any initial velocity to ensure clean gravity-only fall
            rb.linearVelocity = Vector3.zero;
        }

        // Increment the drop count
        dropCount++;
    }

    void OnDrawGizmos()
    {
        if (!isEnabled) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 50f);
    }
}

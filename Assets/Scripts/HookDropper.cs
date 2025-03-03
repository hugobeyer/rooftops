using UnityEngine;
using RoofTops;
using System.Collections;

public class HookDropper : MonoBehaviour
{
    public Vector3 initialVelocity;
    [Header("Drop Settings")]
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private float spawnInterval = 1f; // Time between spawns
    [SerializeField] private float dropDelay = 2f; // Delay before the first drop


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
    
    private float timer;
    private float nextDropTime;
    private int dropsMade = 0; // Keep track of how many drops have happened

    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            isEnabled = value;
            timer = 0f; // Reset timer when enabled/disabled
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
        dropsMade = 0;
        
        // If no player reference has been assigned, try to find one using the "Player" tag.
        if (playerReference == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                playerReference = player.transform;
            }
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

        if (Time.time >= nextDropTime)
        {
            DropObject();
            dropsMade++; // Increment drop count
            
            nextDropTime = Time.time + spawnInterval;

            // After every groupSize drops, add extra group spacing.
            if (groupSize > 0 && dropsMade % groupSize == 0)
            {
                nextDropTime += groupSpacing;
            }
        }
    }

    private void DropObject()
    {
        // Instantiate WITHOUT parenting initially
        GameObject currentDrop = Instantiate(dropPrefab, transform.position, Quaternion.identity);

        // Then, parent to the world
        currentDrop.transform.SetParent(null);

        // Get the Rigidbody of the instantiated object
        Rigidbody rb = currentDrop.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Assuming initialVelocity is set in the Inspector
            rb.linearVelocity = initialVelocity;
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

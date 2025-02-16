using UnityEngine;
using TMPro;

public class JumpDistanceDisplay : MonoBehaviour
{
    public TMP_Text distanceText;  // Assign via inspector
    public float holdDuration = 0.5f; // Time to show final distance after landing
    public float fadeDuration = 0.3f;

    // New public parameter: assign the transform that moves (e.g. ModulePool.moduleMovement)
    [Tooltip("Assign the transform that is moving (usually ModulePool.moduleMovement). If left empty, the script will try to find it automatically.")]
    public Transform movementSource;

    private PlayerController player;
    private float jumpStartZ;
    private Coroutine fadeCoroutine;
    private static JumpDistanceDisplay instance;

    void Awake()
    {
        instance = this;
        if (distanceText == null)
            distanceText = GetComponent<TMP_Text>();
        player = FindAnyObjectByType<PlayerController>();

        // If movementSource wasn't assigned in the inspector, try auto-assign it from ModulePool.
        if(movementSource == null)
        {
            ModulePool pool = FindAnyObjectByType<ModulePool>();
            if(pool != null)
                movementSource = pool.moduleMovement;
        }
        
        // Initially hide the text.
        if(distanceText != null)
            distanceText.alpha = 0f;
    }

    void Update()
    {
        if (!player || !movementSource) return;

        // Start tracking when leaving ground
        if (player.IsGroundedOnCollider || player.IsGroundedOnTrigger)
        {
            jumpStartZ = movementSource.position.z;
        }

        // Show final distance when landing
        if ((player.IsGroundedOnCollider || player.IsGroundedOnTrigger) && 
            player.LastJumpDistance > 0)
        {
            ShowDistance(player.LastJumpDistance);
        }
    }

    void ShowDistance(float distance)
    {
        float absoluteDistance = Mathf.Abs(distance);
        distanceText.text = $"+{absoluteDistance:F1}m";
        
        // Notify total tracker
        TotalJumpDistanceTracker tracker = FindAnyObjectByType<TotalJumpDistanceTracker>();
        tracker?.AddJumpDistance(absoluteDistance);
        
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeDisplay());
    }

    System.Collections.IEnumerator FadeDisplay()
    {
        distanceText.alpha = 1f;
        yield return new WaitForSeconds(holdDuration);
        
        float elapsed = 0f;
        Color originalColor = distanceText.color;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            distanceText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        
        distanceText.text = "";
        distanceText.color = originalColor;
    }

    public static void ResetDisplay()
    {
        if(instance != null && instance.distanceText != null)
        {
            instance.distanceText.text = "";
            instance.distanceText.alpha = 0f;
            instance.jumpStartZ = 0f;
            if(instance.fadeCoroutine != null)
                instance.StopCoroutine(instance.fadeCoroutine);
        }
    }
} 
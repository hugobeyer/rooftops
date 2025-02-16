using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    [Header("Core Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 3f, -10);
    public float smoothTime = 0.3f;

    [Header("Camera Effects")]
    public float shakeIntensity = 0.1f;
    public float shakeFrequency = 1f;
    
    [Header("Pitch Variation")]
    public float pitchIntensity = 25f;
    public float pitchSpeed = 2f;
    public AnimationCurve pitchVariationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Death Camera")]
    public float deathTransitionDuration = 1.0f;
    public float deathHeightOffset = 2f;
    public float deathDistanceOffset = -15f;  // How far back to move camera
    private bool isInDeathView = false;
    private Vector3 originalOffset;

    private Vector3 velocity = Vector3.zero;
    private Vector3 noiseOffset;
    private float pitchVelocity;
    private float currentPitch;

    public PlayerController player;

    void Start()
    {
        noiseOffset = new Vector3(Random.Range(0f, 1000f), Random.Range(0f, 1000f), 0);
        originalOffset = offset;
    }

    void LateUpdate()
    {
        if(!player) return;
        
        // Add pause check
        if (GameManager.Instance.IsPaused)
        {
            // Don't update camera when paused
            return;
        }
        
        Vector3 targetPosition;
        
        if (isInDeathView)
        {
            // In death view, look at next module if available
            GameObject nextModule = ModulePool.Instance?.GetNextModule();
            if (nextModule != null)
            {
                targetPosition = nextModule.transform.position + offset;
            }
            else
            {
                targetPosition = target.position + offset;
            }
        }
        else
        {
            // Normal following
            targetPosition = target.position + offset;
        }
        
        targetPosition += CalculateShakeOffset();
        
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetPosition, 
            ref velocity, 
            smoothTime
        );

        // Only apply pitch when not in death view
        if (!isInDeathView)
        {
            ApplyPitch();
        }
        else
        {
            // In death view, look straight ahead
            transform.rotation = Quaternion.LookRotation(
                Vector3.forward,
                Vector3.up
            );
        }
    }

    Vector3 CalculateShakeOffset()
    {
        if(shakeIntensity <= 0) return Vector3.zero;
        
        float time = Time.time * shakeFrequency;
        
        return new Vector3(
            Mathf.PerlinNoise(time, noiseOffset.x) - 0.5f,
            Mathf.PerlinNoise(noiseOffset.y, time) - 0.5f,
            0
        ) * shakeIntensity;
    }

    void ApplyPitch()
    {
        if(target.TryGetComponent<PlayerController>(out var player))
        {
            //Rigidbody rb = player.GetComponent<Rigidbody>();
            float horizontalSpeed = Mathf.PerlinNoise(Time.time, noiseOffset.x) - 0.5f;
            // float someValue = player.maxRunSpeed; // comment it out or remove it
            float normalizedSpeed = horizontalSpeed;
            
            float targetPitch = pitchVariationCurve.Evaluate(normalizedSpeed) * pitchIntensity;
            
            currentPitch = Mathf.SmoothDamp(
                currentPitch, 
                targetPitch, 
                ref pitchVelocity, 
                pitchSpeed
            );
        }

        Quaternion lookRotation = Quaternion.LookRotation(
            target.position - transform.position + Vector3.up * 0.5f
        );
        
        transform.rotation = lookRotation * Quaternion.Euler(currentPitch, 0, currentPitch);
    }

    public void TransitionToDeathView()
    {
        if (isInDeathView) return;
        isInDeathView = true;
        StartCoroutine(TransitionCamera());
    }

    private IEnumerator TransitionCamera()
    {
        Vector3 startOffset = offset;
        
        // Get the next module's height
        float targetHeight = deathHeightOffset;
        if (ModulePool.Instance != null)
        {
            GameObject nextModule = ModulePool.Instance.GetNextModule();
            if (nextModule != null)
            {
                targetHeight = nextModule.transform.position.y + deathHeightOffset;
            }
        }
        
        Vector3 deathOffset = new Vector3(0, targetHeight, deathDistanceOffset);
        float elapsedTime = 0;

        while (elapsedTime < deathTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / deathTransitionDuration;
            
            // Smooth interpolation
            t = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
            
            offset = Vector3.Lerp(startOffset, deathOffset, t);
            yield return null;
        }
    }

    public void ResetCamera()
    {
        isInDeathView = false;
        offset = originalOffset;
    }
} 
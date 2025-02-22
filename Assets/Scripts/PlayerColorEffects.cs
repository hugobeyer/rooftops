using UnityEngine;
using System.Collections;

public class PlayerColorEffects : MonoBehaviour
{
    // Removed unused color fields
    
    public Material targetMaterial;  // Assign in Inspector, or it will be auto-fetched at runtime if left unassigned
    
    private Coroutine colorCoroutine;
    
    // Cache property ID
    private readonly int jumpLerpID = Shader.PropertyToID("_JumpLerp");  // Updated to match shader property name

    // Public parameters for the color effect timings
    public float lerpInTime = 0.5f;      // Duration (in seconds) to interpolate from 0 to 1
    public float holdDuration = 4.0f;      // Duration (in seconds) to hold the value at 1
    public float lerpOutTime = 0.5f;     // Duration (in seconds) to interpolate from 1 to 0

    // Add a guard flag to prevent repeated triggers
    private bool isEffectRunning = false;

    void Start()
    {
        if (targetMaterial == null)
        {
            // Get the MeshRenderer component on this GameObject (the mesh)
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                return;
            }

            // Use the instance material to avoid modifying a shared asset
            targetMaterial = meshRenderer.material;
        }
        targetMaterial.SetFloat(jumpLerpID, 0);
    }

    public void StartSlowdownEffect()
    {
        if (targetMaterial == null)
        {
            return;
        }

        // Trigger the effect only if it isn't already running.
        if (!isEffectRunning)
        {
            isEffectRunning = true;
            StartCoroutine(LerpJumpEffect());
        }
    }

    private IEnumerator LerpJumpEffect()
    {
        // Lerp in: interpolate from 0 to 1 over lerpInTime
        yield return StartCoroutine(LerpEffect(0f, 1f, lerpInTime));

        // Hold the state at 1 for holdDuration seconds (real time)
        yield return new WaitForSecondsRealtime(holdDuration);

        // Lerp out: interpolate from 1 to 0 over lerpOutTime
        yield return StartCoroutine(LerpEffect(1f, 0f, lerpOutTime));

        targetMaterial.SetFloat(jumpLerpID, 0f);
        // Mark the effect as finished.
        isEffectRunning = false;
    }

    // Helper coroutine to interpolate the shader value
    private IEnumerator LerpEffect(float startValue, float endValue, float duration)
    {
        float startTime = Time.unscaledTime;
        while (Time.unscaledTime - startTime < duration)
        {
            float t = (Time.unscaledTime - startTime) / duration;
            float lerpValue = Mathf.Lerp(startValue, endValue, t);
            targetMaterial.SetFloat(jumpLerpID, lerpValue);
            yield return null;
        }
    }

    // Removed internal input handling.
    
    // Public method called by the Animator via an Animation Event.
    public void TriggerJumpEffect()
    {
        StartSlowdownEffect();
    }
} 
using UnityEngine;
using RoofTops;

public class JumpPadEffectController : MonoBehaviour
{
    [Header("Effect Settings")]
    public float effectDuration = 1f;       // How long the effect lasts
    public new ParticleSystem particleSystem;  // Add this if using particles

    void Start()
    {
        // Play particles if assigned
        if (particleSystem != null)
        {
            particleSystem.Play();
        }

        // Destroy this effect after duration
        Destroy(gameObject, effectDuration);
    }
} 
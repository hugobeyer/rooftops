using UnityEngine;

public class PlayPFXOnPlayerHit : MonoBehaviour
{
    [Tooltip("The particle effect prefab to play when this object hits the player.")]
    public GameObject pfxPrefab;

    [Tooltip("The layer name that represents the Player.")]
    public string playerLayerName = "Player";

    [Tooltip("Optional: Parent transform for the instantiated particle effect.")]
    public Transform pfxParent;

    // For physical collisions (non-trigger)
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer(playerLayerName))
        {
            // Get a contact point if available; otherwise, use the object's position.
            Vector3 spawnPos = (collision.contacts.Length > 0) 
                ? collision.contacts[0].point 
                : transform.position;

            if (pfxPrefab != null)
            {
                // Spawn the PFX at the collision point.
                GameObject pfxInstance = Instantiate(pfxPrefab, spawnPos, Quaternion.identity, pfxParent);

                // Get the ParticleSystem and force it to play, in case Play On Awake is disabled.
                ParticleSystem ps = pfxInstance.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                    float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
                    Destroy(pfxInstance, lifetime);
                }
                else
                {
                    // Fallback: destroy after 5 seconds.
                    Destroy(pfxInstance, 5f);
                }
            }
        }
    }

    // Optionally, if you want to support trigger-based collisions too.
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(playerLayerName))
        {
            Vector3 spawnPos = other.bounds.center; // Spawn at the center of the object.
            if (pfxPrefab != null)
            {
                GameObject pfxInstance = Instantiate(pfxPrefab, spawnPos, Quaternion.identity, pfxParent);
                ParticleSystem ps = pfxInstance.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                    float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
                    Destroy(pfxInstance, lifetime);
                }
                else
                {
                    Destroy(pfxInstance, 5f);
                }
            }
        }
    }
} 
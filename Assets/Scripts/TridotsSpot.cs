using UnityEngine;
using System.Collections;

namespace RoofTops
{
    public class TridotsSpot : MonoBehaviour
    {
        [Header("Tridots Settings")]
        public int scoreValue = 1;
        public float rotationSpeed = 90f;

        [Header("Audio")]
        public AudioSource audioSource;  // Reference to the AudioSource component
        [Range(0.8f, 1.2f)] public float minPitch = 0.9f;
        [Range(0.8f, 1.2f)] public float maxPitch = 1.1f;

        [Header("VFX")]
        public GameObject collectVFXPrefab;  // Assign particle effect prefab in inspector
        public float vfxYOffset = 0.5f;  // Default offset of 0.5 units up

        [Header("Explosion Effect")]
        public float explosionDuration = 0.25f;
        private Material material;
        private MeshRenderer meshRenderer;
        private bool isCollected = false;

        void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                // Create a material instance to avoid modifying the shared material
                material = new Material(meshRenderer.material);
                meshRenderer.material = material;
            }

            // Get AudioSource if not assigned
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        void Update()
        {
            if (!isCollected)  // Only rotate if not collected
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !isCollected)  // Add collected check
            {
                isCollected = true;  // Stop rotation immediately

                // Spawn collection VFX
                if (collectVFXPrefab != null)
                {
                    Vector3 spawnPos = transform.position + Vector3.up * vfxYOffset;
                    GameObject vfx = Instantiate(collectVFXPrefab, spawnPos, Quaternion.identity);
                    vfx.transform.SetParent(transform);
                }

                // Play collection sound with random pitch
                if (audioSource != null)
                {
                    audioSource.pitch = Random.Range(minPitch, maxPitch);
                    audioSource.Play();
                }

                // Update tridots centrally via EconomyManager (AddTridots internally updates gameData)
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.AddTridots(scoreValue);
                }
                else
                {
                    Debug.LogWarning("EconomyManager.Instance is null in TridotsSpot.OnTriggerEnter");
                }

                // Remove ScoreManager code and replace with comment
                // Score is now handled through EconomyManager

                // Start explosion effect before destroying
                StartCoroutine(ExplodeAndDestroy());
            }
        }

        private IEnumerator ExplodeAndDestroy()
        {
            float elapsed = 0;

            if (material != null)
            {
                // Enable keyword if not already enabled
                material.EnableKeyword("_EXPLODE");

                while (elapsed < explosionDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / explosionDuration;

                    // Set the _Explode property from 0 to 1
                    material.SetFloat("_Explode", progress);

                    yield return null;
                }
            }

            // Wait for audio to finish if it's playing
            if (audioSource != null && audioSource.isPlaying)
            {
                yield return new WaitForSeconds(audioSource.clip.length);
            }

            Destroy(gameObject);
        }

        void OnDestroy()
        {
            // Clean up the material instance
            if (material != null)
            {
                Destroy(material);
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
    }
}
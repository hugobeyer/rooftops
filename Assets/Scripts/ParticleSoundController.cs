using UnityEngine;
using System.Collections;

namespace RoofTops
{
    public class ParticleSoundController : MonoBehaviour
    {
        [Header("Particle Settings")]
        private ParticleSystem particleSystemComponent;
        private ParticleSystem.Particle[] particles;
        private int previousParticleCount = 0;
        
        [Header("Timing Settings")]
        [SerializeField] private float startDelay = 2f;
        private bool hasStarted = false;
        
        [Header("Audio Settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip particleSound;
        [SerializeField] private float minPitch = 0.8f;
        [SerializeField] private float maxPitch = 1.2f;

        private PlayerAnimatorController playerAnimator;

        void Awake()
        {
            particleSystemComponent = GetComponent<ParticleSystem>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // Setup audio source
            audioSource.playOnAwake = false;
            audioSource.clip = particleSound;
            audioSource.loop = false;
            audioSource.volume = 1f;
            
            // Initially stop particles
            if (particleSystemComponent != null)
            {
                particleSystemComponent.Stop();
            }
            
            // Initialize particle array
            particles = new ParticleSystem.Particle[10];
        }
        
        void Start()
        {
            // Subscribe to game start event
            if (GameManager.Instance != null)
            {
                GameManager.Instance.onGameStarted.AddListener(OnGameStart);
            }

            playerAnimator = FindObjectOfType<PlayerAnimatorController>();
        }

        void OnDestroy()
        {
            // Clean up subscription
            if (GameManager.Instance != null)
            {
                GameManager.Instance.onGameStarted.RemoveListener(OnGameStart);
            }
        }
        
        void OnGameStart()
        {
            if (particleSystemComponent != null)
            {
                StartCoroutine(DelayedStart());
            }
        }

        private IEnumerator DelayedStart()
        {
            Debug.Log($"DelayedStart - Waiting {startDelay} seconds");
            yield return new WaitForSeconds(startDelay);
            
            particleSystemComponent.Play();
            hasStarted = true;

            // Trigger the turn through PlayerAnimatorController
            if (playerAnimator != null)
            {
                playerAnimator.TriggerTurn();
            }
        }

        void Update()
        {
            if (!hasStarted) return;
            
            if (particleSystemComponent != null && particleSystemComponent.isPlaying)
            {
                int numParticlesAlive = particleSystemComponent.GetParticles(particles);
                
                // Only play sound if we have MORE particles than before
                if (numParticlesAlive > previousParticleCount)
                {
                    PlayRandomPitchSound();
                }
                
                previousParticleCount = numParticlesAlive;
            }
            else
            {
                previousParticleCount = 0;
            }
        }
        
        private void PlayRandomPitchSound()
        {
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.pitch = Random.Range(minPitch, maxPitch);
                audioSource.PlayOneShot(audioSource.clip);
            }
        }
    }
}
using UnityEngine;

namespace RoofTops
{
    public class VisibilityController : MonoBehaviour
    {
        public float fadeDelay = 0.5f;  // Delay before starting to hide
        private MeshRenderer[] meshRenderers;
        private bool startedFade = false;
        private float fadeTimer = 0f;

        void Start()
        {
            // Get all mesh renderers in children
            meshRenderers = GetComponentsInChildren<MeshRenderer>();
            if (meshRenderers.Length == 0)
            {
                Debug.LogWarning("No MeshRenderers found in children of " + gameObject.name);
                return;
            }
        }

        void Update()
        {
            if (!startedFade && GameManager.Instance.HasGameStarted)
            {
                startedFade = true;
                fadeTimer = fadeDelay;
            }

            if (startedFade)
            {
                fadeTimer -= Time.deltaTime;
                if (fadeTimer <= 0)
                {
                    // Hide all child renderers
                    foreach (var renderer in meshRenderers)
                    {
                        renderer.enabled = false;
                    }
                }
            }
        }
    }
} 
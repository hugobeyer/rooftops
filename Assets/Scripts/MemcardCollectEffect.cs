using UnityEngine;

namespace RoofTops
{
    public class MemcardCollectEffect : MonoBehaviour
    {
        [Header("Effect Settings")]
        [Tooltip("Speed of upward movement")]
        public float floatSpeed = 2.0f;

        [Tooltip("Speed of rotation")]
        public float rotationSpeed = 180.0f;

        [Tooltip("How quickly the effect fades out")]
        public float fadeSpeed = 1.0f;

        [Tooltip("The scale animation curve")]
        public AnimationCurve scaleCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.2f, 1.2f),
            new Keyframe(0.3f, 1.0f),
            new Keyframe(1f, 0f)
        );

        // Private references
        private float lifetime = 0f;
        private float duration;
        private MeshRenderer meshRenderer;
        private Color originalColor;

        private void Start()
        {
            // Get components
            meshRenderer = GetComponent<MeshRenderer>();

            // Set duration from parent if available
            MemcardCollectible memcard = GetComponentInParent<MemcardCollectible>();
            if (memcard != null)
            {
                duration = memcard.effectDuration;
            }
            else
            {
                duration = 1.0f;
            }

            // Store original color
            if (meshRenderer != null && meshRenderer.material != null)
            {
                originalColor = meshRenderer.material.color;
            }

            // Start with zero scale
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            // Increment lifetime
            lifetime += Time.deltaTime;

            // Calculate progress (0-1)
            float progress = lifetime / duration;

            // Move upward
            transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);

            // Rotate
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            // Scale based on curve
            float scale = scaleCurve.Evaluate(progress);
            transform.localScale = Vector3.one * scale;

            // Fade out
            if (meshRenderer != null && meshRenderer.material != null)
            {
                Color color = originalColor;
                color.a = Mathf.Lerp(originalColor.a, 0f, progress * fadeSpeed);
                meshRenderer.material.color = color;
            }

            // Destroy when lifetime exceeds duration
            if (lifetime >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
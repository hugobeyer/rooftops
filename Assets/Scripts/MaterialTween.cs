using UnityEngine;
using DG.Tweening; // Ensure DOTween is imported

public class MaterialTween : MonoBehaviour
{
    [Header("Material Settings")]
    public Material targetMaterial;
    public string propertyName = "_Color"; // Example property, can be "_EmissionColor", "_Glossiness", etc.

    [Header("Tween Settings")]
    public Color startValue = Color.white;
    public Color endValue = Color.red;
    public float duration = 1f;
    public Ease easeType = Ease.InOutSine;
    public bool loop = false;

    private void Start()
    {
        if (targetMaterial == null)
        {
            Debug.LogError("Material not assigned!");
            return;
        }

        targetMaterial = GetComponent<Renderer>().material; // creates a runtime instance

        // Set initial value
        targetMaterial.SetColor(propertyName, startValue);

        // Tween the material property
        var tween = targetMaterial.DOColor(endValue, propertyName, duration)
                                  .SetEase(easeType);

        if (loop)
        {
            tween.SetLoops(-1, LoopType.Yoyo);
        }
    }
}
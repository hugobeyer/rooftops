using UnityEngine;
using System;

[Serializable]
public class RenderQueueSetting
{
    public GameObject targetObject;
    public int renderQueue = 2000; // Default geometry queue
}

public class RenderQueueManager : MonoBehaviour
{
    [Tooltip("Array of objects and their desired render queue values")]
    public RenderQueueSetting[] renderQueueSettings;

    [Header("Debug")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool applyOnValidate = true;

    void Start()
    {
        if (applyOnStart)
        {
            ApplyRenderQueues();
        }
    }

    private void OnValidate()
    {
        if (applyOnValidate)
        {
            ApplyRenderQueues();
        }
    }

    [ContextMenu("Apply Render Queues")]
    public void ApplyRenderQueues()
    {
        if (renderQueueSettings == null) return;

        foreach (var setting in renderQueueSettings)
        {
            if (setting.targetObject == null) continue;

            // Get all renderers (including children)
            Renderer[] renderers = setting.targetObject.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                // Use sharedMaterials to avoid leaks in edit mode
                Material[] materials = renderer.sharedMaterials;
                bool materialsModified = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && materials[i].renderQueue != setting.renderQueue)
                    {
                        // Create a new material instance only if we're in play mode
                        if (Application.isPlaying)
                        {
                            materials[i] = new Material(materials[i]);
                        }
                        materials[i].renderQueue = setting.renderQueue;
                        materialsModified = true;
                    }
                }

                // Only reassign materials if we actually modified them
                if (materialsModified)
                {
                    if (Application.isPlaying)
                    {
                        renderer.materials = materials;
                    }
                    else
                    {
                        renderer.sharedMaterials = materials;
                    }
                }
            }
        }
    }

    // Helper method to set render queue at runtime
    public void SetRenderQueue(GameObject target, int queue)
    {
        // Find existing setting or create new one
        RenderQueueSetting setting = Array.Find(renderQueueSettings, s => s.targetObject == target);
        
        if (setting != null)
        {
            setting.renderQueue = queue;
        }
        else
        {
            // Resize array and add new setting
            Array.Resize(ref renderQueueSettings, (renderQueueSettings?.Length ?? 0) + 1);
            renderQueueSettings[renderQueueSettings.Length - 1] = new RenderQueueSetting 
            { 
                targetObject = target, 
                renderQueue = queue 
            };
        }

        ApplyRenderQueues();
    }
} 
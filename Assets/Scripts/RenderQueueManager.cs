using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class RenderQueueSetting
{
    public GameObject targetObject;
    [Tooltip("Higher numbers will render on top")]
    public int renderQueue = 2000; // Default geometry queue
    [Tooltip("Apply to objects with this name pattern")]
    public string namePattern; // For matching pooled/instantiated objects
    [Tooltip("Higher numbers will override lower numbers for the same object")]
    public int priority = 0; // Higher priority settings override lower ones
}

public class RenderQueueManager : MonoBehaviour
{
    [Tooltip("Array of objects and their desired render queue values")]
    public RenderQueueSetting[] renderQueueSettings;

    [Header("Debug")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool applyOnValidate = true;

    // Cache for pooled/instantiated objects, storing both queue and priority
    private Dictionary<string, (int queue, int priority)> namePatternToQueue = new Dictionary<string, (int queue, int priority)>();

    void Awake()
    {
        // Build the name pattern cache
        if (renderQueueSettings != null)
        {
            foreach (var setting in renderQueueSettings)
            {
                if (!string.IsNullOrEmpty(setting.namePattern))
                {
                    // If pattern exists, only update if new priority is higher
                    if (namePatternToQueue.TryGetValue(setting.namePattern, out var existing))
                    {
                        if (setting.priority > existing.priority)
                        {
                            namePatternToQueue[setting.namePattern] = (setting.renderQueue, setting.priority);
                        }
                    }
                    else
                    {
                        namePatternToQueue[setting.namePattern] = (setting.renderQueue, setting.priority);
                    }
                }
            }
        }
    }

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

    void OnEnable()
    {
        ApplyRenderQueues();
    }

    // Call this when new objects are spawned/pooled
    public void ApplyToNewObject(GameObject obj)
    {
        if (obj == null) return;

        int highestPriority = int.MinValue;
        int selectedQueue = -1;

        // Find the highest priority matching pattern
        foreach (var pattern in namePatternToQueue)
        {
            if (obj.name.Contains(pattern.Key) && pattern.Value.priority > highestPriority)
            {
                highestPriority = pattern.Value.priority;
                selectedQueue = pattern.Value.queue;
            }
        }

        if (selectedQueue != -1)
        {
            ApplyRenderQueueToObject(obj, selectedQueue);
        }
    }

    private void ApplyRenderQueueToObject(GameObject obj, int queue)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            bool materialsModified = false;
            Material[] materials = Application.isPlaying ? renderer.materials : renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null)
                {
                    if (Application.isPlaying && materials[i].renderQueue != queue)
                    {
                        materials[i] = new Material(materials[i]);
                        materialsModified = true;
                    }

                    if (materials[i].renderQueue != queue)
                    {
                        materials[i].renderQueue = queue;
                        materialsModified = true;

                        #if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                            EditorUtility.SetDirty(materials[i]);
                        }
                        #endif
                    }
                }
            }

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

    [ContextMenu("Apply Render Queues")]
    public void ApplyRenderQueues()
    {
        if (renderQueueSettings == null) return;

        // Sort settings by priority
        var sortedSettings = new List<RenderQueueSetting>(renderQueueSettings);
        sortedSettings.Sort((a, b) => a.priority.CompareTo(b.priority));

        // Dictionary to track highest priority queue for each object
        var objectQueues = new Dictionary<GameObject, (int queue, int priority)>();

        // Process all settings in priority order
        foreach (var setting in sortedSettings)
        {
            // Handle direct object references
            if (setting.targetObject != null)
            {
                if (objectQueues.TryGetValue(setting.targetObject, out var existing))
                {
                    if (setting.priority > existing.priority)
                    {
                        objectQueues[setting.targetObject] = (setting.renderQueue, setting.priority);
                    }
                }
                else
                {
                    objectQueues[setting.targetObject] = (setting.renderQueue, setting.priority);
                }
            }

            // Handle name patterns
            if (!string.IsNullOrEmpty(setting.namePattern))
            {
                var objects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (var obj in objects)
                {
                    if (obj.name.Contains(setting.namePattern))
                    {
                        if (objectQueues.TryGetValue(obj, out var existing))
                        {
                            if (setting.priority > existing.priority)
                            {
                                objectQueues[obj] = (setting.renderQueue, setting.priority);
                            }
                        }
                        else
                        {
                            objectQueues[obj] = (setting.renderQueue, setting.priority);
                        }
                    }
                }
            }
        }

        // Apply the final queue values
        foreach (var kvp in objectQueues)
        {
            ApplyRenderQueueToObject(kvp.Key, kvp.Value.queue);
        }

        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
        #endif
    }

    // Helper method to set render queue at runtime
    public void SetRenderQueue(GameObject target, int queue, int priority = 0)
    {
        // Find existing setting or create new one
        RenderQueueSetting setting = Array.Find(renderQueueSettings, s => s.targetObject == target);
        
        if (setting != null)
        {
            if (priority >= setting.priority)
            {
                setting.renderQueue = queue;
                setting.priority = priority;
                ApplyRenderQueueToObject(target, queue);
            }
        }
        else
        {
            // Resize array and add new setting
            Array.Resize(ref renderQueueSettings, (renderQueueSettings?.Length ?? 0) + 1);
            renderQueueSettings[renderQueueSettings.Length - 1] = new RenderQueueSetting 
            { 
                targetObject = target, 
                renderQueue = queue,
                priority = priority
            };
            ApplyRenderQueueToObject(target, queue);
        }
    }

    private void UpdateAllRenderers()
    {
        Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        
        // ... existing code ...
    }
} 
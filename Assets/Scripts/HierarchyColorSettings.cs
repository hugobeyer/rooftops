using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class HierarchyColorRule
{
    public string objectName;
    public Color color = Color.white;
    [Tooltip("If true, will color objects that contain this name anywhere in their name")]
    public bool useContains = true; // Changed to true by default since we want contains behavior

    public HierarchyColorRule(string name, Color col, bool contains = true)
    {
        objectName = name;
        color = col;
        useContains = contains;
    }
}

[CreateAssetMenu(fileName = "HierarchyColorSettings", menuName = "Custom/Hierarchy Color Settings")]
public class HierarchyColorSettings : ScriptableObject
{
    public List<HierarchyColorRule> colorRules = new List<HierarchyColorRule>();

    void Reset()
    {
        // This will be called when the asset is first created
        colorRules = new List<HierarchyColorRule>
        {
            // System objects (Light blue)
            new HierarchyColorRule("System", new Color(0.4f, 0.7f, 1f, 1f)),
            new HierarchyColorRule("Manager", new Color(0.4f, 0.7f, 1f, 1f)),
            
            // UI elements (Orange)
            new HierarchyColorRule("UI", new Color(1f, 0.6f, 0f, 1f)),
            new HierarchyColorRule("Menu", new Color(1f, 0.6f, 0f, 1f)),
            new HierarchyColorRule("Panel", new Color(1f, 0.6f, 0f, 1f)),
            
            // Camera related (Purple)
            new HierarchyColorRule("Camera", new Color(0.6f, 0.4f, 1f, 1f)),
            new HierarchyColorRule("Cam", new Color(0.6f, 0.4f, 1f, 1f)),
            
            // Background elements (Dark blue)
            new HierarchyColorRule("Background", new Color(0.2f, 0.3f, 0.7f, 1f)),
            
            // Player (Green - keeping the default)
            new HierarchyColorRule("Player", new Color(0.2f, 0.6f, 0.1f, 1f)),
            
            // Gameplay elements (Light red)
            new HierarchyColorRule("Gameplay", new Color(1f, 0.4f, 0.4f, 1f)),
            new HierarchyColorRule("Drone", new Color(1f, 0.4f, 0.4f, 1f)),
            
            // Audio (Yellow)
            new HierarchyColorRule("Audio", new Color(1f, 0.92f, 0.016f, 1f)),
            new HierarchyColorRule("Sound", new Color(1f, 0.92f, 0.016f, 1f)),
            
            // Visual effects (Cyan)
            new HierarchyColorRule("Particle", new Color(0f, 0.8f, 0.8f, 1f)),
            new HierarchyColorRule("VFX", new Color(0f, 0.8f, 0.8f, 1f)),
            
            // Editor only objects (Gray)
            new HierarchyColorRule("EDITOR", new Color(0.5f, 0.5f, 0.5f, 1f))
        };
    }
    
    public bool TryGetColor(string objectName, out Color color)
    {
        foreach (var rule in colorRules)
        {
            if (rule.useContains)
            {
                if (objectName.IndexOf(rule.objectName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    color = rule.color;
                    return true;
                }
            }
            else if (objectName == rule.objectName)
            {
                color = rule.color;
                return true;
            }
        }
        
        color = Color.white;
        return false;
    }
} 
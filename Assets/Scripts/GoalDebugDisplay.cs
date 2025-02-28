using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RoofTops;

public class GoalDebugDisplay : MonoBehaviour
{
    public GameObject textObject;
    private Text uiText;
    private TextMeshProUGUI tmpText;
    private bool usingTMP = false;
    
    void Start()
    {
        if (textObject != null)
        {
            // Try to get regular Text component
            uiText = textObject.GetComponent<Text>();
            
            // If no Text component, try TextMeshProUGUI
            if (uiText == null)
            {
                tmpText = textObject.GetComponent<TextMeshProUGUI>();
                if (tmpText != null)
                {
                    usingTMP = true;
                }
                else
                {
                    Debug.LogError("The assigned GameObject does not have a Text or TextMeshProUGUI component!");
                }
            }
        }
    }
    
    void Update()
    {
        if ((uiText == null && tmpText == null) || GoalAchievementManager.Instance == null)
            return;
            
        string info = "<size=50%><b><color=#FFD700>GOAL SYSTEM DEBUG</color></b></size>\n";
        info += "<color=#888888>━━━━━━━━━━━━━━━━━━━━━━━━</color>\n";
        
        // Simple header
        info += "<b><color=#AADDFF>Category | Current | Goal | Prog | Idx</color></b>\n";
        
        // Get all categories
        var categories = new List<string> { "Distance", "Tridots", "Memcard" };
        
        foreach (var categoryName in categories)
        {
            var category = GoalAchievementManager.Instance.GetGoalCategory(categoryName);
            if (category != null)
            {
                // Get current value from the category's function
                object currentValue = category.getCurrentValueFunc?.Invoke();
                
                // Format the current value and goal value
                string currentValueStr = currentValue?.ToString() ?? "N/A";
                string goalValueStr = category.currentGoalValue?.ToString() ?? "N/A";
                
                // Add category name with color
                string categoryColor = GetCategoryColor(categoryName);
                info += $"<b><color={categoryColor}>{category.displayName}</color></b> | ";
                
                // Add current value (green if close to goal)
                bool isCloseToGoal = IsCloseToGoal(currentValue, category.currentGoalValue);
                string currentValueColor = isCloseToGoal ? "#ADFF2F" : "#FFFFFF";
                info += $"<color={currentValueColor}>{currentValueStr}</color> | ";
                
                // Add goal value
                info += $"<color=#FFCC00>{goalValueStr}</color> | ";
                
                // Add progress index
                info += $"<color=#AAAAAA>{category.currentProgressIndex}</color> | ";
                
                // Add goal index
                info += $"<color=#AAAAAA>{category.goalIndex}</color>\n";
            }
        }
        
        // Set text to appropriate component
        if (usingTMP)
        {
            tmpText.text = info;
        }
        else
        {
            // Strip rich text tags for regular Text component
            uiText.text = StripRichTextTags(info);
        }
    }
    
    private string GetCategoryColor(string categoryName)
    {
        switch (categoryName)
        {
            case "Distance": return "#00FFFF"; // Cyan
            case "Tridots": return "#FF6347";  // Tomato
            case "Memcard": return "#9370DB";  // Medium Purple
            default: return "#FFFFFF";         // White
        }
    }
    
    private bool IsCloseToGoal(object current, object goal)
    {
        if (current == null || goal == null) return false;
        
        if (current is float currentFloat && goal is float goalFloat)
        {
            return currentFloat >= goalFloat * 0.8f;
        }
        else if (current is int currentInt && goal is int goalInt)
        {
            return currentInt >= goalInt * 0.8f;
        }
        
        return false;
    }
    
    private string StripRichTextTags(string input)
    {
        // Simple method to strip rich text tags for regular Text component
        return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty);
    }
} 
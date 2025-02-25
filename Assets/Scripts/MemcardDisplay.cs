using UnityEngine;
using RoofTops;

public class MemcardDisplay : MonoBehaviour
{
    // Assign a TextMesh component here in the Inspector.
    public TextMesh memcardText;
    
    // Optional: custom text format (e.g. "Memcards: {0}")
    public string textFormat = "Memcards: {0}";
    
    // Optional: color settings
    public Color textColor = Color.yellow;
    
    // Reference to game manager
    private GameManager gameManager;
    
    // Local count tracker (in case we can't find GameManager)
    private int memcardCount = 0;

    void Start()
    {
        // Find the GameManager
        gameManager = FindObjectOfType<GameManager>();
        
        // Set the text color if we have a TextMesh
        if (memcardText != null)
        {
            memcardText.color = textColor;
        }
        else
        {
            Debug.LogWarning("MemcardDisplay: TextMesh component not assigned.");
        }
        
        // Initialize the display
        UpdateDisplay();
    }

    void Update()
    {
        // Update the display each frame
        UpdateDisplay();
    }
    
    void UpdateDisplay()
    {
        if (memcardText == null)
            return;
            
        // Get the count from GameManager or use local count
        int currentCount = 0;
        
        if (gameManager != null && gameManager.gameData != null)
        {
            currentCount = gameManager.gameData.lastRunMemcardsCollected;
        }
        else
        {
            currentCount = memcardCount;
        }
        
        // Format and display the text
        memcardText.text = string.Format(textFormat, currentCount);
    }
    
    // Public method to update the count directly (can be called from MemcardCollectible)
    public void UpdateCount(int newCount)
    {
        memcardCount = newCount;
        UpdateDisplay();
    }
    
    // Public method to increment the count (can be called from MemcardCollectible)
    public void IncrementCount(int amount = 1)
    {
        memcardCount += amount;
        UpdateDisplay();
    }
} 
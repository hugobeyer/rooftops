using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "ScriptableObjects/GameDataObject", order = 1)]
public class GameDataObject : ScriptableObject
{
    public float bestDistance;
    public float lastRunDistance;
    
    // Add tridots tracking
    public int totalTridotCollected;      // All-time total
    public int lastRunTridotCollected;    // Last run total
    public int bestRunTridotCollected;    // Best run total
    
    // Add memcard tracking
    public int totalMemcardsCollected;   // All-time total memcards
    public int lastRunMemcardsCollected; // Memcards collected in last run
    public int bestRunMemcardsCollected; // Best run memcard count
    
    // Track tutorial messages shown
    public bool hasShownDashInfo = false; // Whether the dash info message has been shown
} 
using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "ScriptableObjects/GameDataObject", order = 1)]
public class GameDataObject : ScriptableObject
{
    public float bestDistance;
    public float lastRunDistance;
    
    // Add bonus tracking
    public int totalBonusCollected;      // All-time total
    public int lastRunBonusCollected;    // Last run total
    public int bestRunBonusCollected;    // Best run total
} 
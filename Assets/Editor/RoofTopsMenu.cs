using UnityEngine;
using UnityEditor;
using RoofTops;
public class RoofTopsMenu : Editor
{
    [MenuItem("RoofTops/Reset All Game Data")]
    private static void ResetAllGameData()
    {
        // Reset data in all GameDataObject assets
        string[] guids = AssetDatabase.FindAssets("t:GameDataObject");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameDataObject gameData = AssetDatabase.LoadAssetAtPath<GameDataObject>(path);
            if (gameData != null)
            {
                gameData.totalTridotCollected = 0;
                gameData.lastRunTridotCollected = 0;
                gameData.bestRunTridotCollected = 0;
                gameData.bestDistance = 0;
                gameData.lastRunDistance = 0;
                gameData.lastRunMemcardsCollected = 0;
                gameData.totalMemcardsCollected = 0;
                gameData.bestRunMemcardsCollected = 0;
                gameData.hasShownDashInfo = false;
                EditorUtility.SetDirty(gameData);
                Debug.Log($"Reset data in {path}");
            }
        }
        AssetDatabase.SaveAssets();
        // Reset GameManager's runtime data
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null && gameManager.gameData != null)
        {
            gameManager.ClearGameData();
            Debug.Log("Reset runtime game data using GameManager.ClearGameData()");
        }
        // Reset UI displays if they exist
        if (TridotsTextDisplay.Instance != null)
        {
            TridotsTextDisplay.Instance.ResetTotal();
            Debug.Log("Reset tridots display UI");
        }
        // Clear PlayerPrefs
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("Cleared all PlayerPrefs data");
        Debug.Log("All game data has been reset successfully.");
    }
}
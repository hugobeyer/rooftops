using UnityEngine;

public class DeathMessageDisplay : MonoBehaviour
{
    public static DeathMessageDisplay Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // Keep the object across scenes
        }
        else
        {
            Destroy(gameObject);
        }
        
        gameObject.SetActive(false);  // Hide on start
    }
    
    public void ShowMessage()
    {
        if (this == null) return;  // Safety check
        gameObject.SetActive(true);
    }
    
    public void HideMessage()
    {
        if (this == null) return;  // Safety check
        gameObject.SetActive(false);
    }
} 
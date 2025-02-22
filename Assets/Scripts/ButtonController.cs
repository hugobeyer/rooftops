using UnityEngine;
using UnityEngine.UI; // Required for UI components

public class ButtonController : MonoBehaviour
{
    private Button button; // Reference to the UI Button component

    void Start()
    {
        // Get the Button component attached to this GameObject
        button = GetComponent<Button>();
        
        // Add a listener for the click event
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }

    // This function will be called when the button is clicked
    public void OnButtonClick()
    {
        Debug.Log("Button was clicked!");
        // Add your button click logic here
    }

    void OnDestroy()
    {
        // Clean up by removing the listener when the object is destroyed
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
} 
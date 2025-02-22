using UnityEngine;
using UnityEngine.EventSystems; // Required for UI event interfaces

public class PanelController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rectTransform;
    private bool isHolding = false;

    void Start()
    {
        Debug.Log("Panel Start - Is Canvas enabled: " + GetComponentInParent<Canvas>().enabled);
        Debug.Log("Panel RectTransform size: " + GetComponent<RectTransform>().rect.size);
        rectTransform = GetComponent<RectTransform>();

        // Add this to verify InputManager exists
        Debug.Log("InputManager exists: " + InputManager.Exists());
    }

    void Update()
    {
        // While holding, keep sending the held state
        if (isHolding)
        {
            InputManager.Instance.SetJumpHeld();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Panel CLICKED");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Panel ENTER");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Panel EXIT");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("Panel DOWN - Position: " + eventData.position);
        if (InputManager.Instance == null)
        {
            Debug.LogError("InputManager.Instance is null!");
            return;
        }
        isHolding = true;
        InputManager.Instance.SetJumpPressed();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("Panel UP");
        isHolding = false;
        InputManager.Instance.SetJumpReleased();
    }
} 
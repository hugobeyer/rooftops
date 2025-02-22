using UnityEngine;
using UnityEngine.EventSystems; // Required for UI event interfaces

public class PanelController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rectTransform;
    private bool isHolding = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
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
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
    }

    public void OnPointerExit(PointerEventData eventData)
    {
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (InputManager.Instance == null)
        {
            return;
        }
        isHolding = true;
        InputManager.Instance.SetJumpPressed();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        InputManager.Instance.SetJumpReleased();
    }
} 
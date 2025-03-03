using UnityEngine;
using UnityEngine.EventSystems; // Required for UI event interfaces

public class PanelController : MonoBehaviour//, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rectTransform;
    //private bool isHolding = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    //void Update()
    //{
    //    //// While holding, keep sending the held state
    //    //if (isHolding)
    //    //{
    //    //    InputActionManager.Instance.SetJumpHeld();
    //    //}
    //}

    //public void OnPointerClick(PointerEventData eventData)
    //{
    //}

    //public void OnPointerEnter(PointerEventData eventData)
    //{
    //}

    //public void OnPointerExit(PointerEventData eventData)
    //{
    //}

    //public void OnPointerDown(PointerEventData eventData)
    //{
    //    //if (InputActionManager.Instance == null)
    //    //{
    //    //    return;
    //    //}
    //    //isHolding = true;
    //    //InputActionManager.Instance.SetJumpPressed();
    //}

    //public void OnPointerUp(PointerEventData eventData)
    //{
    //    //isHolding = false;
    //    //InputActionManager.Instance.SetJumpReleased();
    //}
} 
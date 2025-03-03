using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Required for UI event interfaces

public class PanelController : MonoBehaviour//, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    //private RectTransform rectTransform;
    //private bool isHolding = false;
    Button button;

    void Start()
    {
        //button = GetComponent<Button>();
        //button.onClick.AddListener(() => );
        //rectTransform = GetComponent<RectTransform>();
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
    //    //InputActionManager.Instance.c();
    //}

    //public void OnPointerUp(PointerEventData eventData)
    //{
    //    //isHolding = false;
    //    //InputActionManager.Instance.SetJumpReleased();
    //}
} 
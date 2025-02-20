using UnityEngine;
using UnityEngine.UI;

namespace RoofTops
{
    public class PauseButton : MonoBehaviour
    {
        private Button button;
        private CanvasGroup canvasGroup;

        void Start()
        {
            button = GetComponent<Button>();
            canvasGroup = GetComponent<CanvasGroup>();
            
            // Hide until game starts
            if (canvasGroup != null)
                canvasGroup.alpha = 0;
            
            if (button != null)
                button.onClick.AddListener(OnPauseClicked);
        }

        void Update()
        {
            // Show button when game starts
            if (GameManager.Instance.HasGameStarted && canvasGroup != null)
            {
                canvasGroup.alpha = 1;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        void OnPauseClicked()
        {
            GameManager.Instance.TogglePause();
        }

        void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnPauseClicked);
            }
        }
    }
} 
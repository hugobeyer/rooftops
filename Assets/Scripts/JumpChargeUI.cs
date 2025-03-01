using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RoofTops;

public class JumpChargeUI : MonoBehaviour
{
    [Header("UI References")]
    public Image chargeBar;
    public TextMeshProUGUI chargeText;

    [Header("Visual Settings")]
    public Color minChargeColor = Color.white;
    public Color maxChargeColor = Color.yellow;
    public bool showPercentage = true;

    private PlayerController playerController;
    private CanvasGroup canvasGroup;

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Hide initially
        canvasGroup.alpha = 0;
    }

    void Update()
    {
        if (playerController == null) return;

        float chargeProgress = (playerController.currentChargedJumpForce - playerController.jumpForce) /
                             (playerController.maxJumpForce - playerController.jumpForce);

        if (playerController.isChargingJump)
        {
            // Show and update UI
            canvasGroup.alpha = 1;

            if (chargeBar != null)
            {
                chargeBar.fillAmount = chargeProgress;
                chargeBar.color = Color.Lerp(minChargeColor, maxChargeColor, chargeProgress);
            }

            if (chargeText != null && showPercentage)
            {
                chargeText.text = $"{chargeProgress * 100:0}%";
            }
        }
        else
        {
            // Hide UI
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0, Time.deltaTime * 2);
        }
    }
}
using UnityEngine;
using TMPro;

public class BonusTextDisplay : MonoBehaviour
{
    public TMP_Text bonusText;  // Assign in inspector
    private static BonusTextDisplay instance;

    void Awake()
    {
        instance = this;
        // Auto-reference if not assigned
        if (bonusText == null)
        {
            bonusText = GetComponent<TMP_Text>();
        }
    }

    public static void ShowBonus(float amount)
    {
        if (instance == null)
        {
            Debug.LogWarning("No BonusTextDisplay instance found!");
            return;
        }

        if (instance.bonusText == null)
        {
            Debug.LogWarning("No TMP_Text component assigned to BonusTextDisplay!");
            return;
        }

        instance.bonusText.text = "+" + amount.ToString("F0");
        instance.StopAllCoroutines();
        instance.StartCoroutine(instance.FadeOutText());
    }

    private System.Collections.IEnumerator FadeOutText()
    {
        float duration = 1f;
        float elapsed = 0f;
        Color startColor = bonusText.color;

        // Make sure text is visible at start
        bonusText.color = new Color(startColor.r, startColor.g, startColor.b, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed/duration);
            bonusText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        bonusText.text = "";
        bonusText.color = startColor;
    }
} 
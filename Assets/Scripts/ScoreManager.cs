using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public TMP_Text scoreText;
    public float score = 0;
    public float scoreMultiplier = 1f;

    public void AddScore(float amount)
    {
        score += amount * scoreMultiplier;
        if(scoreText != null)
        {
            scoreText.text = score.ToString("F0");
        }
    }
} 
using UnityEngine;

public class BonusSpot : MonoBehaviour
{
    [Header("Bonus Settings")]
    public int scoreValue = 100;
    public float rotationSpeed = 90f;

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            // Update display counter for this run
            DisplayBonusStats.AddBonus();
            
            // Save to persistent data
            GameManager.Instance.gameData.totalBonusCollected++;
            
            ScoreManager scoreManager = FindAnyObjectByType<ScoreManager>();
            if(scoreManager != null)
            {
                scoreManager.AddScore(scoreValue);
                BonusTextDisplay.ShowBonus(scoreValue);
            }
            
            Destroy(gameObject);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }
} 
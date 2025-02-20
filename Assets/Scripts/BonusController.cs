using UnityEngine;
using RoofTops;

public class BonusController : MonoBehaviour
{
    public static BonusController Instance { get; private set; }

    [Header("Bonus Settings")]
    public int bonusValue = 1;
    public GameObject bonusEffectPrefab;  // Optional visual effect

    void Awake()
    {
        Instance = this;
    }

    public void CollectBonus()
    {
        // Update GameManager's data
        GameManager.Instance.AddBonus(bonusValue);

        // Play effect if assigned
        if (bonusEffectPrefab != null)
        {
            Instantiate(bonusEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CollectBonus();
            gameObject.SetActive(false);  // Hide the bonus
        }
    }
} 
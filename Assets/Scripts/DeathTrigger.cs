using UnityEngine;
using RoofTops;

public class DeathTrigger : MonoBehaviour
{
    public PlayerController player;

    void Update()
    {
        // Just follow player's Y position
        Vector3 newPos = transform.localPosition;
        newPos.y = player.transform.position.y;
        transform.localPosition = newPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        player.HandleDeath();
    }
} 
using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    public float lifetime = 5f; // Time until destruction

    void Start()
    {
        // Invoke the DestroyObject method after 'lifetime' seconds
        Invoke("DestroyObject", lifetime);
    }

    void DestroyObject()
    {
        // Destroy the GameObject this script is attached to
        Destroy(gameObject);
    }
} 
using UnityEngine;

public class DeathUICamera : MonoBehaviour
{
    void LateUpdate()
    {
        // Match main camera's FOV every frame
        if (Camera.main != null)
        {
            GetComponent<Camera>().fieldOfView = Camera.main.fieldOfView;
        }
    }
} 
using UnityEngine;

public class ModulesDistanceDisplay : MonoBehaviour
{
    // The transform that is being translated by the ModulePool script.
    public Transform moduleMovement;

    // Additional height above any collider
    public float additionalHeight = 0.125f;

    // Layer mask for modules
    [Tooltip("Set this to the layer your modules are on")]
    public LayerMask moduleLayer;

    private float startZ;
    private Vector3 markerPosition;

    void Start()
    {
        if(moduleMovement != null)
        {
            startZ = moduleMovement.position.z;
            markerPosition = transform.position;
        }
        else
        {
            Debug.LogWarning("ModulesDistanceDisplay: moduleMovement transform not assigned.");
        }
    }

    void Update()
    {
        if(moduleMovement == null) return;

        // Calculate and update distance
        float traveledDistance = Mathf.Abs(moduleMovement.position.z - startZ);
        DistanceTextDisplay.UpdateDistance(traveledDistance);

        // Find all modules (they are children of moduleMovement)
        float highestPoint = float.MinValue;
        foreach(Transform child in moduleMovement)
        {
            // Check if this module is near our Z position
            if (Mathf.Abs(child.position.z - transform.position.z) < 1f)
            {
                BoxCollider collider = child.GetComponent<BoxCollider>();
                if(collider != null)
                {
                    float topPoint = child.position.y + 
                                   (collider.center.y + collider.size.y/2) * child.lossyScale.y;
                    highestPoint = Mathf.Max(highestPoint, topPoint);
                }
            }
        }

        // Update position
        if(highestPoint > float.MinValue)
        {
            transform.position = new Vector3(
                transform.position.x,
                highestPoint + additionalHeight,
                transform.position.z
            );
        }
    }
}
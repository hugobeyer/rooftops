using UnityEngine;

public class Rotator : MonoBehaviour
{
    public enum Axis { X, Y, Z, Custom }
    
    [Header("Rotation Settings")]
    [Tooltip("Select the rotation axis.")]
    public Axis rotationAxis = Axis.Y;
    
    [Tooltip("If using Custom axis, set the axis vector.")]
    public Vector3 customAxis = Vector3.up;
    
    [Tooltip("Rotation speed in degrees per second.")]
    public float rotationSpeed = 90f;

    void Update()
    {
        // Determine the axis vector to rotate along.
        Vector3 axisVector;
        switch (rotationAxis)
        {
            case Axis.X:
                axisVector = Vector3.right;
                break;
            case Axis.Y:
                axisVector = Vector3.up;
                break;
            case Axis.Z:
                axisVector = Vector3.forward;
                break;
            case Axis.Custom:
                axisVector = customAxis.normalized;
                break;
            default:
                axisVector = Vector3.up;
                break;
        }
        
        // Rotate the GameObject about the chosen axis, in local space.
        transform.Rotate(axisVector, rotationSpeed * Time.deltaTime, Space.Self);
    }
} 
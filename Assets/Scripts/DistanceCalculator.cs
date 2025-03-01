using UnityEngine;
using System.Collections;
using RoofTops;

public class DistanceCalculator : MonoBehaviour
{
    // We now use the ModulePool's currentMoveSpeed to accumulate distance.
    private float accumulatedDistance = 0f;
    public float CurrentDistance { get; private set; }

    void Start()
    {
        // No need to assign moduleMovementTransform here.
        // Make sure ModulePool.Instance is available before gameplay starts.
    }

    void Update()
    {
        // Instead of relying on transform changes, accumulate distance using the move speed.
        if (ModulePool.Instance != null)
        {
            float movementDelta = ModulePool.Instance.currentMoveSpeed * Time.deltaTime;
            accumulatedDistance += movementDelta;
            CurrentDistance = accumulatedDistance;
        }
    }

    // No longer needed because we now rely on ModulePool's currentMoveSpeed.
}
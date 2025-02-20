using UnityEngine;
using RoofTops;

public class SpeedManager : MonoBehaviour
{
    public static SpeedManager Instance { get; private set; }

    [Header("Speed Settings")]
    public float baseSpeed = 6f;
    public float speedIncreaseRate = 0.1f;
    public float currentSpeed { get; private set; }

    private ModulePool modulePool;
    private VistaPool vistaPool;
    private bool isMoving = true;

    void Awake()
    {
        Instance = this;
        currentSpeed = baseSpeed;
    }

    void Start()
    {
        modulePool = ModulePool.Instance;
        vistaPool = VistaPool.Instance;
    }

    void Update()
    {
        if (GameManager.Instance.IsPaused) return;

        if (isMoving)
        {
            currentSpeed += speedIncreaseRate * Time.unscaledDeltaTime;
            
            // Update both pools with new speed
            if (modulePool != null) modulePool.currentMoveSpeed = currentSpeed;
            if (vistaPool != null) vistaPool.currentMoveSpeed = currentSpeed;
        }
    }

    public void SetMovement(bool moving)
    {
        isMoving = moving;
        if (moving)
        {
            currentSpeed = baseSpeed;
            modulePool?.SetMovement(true);
            vistaPool?.SetMovement(true);
        }
        else
        {
            modulePool?.SetMovement(false);
            vistaPool?.SetMovement(false);
        }
    }

    public void ResetSpeed()
    {
        currentSpeed = baseSpeed;
        modulePool?.ResetSpeed();
        vistaPool?.ResetSpeed();
    }
} 
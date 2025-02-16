using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ShadowCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera mainCamera;
    [Range(0.1f, 1f)] public float resolutionScale = 0.5f;
    [Range(0f, 1f)] public float shadowIntensity = 0.5f;

    [Header("Layer Settings")]
    public LayerMask shadowReceivers = 1 << 8; // Default to layer 8

    [Header("Camera References")]
    public Camera shadowCamera; // Assign this to the same GameObject's camera

    private Camera shadowCam;
    private RenderTexture shadowRT;

    void Start()
    {
        shadowCam = GetComponent<Camera>();
        
        // Camera setup
        shadowCam.CopyFrom(mainCamera);
        shadowCam.clearFlags = CameraClearFlags.SolidColor;
        shadowCam.backgroundColor = new Color(0, 0, 0, 0); // Transparent
        shadowCam.cullingMask = shadowReceivers;
        shadowCam.depth = mainCamera.depth + 1;
        
        // Create render texture
        shadowRT = new RenderTexture(
            (int)(Screen.width * resolutionScale), 
            (int)(Screen.height * resolutionScale), 
            24, // Depth buffer for URP
            RenderTextureFormat.DefaultHDR // URP-compatible format
        );
        shadowRT.name = "ShadowRT";
        shadowCamera.targetTexture = shadowRT;
    }

    void LateUpdate()
    {
        // Exact copy of main camera transform
        transform.position = mainCamera.transform.position;
        transform.rotation = mainCamera.transform.rotation;
        
        // Match all camera properties
        shadowCam.fieldOfView = mainCamera.fieldOfView;
        shadowCam.orthographic = mainCamera.orthographic;
        shadowCam.orthographicSize = mainCamera.orthographicSize;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (shadowRT != null)
        {
            // Blend the shadow texture with the main camera's view
            Graphics.Blit(shadowRT, dest, new Vector2(1, 1), new Vector2(0, 0));
        }
    }
} 
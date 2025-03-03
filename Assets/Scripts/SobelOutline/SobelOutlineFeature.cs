using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SobelOutlineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class SobelOutlineSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Material sobelMaterial = null;
        
        [Header("Outline Settings")]
        public Color outlineColor = Color.red;
        [Range(0.1f, 100.0f)]
        public float outlineThickness = 5.0f;
        [Range(0.0f, 100.0f)]
        public float outlineThreshold = 0.01f;
        [Range(0.0f, 100.0f)]
        public float depthSensitivity = 10.0f;
        [Range(0.0f, 100.0f)]
        public float colorSensitivity = 10.0f;
        public bool debugMode = false;
    }

    public SobelOutlineSettings settings = new SobelOutlineSettings();
    private SobelOutlinePass sobelPass;

    public override void Create()
    {
        sobelPass = new SobelOutlinePass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.sobelMaterial == null)
        {
            Debug.LogWarning("Sobel Outline material is null. Skipping pass.");
            return;
        }

        // Update material properties from settings
        settings.sobelMaterial.SetColor("_OutlineColor", settings.outlineColor);
        settings.sobelMaterial.SetFloat("_OutlineThickness", settings.outlineThickness);
        settings.sobelMaterial.SetFloat("_OutlineThreshold", settings.outlineThreshold);
        settings.sobelMaterial.SetFloat("_DepthSensitivity", settings.depthSensitivity);
        settings.sobelMaterial.SetFloat("_ColorSensitivity", settings.colorSensitivity);
        settings.sobelMaterial.SetFloat("_DebugMode", settings.debugMode ? 1.0f : 0.0f);

        sobelPass.SetupPass(renderer);
        renderer.EnqueuePass(sobelPass);
    }

    protected override void Dispose(bool disposing)
    {
        sobelPass.Dispose();
        base.Dispose(disposing);
    }

    class SobelOutlinePass : ScriptableRenderPass
    {
        private RTHandle cameraColorTarget;
        private RTHandle tempRenderTarget;
        private Material sobelMaterial;
        private SobelOutlineSettings settings;
        private string profilerTag = "Sobel Outline Effect";
        private ScriptableRenderer renderer;

        public SobelOutlinePass(SobelOutlineSettings settings)
        {
            this.settings = settings;
            this.sobelMaterial = settings.sobelMaterial;
            renderPassEvent = settings.renderPassEvent;
        }

        public void SetupPass(ScriptableRenderer renderer)
        {
            this.renderer = renderer;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            
            // Ensure we have a temporary render target
            RenderingUtils.ReAllocateIfNeeded(ref tempRenderTarget, descriptor, name: "_TempSobelTexture");
            
            // Get the camera color target
            cameraColorTarget = renderer.cameraColorTargetHandle;
            
            // Configure targets
            ConfigureTarget(cameraColorTarget);
            ConfigureClear(ClearFlag.None, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (sobelMaterial == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            
            // Log when the pass is executed
            cmd.Clear();
            cmd.BeginSample(profilerTag);
            
            // Update material properties from settings
            sobelMaterial.SetColor("_OutlineColor", settings.outlineColor);
            sobelMaterial.SetFloat("_OutlineThickness", settings.outlineThickness);
            sobelMaterial.SetFloat("_OutlineThreshold", settings.outlineThreshold);
            sobelMaterial.SetFloat("_DepthSensitivity", settings.depthSensitivity);
            sobelMaterial.SetFloat("_ColorSensitivity", settings.colorSensitivity);
            sobelMaterial.SetFloat("_DebugMode", settings.debugMode ? 1.0f : 0.0f);
            
            // Set the camera color texture
            cmd.SetGlobalTexture("_CameraColorTexture", cameraColorTarget);
            
            // Blit from camera target to temp with sobel material
            Blitter.BlitCameraTexture(cmd, cameraColorTarget, tempRenderTarget, sobelMaterial, 0);
            
            // Blit from temp back to camera target
            Blitter.BlitCameraTexture(cmd, tempRenderTarget, cameraColorTarget);
            
            cmd.EndSample(profilerTag);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // Nothing to clean up
        }

        public void Dispose()
        {
            tempRenderTarget?.Release();
        }
    }
} 
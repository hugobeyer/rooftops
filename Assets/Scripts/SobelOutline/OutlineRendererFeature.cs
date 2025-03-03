using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class OutlineSettings
    {
        public Material outlineMaterial = null;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Color outlineColor = Color.red;
        [Range(0.1f, 10f)]
        public float outlineThickness = 1.0f;
        [Range(0.0f, 1.0f)]
        public float outlineThreshold = 0.1f;
        [Range(0.0f, 10.0f)]
        public float depthSensitivity = 1.0f;
        [Range(0.0f, 10.0f)]
        public float colorSensitivity = 1.0f;
    }

    public OutlineSettings settings = new OutlineSettings();
    private OutlinePass outlinePass;

    public override void Create()
    {
        outlinePass = new OutlinePass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.outlineMaterial == null)
        {
            Debug.LogWarning("Outline material is null. Skipping pass.");
            return;
        }

        outlinePass.SetupPass(renderer);
        renderer.EnqueuePass(outlinePass);
    }

    protected override void Dispose(bool disposing)
    {
        outlinePass.Dispose();
    }

    class OutlinePass : ScriptableRenderPass
    {
        private RTHandle cameraColorTarget;
        private RTHandle tempRenderTarget;
        private OutlineSettings settings;
        private Material outlineMaterial;
        private ScriptableRenderer renderer;
        private static readonly string profilerTag = "Outline Effect";

        public OutlinePass(OutlineSettings settings)
        {
            this.settings = settings;
            this.outlineMaterial = settings.outlineMaterial;
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
            
            RenderingUtils.ReAllocateIfNeeded(ref tempRenderTarget, descriptor, name: "_TempOutlineTexture");
            
            cameraColorTarget = renderer.cameraColorTargetHandle;
            
            ConfigureTarget(cameraColorTarget);
            ConfigureClear(ClearFlag.None, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (outlineMaterial == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            
            cmd.Clear();
            cmd.BeginSample(profilerTag);
            
            // Update material properties
            outlineMaterial.SetColor("_OutlineColor", settings.outlineColor);
            outlineMaterial.SetFloat("_OutlineThickness", settings.outlineThickness);
            outlineMaterial.SetFloat("_OutlineThreshold", settings.outlineThreshold);
            outlineMaterial.SetFloat("_DepthSensitivity", settings.depthSensitivity);
            outlineMaterial.SetFloat("_ColorSensitivity", settings.colorSensitivity);
            
            // Blit from camera target to temp with outline material
            Blitter.BlitCameraTexture(cmd, cameraColorTarget, tempRenderTarget, outlineMaterial, 0);
            
            // Blit from temp back to camera target
            Blitter.BlitCameraTexture(cmd, tempRenderTarget, cameraColorTarget);
            
            cmd.EndSample(profilerTag);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            tempRenderTarget?.Release();
        }
    }
}
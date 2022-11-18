using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AtmosphericFogRenderFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        static readonly int _Color = Shader.PropertyToID("_Color");
        static readonly int _SunColor = Shader.PropertyToID("_SunColor");
        static readonly int _FogDensity = Shader.PropertyToID("_FogDensity");
        static readonly int _FogDensityPower = Shader.PropertyToID("_FogDensityPower");
        static readonly int _UseHeightFog = Shader.PropertyToID("_UseHeightFog");
        static readonly int _FogHeightStart = Shader.PropertyToID("_FogHeightStart");
        static readonly int _FogHeightEnd = Shader.PropertyToID("_FogHeightEnd");
        static readonly int _FogHeightPower = Shader.PropertyToID("_FogHeightPower");
        static readonly int _ExtraFogHeightEnd = Shader.PropertyToID("_ExtraFogHeightEnd");
        static readonly int _ExtraFogHeightPower = Shader.PropertyToID("_ExtraFogHeightPower");
        static readonly int _DirectionalIntesity = Shader.PropertyToID("_DirectionalIntesity");
        static readonly int _DirectionalPower = Shader.PropertyToID("_DirectionalPower");
        static readonly int _SkyAlpha = Shader.PropertyToID("_SkyAlpha");
        public RenderTargetIdentifier source;
        public Settings settings;
        RenderTargetHandle tempTexture;
        Material material;
        int buffer0ID = Shader.PropertyToID("_TemporaryBuffer0");

        public CustomRenderPass ()
        {
            Shader shader = Shader.Find("OD/AtmosphericFog");
            if(shader == null) return;
            material = CoreUtils.CreateEngineMaterial(shader);//material = new Material(shader);
        }
        
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            var cameraTargetDescriptor = cameraData.cameraTargetDescriptor;

            this.source = cameraData.renderer.cameraColorTargetHandle;
            cmd.GetTemporaryRT( tempTexture.id , cameraTargetDescriptor );
            cameraTargetDescriptor.depthBufferBits = 0;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || settings == null) return;
            if(renderingData.cameraData.cameraType != CameraType.Game && renderingData.cameraData.cameraType != CameraType.SceneView) return;

            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.Clear();

            //Shader.SetGlobalMatrix("_InverseView", renderingData.cameraData.camera.cameraToWorldMatrix);
            material.SetMatrix("_InverseView", renderingData.cameraData.camera.cameraToWorldMatrix);
            
            material.SetColor(_Color, settings.color);
            material.SetColor(_SunColor, settings.sunColor);
            material.SetFloat(_FogDensity, settings.fogDensity);
            material.SetFloat(_FogDensityPower, settings.fogDensityPower);
            material.SetFloat(_SkyAlpha, settings.skyAlpha);

            material.SetInt(_UseHeightFog, settings.useHeightFog ? 1 : 0);
            material.SetFloat(_FogHeightStart, settings.fogHeightStart);
            material.SetFloat(_FogHeightEnd, settings.fogHeightEnd);
            material.SetFloat(_FogHeightPower, settings.fogHeightPower);

            material.SetFloat(_ExtraFogHeightEnd, settings.extraFogHeightEnd);
            material.SetFloat(_ExtraFogHeightPower, settings.extraFogHeightPower);

            material.SetFloat(_DirectionalIntesity, settings.directionalIntesity);
            material.SetFloat(_DirectionalPower, settings.directionalPower);

            cmd.Blit( source , tempTexture.id , material , 0 );
            cmd.Blit( tempTexture.id , source );

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            
        }
    }

    CustomRenderPass m_ScriptablePass;
    public Settings settings;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();
        m_ScriptablePass.settings = settings;

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // m_ScriptablePass.settings = settings;
        renderer.EnqueuePass(m_ScriptablePass);
    }

    [System.Serializable]
    public class Settings
    {
        [Header("General")]
        public Color color = new Color(0.7490196f, 0.8901961f, 1, 1);
        public Color sunColor = new Color(0.8773585f, 0.8098478f, 0.6911268f, 1);
        public float fogDensity = 0.01f;
        [Range(.1f, 30)] public float fogDensityPower = 1;
        [Range(0, 1)] public float skyAlpha = 1;

        [Header("Height")]
        public bool useHeightFog = false;
        public float fogHeightStart = 0;
        public float fogHeightEnd = 30;
        [Range(.1f, 30)] public float fogHeightPower = .5f;

        [Header("Extra Height")]
        public float extraFogHeightEnd = 0;
        [Range(.1f, 30)] public float extraFogHeightPower = .5f;
        
        [Header("Directional")]
        public float directionalIntesity = 1;
        [Range(1, 10)] public float directionalPower = 1;
    }

}

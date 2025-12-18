using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// URP ScriptableRendererFeature that blits a material over the camera.
public class WobbleRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class WobbleSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        public Material wobbleMaterial = null;
    }

    public WobbleSettings settings = new WobbleSettings();

    WobblePass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new WobblePass(settings.renderPassEvent);
        m_ScriptablePass.material = settings.wobbleMaterial;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Prefer material from settings, otherwise check WobbleManager
        Material mat = settings.wobbleMaterial != null ? settings.wobbleMaterial : WobbleManager.WobbleMaterial;
        if (mat == null) return;
        if (!WobbleManager.EffectActive) return;

        m_ScriptablePass.material = mat;
        renderer.EnqueuePass(m_ScriptablePass);
    }

    class WobblePass : ScriptableRenderPass
    {
        public Material material;
        RenderTargetIdentifier m_Source;
        RenderTargetHandle m_TemporaryColorTexture;

        public WobblePass(RenderPassEvent evt)
        {
            this.renderPassEvent = evt;
            m_TemporaryColorTexture.Init("_TempWobbleTexture");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null) return;
            CommandBuffer cmd = CommandBufferPool.Get("WobblePass");

            // Read camera target inside render pass scope (safe)
            m_Source = renderingData.cameraData.renderer.cameraColorTarget;

            var cameraDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            cameraDescriptor.depthBufferBits = 0;

            cmd.GetTemporaryRT(m_TemporaryColorTexture.id, cameraDescriptor, FilterMode.Bilinear);
            // 更新材質顏色，以便 WaterCamera 控制
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", WobbleManager.UnderwaterColor);
            }
            if (material.HasProperty("_Intensity"))
            {
                material.SetFloat("_Intensity", WobbleManager.Intensity);
            }
            if (material.HasProperty("_Amplitude"))
            {
                material.SetFloat("_Amplitude", WobbleManager.Amplitude);
            }
            if (material.HasProperty("_Brightness"))
            {
                material.SetFloat("_Brightness", WobbleManager.Brightness);
            }
            if (material.HasProperty("_Speed"))
            {
                material.SetFloat("_Speed", WobbleManager.Speed);
            }
            if (material.HasProperty("_Offset"))
            {
                material.SetVector("_Offset", new UnityEngine.Vector4(WobbleManager.Offset.x, WobbleManager.Offset.y, 0f, 0f));
            }

            // Perform blit from source -> temporary -> source using the wobble material
            Blit(cmd, m_Source, m_TemporaryColorTexture.Identifier(), material);
            Blit(cmd, m_TemporaryColorTexture.Identifier(), m_Source);

            context.ExecuteCommandBuffer(cmd);
            cmd.ReleaseTemporaryRT(m_TemporaryColorTexture.id);
            CommandBufferPool.Release(cmd);
        }
    }
}

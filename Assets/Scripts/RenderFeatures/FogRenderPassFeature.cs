using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

public class FogRenderPassFeature : ScriptableRendererFeature
{
    class FogRenderPass : ScriptableRenderPass
    {
        const string m_PassName = "FogRenderPass";
        Material m_BlitMaterial;
        // This class stores the data needed by the RenderGraph pass.
        // It is passed as a parameter to the delegate function that executes the RenderGraph pass.
        private class PassData
        {
            public Material m_BlitMaterial;
            public Color fogColor;
            public float fogDensity;
            public float fogOffset;
            public TextureHandle source;
            public TextureHandle destination;
        }

        public void Setup(Material mat)
        {
            m_BlitMaterial = mat;
            requiresIntermediateTexture = true;
        }

        // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        // FrameData is a context container through which URP resources can be accessed and managed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer)
            {
                Debug.LogError("Can't use back buffer as as a texture input. Skipping pass.");
                return;
            }

            var source = resourceData.activeColorTexture;

            var destDesc = renderGraph.GetTextureDesc(source);
            destDesc.name = $"CamerColor-{m_PassName}";
            destDesc.clearBuffer = false;

            TextureHandle destination = renderGraph.CreateTexture(destDesc);

            RenderGraphUtils.BlitMaterialParameters parameters = new(source, destination, m_BlitMaterial, 0);
            // ConfigureInput(ScriptableRenderPassInput.Depth);
            renderGraph.AddBlitPass(parameters, passName: m_PassName);

            resourceData.cameraColor = destination;
        }
    }

    FogRenderPass m_ScriptablePass;
    public Material material;
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new FogRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = renderPassEvent;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isSceneViewCamera)
            return;
        if (material == null)
        {
            Debug.LogWarning("Not material assigned. Skipping Pass.");
            return;
        }
        m_ScriptablePass.Setup(material);
        renderer.EnqueuePass(m_ScriptablePass);
    }

    public void SetFeatureActive(bool active)
    {
        SetActive(active);
    }
}

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Hlubší rozmazání obrazu při otravě, než dovolí URP Depth of Field (max 1.5).
/// Dual filter blur: řetězec zmenšení 1/2 → 1/4 → 1/8 a zpět, pár vzorků na krok,
/// takže je na mobilech levný. Když jed není (_PoisonBlur ≈ 0), pass se vůbec nepřidá.
/// Sílu řídí globální _PoisonWorldBlur z PoisonEffect (0, když je zapnutý starý DoF).
/// </summary>
public class PoisonBlurFeature : ScriptableRendererFeature
{
    [Tooltip("Počet zmenšení (3 = až na 1/8 rozlišení). Víc = hlubší rozmazání, cena roste jen nepatrně.")]
    [Range(1, 5)] public int levels = 4;
    [Tooltip("Násobič rozestupu vzorků při plném jedu. Nad ~3 se začnou objevovat artefakty.")]
    [Range(1f, 4f)] public float maxOffset = 2.5f;

    static readonly int PoisonWorldBlurId = Shader.PropertyToID("_PoisonWorldBlur");
    static readonly int ParamsId = Shader.PropertyToID("_PoisonBlurParams");

    Material material;
    PoisonBlurPass pass;

    public override void Create()
    {
        // URP volá Create() znovu (OnValidate, reload rendereru) bez Dispose – starý materiál uklidit.
        CoreUtils.Destroy(material);
        material = null;

        Shader shader = Shader.Find("Hidden/PoisonBlur");
        if (shader != null) material = CoreUtils.CreateEngineMaterial(shader);
        pass = new PoisonBlurPass
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing,
            // Bez mezitextury by kamera mohla kreslit rovnou do backbufferu a pass by se přeskočil.
            requiresIntermediateTexture = true
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null) return;
        if (renderingData.cameraData.cameraType != CameraType.Game) return;

        float blur = Shader.GetGlobalFloat(PoisonWorldBlurId);
        if (blur < 0.01f) return;

        float offset = Mathf.Lerp(0.5f, maxOffset, blur);
        // Složení s originálem roste rychle, ať nástup není skokový.
        float alpha = PoisonEffect.EaseBlur(blur);
        material.SetVector(ParamsId, new Vector4(offset, alpha, 0f, 0f));

        pass.Setup(material, levels);
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(material);
    }

    class PoisonBlurPass : ScriptableRenderPass
    {
        const int DownPass = 0;
        const int UpPass = 1;
        const int CompositePass = 2;
        const int MaxLevels = 5;

        Material material;
        int levels;
        readonly TextureHandle[] chain = new TextureHandle[MaxLevels + 1];

        class PassData
        {
            public Material material;
            public TextureHandle source;
            public int shaderPass;
        }

        public void Setup(Material mat, int lv)
        {
            material = mat;
            levels = Mathf.Clamp(lv, 1, MaxLevels);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer) return;

            TextureHandle cameraColor = resources.activeColorTexture;
            if (!cameraColor.IsValid()) return;

            TextureDesc baseDesc = renderGraph.GetTextureDesc(cameraColor);
            int w = baseDesc.width, h = baseDesc.height;

            // chain[0] = kamera, chain[i] = 1/2^i
            chain[0] = cameraColor;
            int built = 0;
            for (int i = 1; i <= levels; i++)
            {
                w = Mathf.Max(1, w / 2);
                h = Mathf.Max(1, h / 2);
                if (w < 4 || h < 4) break;

                TextureDesc d = baseDesc;
                d.sizeMode = TextureSizeMode.Explicit;
                d.width = w;
                d.height = h;
                d.msaaSamples = MSAASamples.None;
                d.clearBuffer = false;
                d.filterMode = FilterMode.Bilinear;
                d.wrapMode = TextureWrapMode.Clamp;
                d.name = "PoisonBlur_" + i;
                chain[i] = renderGraph.CreateTexture(d);
                built = i;
            }
            if (built == 0) return;

            // Dolů
            for (int i = 1; i <= built; i++)
                AddBlit(renderGraph, chain[i - 1], chain[i], DownPass, AccessFlags.WriteAll, "PoisonBlur Down");

            // Nahoru (až na 1/2)
            for (int i = built; i > 1; i--)
                AddBlit(renderGraph, chain[i], chain[i - 1], UpPass, AccessFlags.WriteAll, "PoisonBlur Up");

            // Složení zpět do kamery (blend s původním obsahem).
            AddBlit(renderGraph, chain[1], cameraColor, CompositePass, AccessFlags.ReadWrite, "PoisonBlur Composite");
        }

        void AddBlit(RenderGraph rg, TextureHandle source, TextureHandle target, int shaderPass, AccessFlags access, string name)
        {
            using (var builder = rg.AddRasterRenderPass<PassData>(name, out var data))
            {
                data.material = material;
                data.source = source;
                data.shaderPass = shaderPass;
                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(target, 0, access);
                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, d.shaderPass);
                });
            }
        }
    }
}

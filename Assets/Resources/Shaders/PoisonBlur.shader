// Levné celoobrazovkové rozmazání pro efekt otravy – "dual filter" (dual Kawase) blur.
// Obraz se postupně zmenšuje (1/2, 1/4, 1/8…) a zase zvětšuje; každý krok má jen 5–8 vzorků,
// a protože běží v nízkém rozlišení, je to na mobilech velmi levné.
// Používá ho PoisonBlurFeature. Pas 0 = down, 1 = up, 2 = složení přes originál.
Shader "Hidden/PoisonBlur"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    // x = násobič offsetu vzorků, y = síla složení (0–1)
    float4 _PoisonBlurParams;

    half4 S(float2 uv) { return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv); }

    // Velikost pixelu cíle v UV (pro fullscreen trojúhelník konstantní).
    float2 DestTexel(float2 uv) { return abs(float2(ddx(uv.x), ddy(uv.y))); }

    half4 FragDown(Varyings i) : SV_Target
    {
        float2 uv = i.texcoord;
        float2 h = DestTexel(uv) * 0.5 * _PoisonBlurParams.x;
        half4 c = S(uv) * 4.0h;
        c += S(uv - h);
        c += S(uv + h);
        c += S(uv + float2(h.x, -h.y));
        c += S(uv - float2(h.x, -h.y));
        return c * 0.125h;
    }

    half4 FragUp(Varyings i) : SV_Target
    {
        float2 uv = i.texcoord;
        float2 h = DestTexel(uv) * _PoisonBlurParams.x;
        half4 c = S(uv + float2(-h.x * 2.0, 0.0));
        c += S(uv + float2(-h.x, h.y)) * 2.0h;
        c += S(uv + float2(0.0, h.y * 2.0));
        c += S(uv + float2(h.x, h.y)) * 2.0h;
        c += S(uv + float2(h.x * 2.0, 0.0));
        c += S(uv + float2(h.x, -h.y)) * 2.0h;
        c += S(uv + float2(0.0, -h.y * 2.0));
        c += S(uv + float2(-h.x, -h.y)) * 2.0h;
        return c / 12.0h;
    }

    half4 FragComposite(Varyings i) : SV_Target
    {
        half4 c = S(i.texcoord);
        c.a = saturate(_PoisonBlurParams.y);
        return c;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass { Name "Down" HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDown
            ENDHLSL }
        Pass { Name "Up" HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragUp
            ENDHLSL }
        Pass { Name "Composite"
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite
            ENDHLSL }
    }
}

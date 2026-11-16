// UI shader pro efekt otravy: zezelená a rozmaže UI prvky (bylinky).
// Sílu řídí globální proměnné nastavované z PoisonEffect:
//   _PoisonTint  (0–1)  síla zabarvení (pulzuje)
//   _PoisonBlur  (0–1)  síla rozmazání (plynulá)
//   _PoisonBlurEased    stejná síla po křivce rychlého nástupu
//   _PoisonColor        barva jedu
//   _PoisonBlurRadius   max. poloměr rozmazání v UV
//   _PoisonMipBias      max. mip bias (sprite potřebují mipmapy)
// Vychází z UI/Default, takže funguje s Mask i RectMask2D.
Shader "UI/Poison"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            // Globální hodnoty z PoisonEffect
            float _PoisonTint;
            float _PoisonBlur;
            float _PoisonBlurRadius;
            float _PoisonBlurEased;
            float _PoisonMipBias;
            fixed4 _PoisonColor;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            // Vrací premultiplikovaný vzorek, aby se do rozmazání nemíchala barva průhledných texelů.
            half4 SampleTex(float2 uv, float bias)
            {
                half4 c = tex2Dbias(_MainTex, float4(uv, 0, bias)) + _TextureSampleAdd;
                // Mimo 0–1 je průhledný okraj z PoisonUIPadding (Clamp by jinak roztáhl krajní pixely).
                float2 inside = step(0.0, uv) * step(uv, 1.0);
                c.a *= inside.x * inside.y;
                c.rgb *= c.a;
                return c;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 col;
                // _PoisonBlurEased počítá PoisonEffect stejnou křivkou, jakou používá PoisonBlurFeature
                // pro prolnutí světa (rychlý nástup), k tomu pozvolné prohlubování podle surové síly.
                float b = saturate(_PoisonBlurEased) * 0.6 + saturate(_PoisonBlur) * 0.4;
                float r = b * _PoisonBlurRadius;
                // Mip bias rozmaže každý vzorek, takže velký poloměr nevytváří "duchy".
                float bias = b * _PoisonMipBias;

                if (r > 0.0001)
                {
                    // 16 vzorků na zlaté spirále (Vogel disk) – rovnoměrné pokrytí bez viditelných kopií.
                    const float wc = 1.0 / 16.0;
                    col = 0;
                    col += SampleTex(IN.texcoord + float2( 0.1768,  0.0000) * r, bias);
                    col += SampleTex(IN.texcoord + float2(-0.2258,  0.2068) * r, bias);
                    col += SampleTex(IN.texcoord + float2( 0.0346, -0.3938) * r, bias);
                    col += SampleTex(IN.texcoord + float2( 0.2846,  0.3712) * r, bias);
                    col += SampleTex(IN.texcoord + float2(-0.5222, -0.0924) * r, bias);
                    col += SampleTex(IN.texcoord + float2( 0.4947, -0.3147) * r, bias);
                    col += SampleTex(IN.texcoord + float2(-0.1655,  0.6155) * r, bias);
                    col += SampleTex(IN.texcoord + float2(-0.3156, -0.6076) * r, bias);
                    col += SampleTex(IN.texcoord + float2( 0.6846,  0.2500) * r, bias);
                    col += SampleTex(IN.texcoord + float2(-0.7123,  0.2940) * r, bias);
                    col += SampleTex(IN.texcoord + float2( 0.3434, -0.7337) * r, bias);
                    col += SampleTex(IN.texcoord + float2( 0.2537,  0.8089) * r, bias);
                    col += SampleTex(IN.texcoord + float2(-0.7647, -0.4432) * r, bias);
                    col += SampleTex(IN.texcoord + float2( 0.8971, -0.1972) * r, bias);
                    col += SampleTex(IN.texcoord + float2(-0.5475,  0.7788) * r, bias);
                    col += SampleTex(IN.texcoord + float2(-0.1265, -0.9761) * r, bias);
                    col *= wc;
                }
                else
                {
                    col = SampleTex(IN.texcoord, bias);
                }

                // Zpět na nepremultiplikovanou barvu (na konci se znovu násobí alfou).
                col.rgb /= max(col.a, 0.0001);
                col *= IN.color;

                // Zezelenání + mírné odbarvení (podobně jako ColorAdjustments ve Volume).
                float t = saturate(_PoisonTint);
                float grey = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(col.rgb, grey.xxx, t * 0.3);
                col.rgb *= lerp(float3(1, 1, 1), _PoisonColor.rgb, t);

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                col.rgb *= col.a;
                return col;
            }
        ENDCG
        }
    }
}

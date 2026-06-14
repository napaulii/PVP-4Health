Shader "Custom/CloudBackground"
{
    Properties
    {
        [HideInInspector] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _BaseMap ("Base Map", 2D) = "white" {}

        _SkyTop    ("Sky Top Color",    Color) = (0.45, 0.85, 1.0, 1.0)
        _SkyBottom ("Sky Bottom Color", Color) = (1.0,  1.0,  1.0, 1.0)

        _Cloud1 ("Cloud Texture 1", 2D) = "white" {}
        _Cloud2 ("Cloud Texture 2", 2D) = "white" {}
        _Cloud3 ("Cloud Texture 3", 2D) = "white" {}

        _Speed ("Global Speed",  Float) = 0.02
        _Scale ("Global Scale",  Float) = 0.38

        _CloudOpacity ("Cloud Opacity", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"
               "RenderPipeline"="UniversalPipeline"
               "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_Cloud1); SAMPLER(sampler_Cloud1);
            TEXTURE2D(_Cloud2); SAMPLER(sampler_Cloud2);
            TEXTURE2D(_Cloud3); SAMPLER(sampler_Cloud3);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST, _BaseMap_ST;
                float4 _Cloud1_ST, _Cloud2_ST, _Cloud3_ST;
                float4 _SkyTop, _SkyBottom;
                float  _Speed, _Scale;
                float _CloudOpacity;
            CBUFFER_END

            // 2 sins per cloud = organic wander, minimal cost
            float2 Wander(float t, float seed, float speedMul, float scaleMul)
            {
                return float2(
                    sin(t * 0.17 * scaleMul + seed)        * 0.10 + t * _Speed * speedMul,
                    sin(t * 0.11 * scaleMul + seed + 1.38) * 0.06
                );
            }

            float4 Cloud(TEXTURE2D_PARAM(tex, samp), float2 uv,
             float t, float seed, float speedMul, float scaleMul, float4 texST)
            {
                uv  = (uv - 0.5) / (_Scale * scaleMul) + 0.5;
                uv += texST.zw;                              
                uv += Wander(t, seed, speedMul, scaleMul);
                return SAMPLE_TEXTURE2D(tex, samp, frac(uv));
            }

            Varyings vert(Attributes IN)
            {
                Varyings O;
                O.pos = TransformObjectToHClip(IN.pos.xyz);
                O.uv  = IN.uv;
                return O;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float  t  = _Time.y;

                // Sky gradient
                half4 col = lerp(_SkyBottom, _SkyTop, uv.y);

                float4 c1 = Cloud(TEXTURE2D_ARGS(_Cloud1, sampler_Cloud1),
                    uv, t, 0.0,   1.0,  1.00, _Cloud1_ST);

                float4 c2 = Cloud(TEXTURE2D_ARGS(_Cloud2, sampler_Cloud2),
                    uv, t, 5.83, -0.7,  0.85, _Cloud2_ST);

                float4 c3 = Cloud(TEXTURE2D_ARGS(_Cloud3, sampler_Cloud3),
                    uv, t, 11.47, 0.5,  1.15, _Cloud3_ST);

                c1.a *= _CloudOpacity;
                c2.a *= _CloudOpacity;
                c3.a *= _CloudOpacity;

                col = lerp(col, c1, c1.a);
                col = lerp(col, c2, c2.a);
                col = lerp(col, c3, c3.a);
                col.a = 1.0;
                return col;
            }
            ENDHLSL
        }
    }
}
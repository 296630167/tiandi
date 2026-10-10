Shader "天帝/战斗场景高清"
{
    Properties
    {
        [PerRendererData] _MainTex ("图片", 2D) = "white" {}
        _Color ("颜色", Color) = (1,1,1,1)
        _Sharpen ("清晰度", Range(0,0.6)) = 0.32
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Sharpen;
            CBUFFER_END
            float4 _MainTex_TexelSize;
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                half4 center = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float2 t = _MainTex_TexelSize.xy;
                // Use a small 8-tap neighborhood so a 4K source reduced to the
                // tactical camera stays crisp without a second render texture.
                half3 ring =
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(t.x, 0)).rgb +
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - float2(t.x, 0)).rgb +
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0, t.y)).rgb +
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - float2(0, t.y)).rgb +
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + t).rgb +
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - t).rgb +
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(t.x, -t.y)).rgb +
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(-t.x, t.y)).rgb;
                half3 detail = center.rgb - ring * 0.125h;
                center.rgb = saturate(center.rgb + detail * _Sharpen * 1.15h);
                center *= i.color * _Color;
                return center;
            }
            ENDHLSL
        }
    }
}

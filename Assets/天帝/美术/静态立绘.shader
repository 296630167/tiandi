Shader "天帝/静态立绘"
{
    Properties
    {
        [PerRendererData] _MainTex ("图片", 2D) = "white" {}
        _TintColor ("颜色", Color) = (1,1,1,1)
        _Flash ("受击闪白", Range(0,1)) = 0
        _Silhouette ("纯色剪影", Range(0,1)) = 0
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
                float _Flash;
                float _Silhouette;
                float4 _TintColor;
            CBUFFER_END
            Varyings vert(Attributes v)
            {
                Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv; o.color = v.color; return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color * _TintColor;
                c.rgb = lerp(c.rgb, half3(1,1,1), _Flash);
                c.rgb = lerp(c.rgb, _TintColor.rgb, _Silhouette);
                return c;
            }
            ENDHLSL
        }
    }
}

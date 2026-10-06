Shader "天帝/编辑器/主角风格预览"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _Mode ("Style", Float) = 1
        _Outline ("Outline", Float) = 0
        _OutlineWidth ("Outline Width", Float) = 0.003
        _OutlineColor ("Outline Color", Color) = (0.05,0.06,0.08,1)
        _ShadowTint ("Shadow Tint", Color) = (0.38,0.44,0.57,1)
        _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _OutlineColor, _ShadowTint;
            float _Mode, _Outline, _OutlineWidth, _Cull;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz) + o.normalWS * _Outline * _OutlineWidth;
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.tangentWS = float4(TransformObjectToWorldDir(v.tangentOS.xyz), v.tangentOS.w * GetOddNegativeScale());
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }
            float Noise(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            half4 Frag(Varyings i) : SV_Target
            {
                if (_Outline > 0.5) return _OutlineColor;
                float3 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                float3 normal = normalize(i.normalWS);
                if (_Mode < 2.5)
                {
                    float3 tangent = normalize(i.tangentWS.xyz);
                    float3 bitangent = normalize(cross(normal, tangent)) * i.tangentWS.w;
                    float3 bump = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv));
                    bump.xy *= 0.13;
                    normal = normalize(tangent * bump.x + bitangent * bump.y + normal * bump.z);
                }
                float3 lightDir = normalize(float3(-0.55, 0.7, 0.65));
                float n = dot(normal, lightDir);
                float rim = pow(1 - saturate(dot(normal, normalize(GetWorldSpaceViewDir(i.positionWS)))), 3);
                float luminance = dot(base, float3(.2126,.7152,.0722));
                float3 color;
                if (_Mode < 1.5)
                {
                    float skin = step(base.g * 1.06, base.r) * step(base.b * 1.025, base.g) * step(.16, luminance);
                    n = lerp(n, n * .4 + .4, skin * .75);
                    // Three discrete lighting bands and a restrained silhouette.
                    float band = smoothstep(.16,.18,n) * .44 + smoothstep(.58,.60,n) * .56;
                    base = max(0, lerp(luminance.xxx, base, 1.2));
                    color = base * lerp(_ShadowTint.rgb, float3(1.48,1.38,1.23), band) + rim * .045;
                }
                else if (_Mode < 2.5)
                {
                    float light = smoothstep(-.35,.85,n);
                    base = lerp(luminance.xxx, base, .86);
                    base = lerp(base, float3(.64,.66,.59), .075);
                    color = base * lerp(float3(.62,.74,.80), float3(1.52,1.43,1.24), light);
                    color += rim * float3(.13,.11,.08);
                }
                else if (_Mode < 3.5)
                {
                    // Desaturated ink values with paper grain and directional hatching.
                    float grain = Noise(floor(i.positionCS.xy * .55));
                    float value = saturate(pow(luminance, .55) * lerp(.43,1.27,smoothstep(-.15,.7,n)));
                    float hatch = step(.76, frac((i.positionCS.x + i.positionCS.y * .65) / 7));
                    value *= 1 - hatch * .18 * (1 - smoothstep(.1,.6,n));
                    value = saturate(value + (grain - .5) * .045);
                    color = lerp(float3(.055,.072,.068), float3(.94,.94,.88), value);
                }
                else
                {
                    // Colored ink: mineral-pigment hues, dark ink shadows and broken pigment grain.
                    float grain = Noise(floor(i.positionCS.xy * .6));
                    float wash = smoothstep(.08,.60,n + (grain - .5) * .06);
                    base = max(0, lerp(luminance.xxx, base, 1.2));
                    float3 ink = float3(.16,.27,.28);
                    color = base * lerp(ink, float3(1.62,1.45,1.14), wash);
                    float stroke = .5 + .5 * sin(i.positionCS.y * .27 + sin(i.positionCS.x * .085) * 2);
                    color *= .94 + grain * .055 + stroke * .025;
                    color = lerp(color, float3(.029,.052,.052), pow(rim,1.7) * .48);
                }
                return half4(saturate(color), 1);
            }
            ENDHLSL
        }
    }
}

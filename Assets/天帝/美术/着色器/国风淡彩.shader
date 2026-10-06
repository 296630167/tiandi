Shader "天帝/国风淡彩"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BumpScale ("Normal Strength", Range(0,1)) = 0.13
        _Saturation ("Saturation", Range(0,1.5)) = 0.86
        _PaperBlend ("Pigment Softness", Range(0,0.3)) = 0.075
        _PaperColor ("Pigment Color", Color) = (0.8209798,0.8322835,0.7917868,1)
        _ShadowColor ("Cool Shadows", Color) = (0.8094681,0.8756056,0.9063317,1)
        _LightColor ("Warm Light", Color) = (1.2096401,1.1765416,1.1027179,1)
        _RimColor ("Rim Light", Color) = (0.3958812,0.36556458,0.31330413,1)
        _UseSceneLight ("Use Scene Sun", Range(0,1)) = 1
        _PortraitLightDirection ("Portrait Light Direction", Vector) = (-0.55,0.7,0.65,0)
        _Outline ("Outline Material", Float) = 0
        _OutlineWidth ("Outline Width", Range(0,0.01)) = 0.0014
        _OutlineColor ("Outline Color", Color) = (0.15,0.20,0.19,1)
        _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        float4 _BaseColor, _PaperColor, _ShadowColor, _LightColor, _RimColor;
        float4 _OutlineColor, _PortraitLightDirection;
        float _BumpScale, _Saturation, _PaperBlend, _UseSceneLight;
        float _Outline, _OutlineWidth, _Cull;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            float4 tangentWS : TEXCOORD2;
            float2 uv : TEXCOORD3;
            float fog : TEXCOORD4;
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings Vert(Attributes v)
        {
            Varyings o = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            o.normalWS = TransformObjectToWorldNormal(v.normalOS);
            o.positionWS = TransformObjectToWorld(v.positionOS.xyz) + o.normalWS * _Outline * _OutlineWidth;
            o.positionCS = TransformWorldToHClip(o.positionWS);
            o.tangentWS = float4(TransformObjectToWorldDir(v.tangentOS.xyz), v.tangentOS.w * GetOddNegativeScale());
            o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
            o.fog = ComputeFogFactor(o.positionCS.z);
            return o;
        }
        ENDHLSL
        Pass
        {
            Name "国风淡彩"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                if (_Outline > 0.5) return half4(_OutlineColor.rgb,1);
                float3 base = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb * _BaseColor.rgb;
                float3 normal = normalize(i.normalWS);
                if (dot(i.tangentWS.xyz,i.tangentWS.xyz) > .001)
                {
                    float3 tangent = normalize(i.tangentWS.xyz);
                    float3 bitangent = normalize(cross(normal,tangent)) * i.tangentWS.w;
                    float3 bump = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv));
                    bump.xy *= _BumpScale;
                    normal = normalize(tangent * bump.x + bitangent * bump.y + normal * bump.z);
                }
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(i.positionWS));
                #else
                    float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                #endif
                Light sun = GetMainLight(shadowCoord);
                float hasSun = step(.001,dot(sun.color,sun.color));
                float useSun = _UseSceneLight * hasSun;
                float3 direction = normalize(lerp(_PortraitLightDirection.xyz,sun.direction,useSun));
                float lighting = smoothstep(-.35,.85,dot(normal,direction));
                lighting *= lerp(1,sun.shadowAttenuation,useSun);
                float luminance = dot(base,float3(.2126,.7152,.0722));
                base = lerp(luminance.xxx,base,_Saturation);
                base = lerp(base,_PaperColor.rgb,_PaperBlend);
                float3 color = base * lerp(_ShadowColor.rgb,_LightColor.rgb,lighting);
                color *= lerp(float3(1,1,1),clamp(sun.color,.5,1.5),useSun);
                float rim = pow(1 - saturate(dot(normal,normalize(GetWorldSpaceViewDir(i.positionWS)))),3);
                color += rim * _RimColor.rgb;
                color = MixFog(color,i.fog);
                return half4(saturate(color),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            float3 _LightDirection, _LightPosition;
            float4 ShadowVert(Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float3 position = TransformObjectToWorld(v.positionOS.xyz);
                float3 normal = TransformObjectToWorldNormal(v.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 direction = normalize(_LightPosition - position);
                #else
                    float3 direction = _LightDirection;
                #endif
                float4 clipPosition = TransformWorldToHClip(ApplyShadowBias(position,normal,direction));
                #if UNITY_REVERSED_Z
                    clipPosition.z = min(clipPosition.z,UNITY_NEAR_CLIP_VALUE * clipPosition.w);
                #else
                    clipPosition.z = max(clipPosition.z,UNITY_NEAR_CLIP_VALUE * clipPosition.w);
                #endif
                return clipPosition;
            }
            half4 DepthFrag() : SV_Target { clip(.5 - _Outline); return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull [_Cull]
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            half4 DepthFrag(Varyings i) : SV_Target { clip(.5 - _Outline); return i.positionCS.z; }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}

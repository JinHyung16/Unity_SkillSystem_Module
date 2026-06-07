Shader "Jinhyeong/Outline_Lit"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
        _BumpScale("Normal Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}
        _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1.0
        _OcclusionMap("Occlusion Map", 2D) = "white" {}
        _EmissionColor("Emission Color", Color) = (0,0,0,1)
        _EmissionMap("Emission Map", 2D) = "white" {}
        _SpecColor("Specular Color", Color) = (0.2,0.2,0.2,1)
        _SpecGlossMap("Specular Map", 2D) = "white" {}

        [HDR] _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth("Outline Width (px)", Range(0, 5)) = 1.0
        [Toggle(_USE_SMOOTH_NORMAL)] _UseSmoothNormal("Use Smooth Normal (Vertex Color)", Float) = 0
        _OutlineFadeStart("Fade Start (m)", Float) = 30.0
        _OutlineFadeEnd("Fade End (m)", Float) = 60.0

        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _ReceiveShadows("Receive Shadows", Float) = 1.0
        [HideInInspector] _WorkflowMode("WorkflowMode", Float) = 1.0
        [HideInInspector] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0
        [HideInInspector] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [HideInInspector] _EnvironmentReflections("Environment Reflections", Float) = 1.0
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Opaque"
            "RenderPipeline"  = "UniversalPipeline"
            "Queue"           = "Geometry"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma target 2.0
            #pragma multi_compile_local _ _USE_SMOOTH_NORMAL

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _OutlineFadeStart;
                float  _OutlineFadeEnd;
                float  _Smoothness;
                float  _Metallic;
                float  _Cutoff;
                float  _BumpScale;
                float4 _BumpMap_ST;
                float  _OcclusionStrength;
                float4 _OcclusionMap_ST;
                float4 _EmissionColor;
                float4 _EmissionMap_ST;
                float4 _SpecColor;
                float4 _SpecGlossMap_ST;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  fade       : TEXCOORD0;
            };

            Varyings OutlineVert(Attributes IN)
            {
                Varyings OUT;

                float3 normalOS;
                #ifdef _USE_SMOOTH_NORMAL
                    normalOS = IN.color.rgb * 2.0 - 1.0;
                #else
                    normalOS = IN.normalOS;
                #endif

                float4 positionCS = TransformObjectToHClip(IN.positionOS.xyz);

                float3 normalWS = TransformObjectToWorldNormal(normalOS);
                float3 normalCS = mul((float3x3)UNITY_MATRIX_VP, normalWS);

                float2 offset = normalize(normalCS.xy) * _OutlineWidth * positionCS.w;
                offset.x *= _ScreenParams.y / _ScreenParams.x;
                offset *= 2.0 / _ScreenParams.y;

                positionCS.xy += offset;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float viewDist = length(positionWS - _WorldSpaceCameraPos);
                float fade = saturate(1.0 - (viewDist - _OutlineFadeStart) /
                                            max(0.01, _OutlineFadeEnd - _OutlineFadeStart));
                OUT.fade = fade;

                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 OutlineFrag(Varyings IN) : SV_Target
            {
                half4 col = _OutlineColor;
                col.a *= IN.fade;
                clip(col.a - 0.001);
                return col;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ForwardLit"
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }

    FallBack "Universal Render Pipeline/Lit"
}

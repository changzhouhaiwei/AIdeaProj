Shader "Effect/EFF_UI_MaskDistort"
{
    Properties
    {
        [Header(Rendering)]
        [Enum(Alpha, 10, Add, 1)] _BlendDst ("Render Mode", Float) = 10
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0

        [Header(Main)]
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainUVSpeed ("Main UV Speed (XY)", Vector) = (0, 0, 0, 0)

        [Header(Mask)]
        [Toggle(_USE_MASK)] _UseMask ("Enable Mask", Float) = 1
        _MaskTex ("Mask Texture", 2D) = "white" {}
        [Enum(Alpha, 0, Red, 1)] _MaskChannel ("Mask Channel", Float) = 0
        _MaskUVSpeed ("Mask UV Speed (XY)", Vector) = (0, 0, 0, 0)

        [Header(Distort)]
        [Toggle(_USE_DISTORT)] _UseDistort ("Enable Distort", Float) = 1
        _NoiseTex ("Noise Texture", 2D) = "gray" {}
        _DistortStrength ("Distort Strength", Range(0, 0.5)) = 0.05
        _NoiseUVSpeed ("Noise UV Speed (XY)", Vector) = (0, 0, 0, 0)

        [Header(UI Masking)]
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
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull [_Cull]
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha [_BlendDst]
        ColorMask [_ColorMask]

        Pass
        {
            Name "EFFUIMaskDistort"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            #pragma shader_feature_local _USE_MASK
            #pragma shader_feature_local _USE_DISTORT

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _TextureSampleAdd;
            half4 _Color;
            float4 _MainUVSpeed;
            float4 _ClipRect;

            #ifdef _USE_MASK
            sampler2D _MaskTex;
            float4 _MaskTex_ST;
            float4 _MaskUVSpeed;
            half _MaskChannel;
            #endif

            #ifdef _USE_DISTORT
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            float4 _NoiseUVSpeed;
            half _DistortStrength;
            #endif

            float2 FlowUV(float2 uv, float4 st, float4 speed)
            {
                return uv * st.xy + st.zw + _Time.y * speed.xy;
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(v2f IN) : SV_Target
            {
                float2 mainUV = FlowUV(IN.texcoord, _MainTex_ST, _MainUVSpeed);

                #ifdef _USE_DISTORT
                float2 noiseUV = FlowUV(IN.texcoord, _NoiseTex_ST, _NoiseUVSpeed);
                half2 noise = tex2D(_NoiseTex, noiseUV).rg;
                mainUV += (noise * 2.0h - 1.0h) * _DistortStrength;
                #endif

                half4 color = (tex2D(_MainTex, mainUV) + _TextureSampleAdd) * IN.color;

                #ifdef _USE_MASK
                float2 maskUV = FlowUV(IN.texcoord, _MaskTex_ST, _MaskUVSpeed);
                half4 maskSample = tex2D(_MaskTex, maskUV);
                half mask = _MaskChannel > 0.5h ? maskSample.r : maskSample.a;
                color.a *= mask;
                #endif

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}

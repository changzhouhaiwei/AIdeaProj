Shader "UI/IceBG_Mask"
{
    Properties
    {
        [Header(Main)]
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Ice Reveal)]
        _Progress ("Progress", Range(0, 1)) = 1
        _Softness ("Edge Softness", Range(0.001, 0.5)) = 0.08
        [Enum(Box, 0, Radial, 1)] _Shape ("Shape", Float) = 0
        _Aspect ("Aspect (W/H)", Float) = 1
        _Center ("Center (XY)", Vector) = (0.5, 0.5, 0, 0)
        _EdgePower ("Edge Power", Range(0.2, 4)) = 1
        [Toggle] _Invert ("Invert (Center Out)", Float) = 0

        [Header(Mask)]
        _MaskTex ("Mask Texture (R)", 2D) = "white" {}
        _MaskStrength ("Mask Strength", Range(0, 1)) = 0.35
        _MaskBlend ("Mask Blend", Range(0, 1)) = 0.5

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

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "IceBGMask"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

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
            sampler2D _MaskTex;
            float4 _MainTex_ST;
            float4 _MaskTex_ST;
            half4 _TextureSampleAdd;
            half4 _Color;
            float4 _ClipRect;

            half _Progress;
            half _Softness;
            half _Shape;
            half _Aspect;
            float4 _Center;
            half _EdgePower;
            half _Invert;
            half _MaskStrength;
            half _MaskBlend;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            // 0=center, 1=edges. Box follows rect borders; Radial is circular.
            half EdgeFactor(float2 uv)
            {
                float2 d = uv - _Center.xy;
                d.x *= max((float)_Aspect, 0.0001);

                half edge;
                if (_Shape > 0.5)
                {
                    edge = (half)(length(d) * 1.41421356);
                }
                else
                {
                    edge = (half)(max(abs(d.x), abs(d.y)) * 2.0);
                }

                return saturate(pow(saturate(edge), _EdgePower));
            }

            half4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                float2 maskUV = TRANSFORM_TEX(IN.texcoord, _MaskTex);
                half maskSample = tex2D(_MaskTex, maskUV).r;

                half edge = EdgeFactor(IN.texcoord);
                half maskDriven = saturate(edge * lerp(1.0h, maskSample, _MaskBlend)
                    + (maskSample - 0.5h) * _MaskStrength);
                half factor = maskDriven;

                if (_Invert > 0.5h)
                {
                    factor = 1.0h - factor;
                }

                // Progress up: cutoff 1->0, edges appear first then freeze toward center.
                half cutoff = 1.0h - saturate(_Progress);
                half soft = max(_Softness, 0.001h);
                half reveal = smoothstep(cutoff - soft, cutoff + soft, factor);
                color.a *= reveal;

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

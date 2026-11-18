Shader "UI/DissolveAppear"
{
    Properties
    {
        [Header(Main)]
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Dissolve Appear)]
        _Progress ("Progress", Range(0, 1)) = 1
        _Softness ("Edge Softness", Range(0.001, 0.5)) = 0.08
        _NoiseTex ("Noise Texture (R)", 2D) = "gray" {}
        _NoiseScale ("Noise Tiling", Vector) = (1, 1, 0, 0)
        _NoiseStrength ("Noise Contrast", Range(0.1, 2)) = 1

        [Header(Edge Glow)]
        [HDR] _EdgeColor ("Edge Color", Color) = (0.55, 0.85, 1, 1)
        _EdgeWidth ("Edge Width", Range(0, 0.3)) = 0.06
        _EdgeIntensity ("Edge Intensity", Range(0, 4)) = 1.5

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
            Name "DissolveAppear"

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
            sampler2D _NoiseTex;
            float4 _MainTex_ST;
            float4 _NoiseTex_ST;
            half4 _TextureSampleAdd;
            half4 _Color;
            float4 _ClipRect;

            half _Progress;
            half _Softness;
            float4 _NoiseScale;
            half _NoiseStrength;
            half4 _EdgeColor;
            half _EdgeWidth;
            half _EdgeIntensity;

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

            half4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                float2 noiseUV = IN.texcoord * _NoiseScale.xy + _NoiseTex_ST.zw;
                half noise = saturate(pow(saturate(tex2D(_NoiseTex, noiseUV).r), _NoiseStrength));

                // Progress 0 -> hidden, 1 -> fully visible (reverse dissolve)
                half soft = max(_Softness, 0.001h);
                half progress = saturate(_Progress);
                // Remap so Progress=1 fully clears softness band
                half threshold = lerp(-soft, 1.0h + soft, progress);
                half reveal = smoothstep(threshold - soft, threshold + soft, noise);
                // appear: show where noise <= threshold  => invert typical dissolve-out
                reveal = 1.0h - reveal;

                // Keep a thin glowing edge near the dissolve front
                half edgeDist = abs(noise - threshold);
                half edge = 1.0h - smoothstep(0.0h, max(_EdgeWidth, 0.0001h), edgeDist);
                edge *= reveal * (1.0h - saturate((progress - 0.92h) / 0.08h));
                color.rgb += _EdgeColor.rgb * edge * _EdgeIntensity;

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

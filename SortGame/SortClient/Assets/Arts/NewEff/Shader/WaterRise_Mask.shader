Shader "UI/WaterRise_Mask"
{
    Properties
    {
        [Header(Main)]
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Water Rise)]
        _Progress ("Progress", Range(0, 1)) = 1
        _Softness ("Edge Softness", Range(0.001, 0.5)) = 0.06
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.2)) = 0.035
        _WaveFrequency ("Wave Frequency", Range(0.5, 40)) = 8
        _WaveSpeed ("Wave Speed", Float) = 1.2
        _NoiseStrength ("Noise Strength", Range(0, 0.2)) = 0.02
        _NoiseScale ("Noise Scale", Range(0.5, 20)) = 4
        _NoiseSpeed ("Noise Speed (XY)", Vector) = (0.15, 0.35, 0, 0)
        [Toggle] _Invert ("Invert (Top Down)", Float) = 0

        [Header(Optional Mask)]
        _MaskTex ("Mask Texture (R)", 2D) = "white" {}
        _MaskStrength ("Mask Strength", Range(0, 1)) = 0

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
            Name "WaterRiseMask"

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
            half _WaveAmplitude;
            half _WaveFrequency;
            half _WaveSpeed;
            half _NoiseStrength;
            half _NoiseScale;
            float4 _NoiseSpeed;
            half _Invert;
            half _MaskStrength;

            half Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            half ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                half a = Hash21(i);
                half b = Hash21(i + float2(1, 0));
                half c = Hash21(i + float2(0, 1));
                half d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

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

                // UV.y: 0 bottom -> 1 top for typical UI Image
                half height = IN.texcoord.y;
                if (_Invert > 0.5h)
                {
                    height = 1.0h - height;
                }

                half wave = sin((IN.texcoord.x * _WaveFrequency + _Time.y * _WaveSpeed) * 6.2831853h)
                    * _WaveAmplitude;

                float2 noiseUV = IN.texcoord * _NoiseScale + _Time.y * _NoiseSpeed.xy;
                half noise = (ValueNoise(noiseUV) * 2.0h - 1.0h) * _NoiseStrength;

                float2 maskUV = TRANSFORM_TEX(IN.texcoord, _MaskTex);
                half maskSample = tex2D(_MaskTex, maskUV).r;
                half maskBias = (maskSample - 0.5h) * _MaskStrength;

                // Remap Progress so 0 is fully hidden and 1 clears past the top
                // (soft edge + wave/noise dips), otherwise Progress=1 still fades the top.
                half soft = max(_Softness, 0.001h);
                half pad = soft + _WaveAmplitude + _NoiseStrength + abs(_MaskStrength) * 0.5h;
                half waterLevel = lerp(-pad, 1.0h + pad, saturate(_Progress)) + wave + noise + maskBias;
                half aboveWater = smoothstep(waterLevel - soft, waterLevel + soft, height);
                color.a *= (1.0h - aboveWater);

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

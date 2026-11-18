Shader "UI/BrightEdgeDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Dissolve)]
        // 0 = 这层完整显示，1 = 这层溶解完
        _Progress ("Progress", Range(0, 1)) = 0
        // xy 指向先消失的那一边。(0,-1) 从下往上，(0,1) 从上往下，(1,0) 从左往右，(-1,0) 从右往左
        _Direction ("Direction", Vector) = (0, -1, 0, 0)
        _Softness ("Edge Softness", Range(0.001, 0.5)) = 0.03
        _NoiseScale ("Noise Tiling", Vector) = (3, 3, 0, 0)
        _NoiseAmount ("Noise Amount", Range(0, 1)) = 0.22

        [Header(Bright Edge)]
        [HDR] _EdgeColor ("Edge Color", Color) = (1, 0.92, 0.55, 1)
        _EdgeWidth ("Edge Width", Range(0.001, 0.4)) = 0.06
        _EdgeIntensity ("Edge Intensity", Range(0, 8)) = 2

        [HideInInspector] _LocalRect ("Local Rect", Vector) = (0, 0, 1, 1)

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
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
            Name "BrightEdgeDissolve"

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
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 localUv : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _Progress;
            float4 _Direction;
            float _Softness;
            float4 _NoiseScale;
            float _NoiseAmount;
            fixed4 _EdgeColor;
            float _EdgeWidth;
            float _EdgeIntensity;
            float4 _LocalRect;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
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

                // 没写入真实矩形时（默认 1x1），直接用图片 UV。
                // 否则顶点是 ±142 这种本地坐标，会被 saturate 压成 0 或 1，亮边铺满半张图。
                float2 rectUv = float2(
                    (IN.vertex.x - _LocalRect.x) / max(_LocalRect.z, 1e-5),
                    (IN.vertex.y - _LocalRect.y) / max(_LocalRect.w, 1e-5));
                float rectReady = step(2.0, min(_LocalRect.z, _LocalRect.w));
                OUT.localUv = lerp(IN.texcoord, rectUv, rectReady);
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                float2 uv = saturate(IN.localUv);
                float2 dir = _Direction.xy;
                dir /= max(length(dir), 1e-4);

                // remain = 0 的一侧先消失
                float along = dot(uv, dir);
                float alongMin = min(dir.x, 0.0) + min(dir.y, 0.0);
                float alongMax = max(dir.x, 0.0) + max(dir.y, 0.0);
                float remain = 1.0 - (along - alongMin) / max(alongMax - alongMin, 1e-5);

                float noise = ValueNoise(uv * max(_NoiseScale.xy, float2(0.01, 0.01)));
                remain += (noise - 0.5) * _NoiseAmount;

                float soft = max(_Softness, 0.001);
                float pad = soft + _NoiseAmount * 0.5;
                float progress = saturate(_Progress);
                float threshold = lerp(-pad, 1.0 + pad, progress);
                float dist = remain - threshold;

                half spriteA = color.a;

                float visible = smoothstep(-soft, soft, dist);
                float edge = 1.0 - smoothstep(0.0, max(_EdgeWidth, 0.001), abs(dist));
                float show = smoothstep(0.0, 0.02, progress);
                float hide = smoothstep(0.98, 1.0, progress);
                visible = lerp(1.0, visible, show);
                visible = lerp(visible, 0.0, hide);
                edge *= show * (1.0 - hide);
                float gone = step(0.999, progress);
                visible *= 1.0 - gone;
                edge *= 1.0 - gone;

                color.rgb += _EdgeColor.rgb * edge * _EdgeIntensity;
                // 透明像素保持透明。进度拉满后亮边也不再留在画面上。
                color.a = spriteA * max(visible, edge * _EdgeColor.a);

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

    Fallback "UI/Default"
}

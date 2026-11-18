Shader "UI/BrushStrokeDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Dissolve)]
        // 0 = 图片完整，1 = 图片被笔刷溶掉。勾上 Reverse 后反过来，像一笔一笔把图画出来
        _Progress ("Progress", Range(0, 1)) = 0
        [Toggle] _Reverse ("Reverse (Paint On)", Float) = 0
        // 笔刷来回的那条轴。(0,-1) 时笔画是横的，从下往上，一笔向左、一笔向右
        _Direction ("Direction", Vector) = (0, -1, 0, 0)
        _Softness ("Edge Softness", Range(0.001, 0.5)) = 0.045

        [Header(Brush)]
        _StrokeCount ("Stroke Count", Range(1, 12)) = 7
        _StrokeDepth ("Tip Length", Range(0, 0.6)) = 0.5
        _Seed ("Random Seed", Float) = 0
        _SizeVariation ("Size Variation", Range(0, 1)) = 0.75
        _TimingVariation ("Timing Variation", Range(0, 1)) = 0.85
        _BristleCount ("Bristle Count", Range(4, 80)) = 18
        _Bristle ("Bristle Breakup", Range(0, 1)) = 0.8

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
            Name "BrushStrokeDissolve"

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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _Progress;
            float _Reverse;
            float4 _Direction;
            float _Softness;
            float _StrokeCount;
            float _StrokeDepth;
            float _Seed;
            float _SizeVariation;
            float _TimingVariation;
            float _BristleCount;
            float _Bristle;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float Axis01(float2 uv, float2 axis)
            {
                float along = dot(uv, axis);
                float alongMin = min(axis.x, 0.0) + min(axis.y, 0.0);
                float alongMax = max(axis.x, 0.0) + max(axis.y, 0.0);
                return (along - alongMin) / max(alongMax - alongMin, 1e-5);
            }

            float StrokeHash(float id, float channel)
            {
                return Hash21(float2(id + 1.0, channel + _Seed * 17.13));
            }

            float StrokeSize(float id)
            {
                float varied = lerp(0.18, 2.8, pow(StrokeHash(id, 1.7), 0.7));
                return lerp(1.0, varied, saturate(_SizeVariation));
            }

            void StrokeTiming(float id, float count, out float start, out float dur)
            {
                float spread = saturate(_TimingVariation);
                float h1 = StrokeHash(id, 4.9);
                float h2 = StrokeHash(id, 8.2);
                float h3 = StrokeHash(id, 2.1);
                float order = id / max(count - 1.0, 1.0);
                start = lerp(order, h1, spread);
                start = saturate(start * lerp(1.0, 0.62, spread) + h3 * spread * 0.22);
                dur = lerp(0.26, lerp(0.05, 0.58, h2) * lerp(0.55, 1.35, h3), spread);
                dur = max(dur, 0.04);
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

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                half spriteA = color.a;

                float2 uv = saturate(IN.texcoord);
                float2 dir = _Direction.xy;
                dir /= max(length(dir), 1e-4);
                float2 perp = float2(-dir.y, dir.x);

                // along：0 在先被刷到的一侧。across：沿笔锋左右。
                float along = 1.0 - Axis01(uv, dir);
                float across = Axis01(uv, perp);
                float count = round(clamp(_StrokeCount, 1.0, 12.0));
                float progress = saturate(_Progress);
                float soft = max(_Softness, 0.001);

                float totalSize = 0.0;
                float maxEnd = 0.001;
                for (int i = 0; i < 12; i++)
                {
                    if ((float)i >= count)
                        break;
                    float strokeStart;
                    float strokeDur;
                    totalSize += StrokeSize(i);
                    StrokeTiming(i, count, strokeStart, strokeDur);
                    maxEnd = max(maxEnd, strokeStart + strokeDur);
                }

                float erased = 0.0;
                float accSize = 0.0;
                float hairs = clamp(_BristleCount, 4.0, 48.0);
                float breakup = saturate(_Bristle);
                float feather = lerp(0.008, 0.03, saturate(soft));

                for (int j = 0; j < 12; j++)
                {
                    if ((float)j >= count)
                        break;

                    float size = StrokeSize(j);
                    float height = size / max(totalSize, 1e-4);
                    float center = accSize + height * 0.5;
                    float halfHeight = max(height * 0.5, 1e-4);
                    accSize += height;

                    float x = across * 6.0;
                    float x0 = floor(x);
                    float xf = frac(x);
                    xf = xf * xf * (3.0 - 2.0 * xf);
                    float bend = lerp(Hash21(float2(x0, (float)j + 2.0 + _Seed)), Hash21(float2(x0 + 1.0, (float)j + 2.0 + _Seed)), xf);
                    float wobble = (bend - 0.5) * halfHeight * lerp(0.04, 0.14, breakup);
                    float fromCenter = abs(along - center - wobble) / halfHeight;
                    if (fromCenter > 1.4)
                        continue;

                    float bandT = saturate((along - center - wobble) / max(height, 1e-4) + 0.5);
                    float bristlePos = bandT * hairs;
                    float bristleId = floor(bristlePos);
                    float bristleFrac = frac(bristlePos);
                    float hA = Hash21(float2(bristleId + 1.0, (float)j + 19.0 + _Seed));
                    float hB = Hash21(float2(bristleId + 1.0, (float)j + 3.0 + _Seed));

                    float strokeStart;
                    float strokeDur;
                    StrokeTiming(j, count, strokeStart, strokeDur);
                    float local = saturate((progress * maxEnd - strokeStart) / max(strokeDur, 1e-4));
                    float moveLeft = 1.0 - fmod((float)j, 2.0);
                    float oriented = lerp(across, 1.0 - across, moveLeft);
                    float shortPick = 1.0 - step(0.28, hA);
                    float longPick = step(0.72, hA);
                    float lenShort = lerp(0.04, 0.28, pow(saturate(hA / 0.28), 0.65));
                    float lenMid = lerp(0.7, 1.1, saturate((hA - 0.28) / 0.44));
                    float lenLong = lerp(1.55, 2.45, pow(saturate((hA - 0.72) / 0.28), 0.4));
                    float len = lerp(lenMid, lenShort, shortPick);
                    len = lerp(len, lenLong, longPick);
                    len = lerp(1.0, len, breakup);

                    float spread = max(_StrokeDepth, 0.0) * 0.55;
                    float amp = spread * 1.55 + feather;
                    float median = lerp(-amp, 1.0 + amp, local);
                    float travel = median + (len - 1.0) * spread - oriented;
                    float passed = smoothstep(-feather, feather, travel);
                    float past = saturate(max(travel, 0.0) / 0.045);
                    float beyond = saturate((oriented - median) / max(spread * 0.45, 0.02));
                    float halfW = lerp(0.5, 0.12, saturate(beyond * breakup));
                    float hair = 1.0 - smoothstep(halfW, halfW + 0.1, abs(bristleFrac - 0.5));
                    float inside = 1.0 - smoothstep(1.08, 1.28, fromCenter);
                    float body = inside * passed * lerp(hair, 1.0, past * (1.0 - beyond));
                    float wispAlpha = lerp(1.0, lerp(0.35, 1.0, hB), beyond * longPick * breakup);
                    erased = max(erased, body * wispAlpha);
                }
                float visible = (_Reverse > 0.5) ? erased : (1.0 - erased);
                float atStart = smoothstep(0.0, 0.001, progress);
                float atEnd = smoothstep(0.97, 0.99, progress);
                visible = lerp((_Reverse > 0.5) ? 0.0 : 1.0, visible, atStart);
                visible = lerp(visible, (_Reverse > 0.5) ? 1.0 : 0.0, atEnd);

                color.a = spriteA * visible;

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

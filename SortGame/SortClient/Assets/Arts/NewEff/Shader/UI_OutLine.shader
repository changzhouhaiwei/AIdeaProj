Shader "UI/UI_OutLine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 8)) = 1
        _OutlineSoftness ("Outline Softness", Range(0, 1)) = 0.2
        [Toggle(_OUTLINE_8DIR)] _Outline8Dir ("8 Direction (Better Quality)", Float) = 0

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
            Name "UIOutLine"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            #pragma shader_feature_local _OUTLINE_8DIR

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
                half2 outlineUVOffset : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            half4 _TextureSampleAdd;
            half4 _Color;
            half4 _OutlineColor;
            half _OutlineWidth;
            half _OutlineSoftness;
            float4 _ClipRect;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.color = IN.color * _Color;
                // Precompute texel-space width in vertex stage to keep fragment light.
                OUT.outlineUVOffset = _MainTex_TexelSize.xy * _OutlineWidth;
                return OUT;
            }

            half SampleAlpha(float2 uv)
            {
                return (tex2D(_MainTex, uv) + _TextureSampleAdd).a;
            }

            half4 frag(v2f IN) : SV_Target
            {
                half4 texColor = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;
                half4 face = texColor * IN.color;
                half faceA = face.a;

                half2 o = IN.outlineUVOffset;
                half outlineA = 0;

                // 4-dir is enough for most UI icons; optional 8-dir for smoother corners.
                outlineA = max(outlineA, SampleAlpha(IN.texcoord + half2(o.x, 0)));
                outlineA = max(outlineA, SampleAlpha(IN.texcoord - half2(o.x, 0)));
                outlineA = max(outlineA, SampleAlpha(IN.texcoord + half2(0, o.y)));
                outlineA = max(outlineA, SampleAlpha(IN.texcoord - half2(0, o.y)));

                #ifdef _OUTLINE_8DIR
                outlineA = max(outlineA, SampleAlpha(IN.texcoord + half2(o.x, o.y)));
                outlineA = max(outlineA, SampleAlpha(IN.texcoord + half2(o.x, -o.y)));
                outlineA = max(outlineA, SampleAlpha(IN.texcoord + half2(-o.x, o.y)));
                outlineA = max(outlineA, SampleAlpha(IN.texcoord + half2(-o.x, -o.y)));
                #endif

                // Outer rim only: neighbors opaque, current pixel transparent.
                half rim = saturate(outlineA - faceA);
                half soft = max(_OutlineSoftness, 0.0001);
                rim = smoothstep(0, soft, rim);

                half4 outline = _OutlineColor;
                outline.a *= rim * IN.color.a;

                // Premultiplied-style composite: face over outline.
                half4 color;
                color.rgb = face.rgb * faceA + outline.rgb * outline.a * (1 - faceA);
                color.a = outline.a + faceA * (1 - outline.a);

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

Shader "Sprites/TileHintMirrorFlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.314, 0.847, 1, 1)
        _ShineColor ("Shine Color", Color) = (2.12, 2.40, 2.75, 1)
        _BaseStrength ("Base Strength", Range(0, 2)) = 0.35
        _ShineStrength ("Shine Strength", Range(0, 4)) = 1.8
        _ShineWidth ("Shine Width", Range(0.02, 1)) = 0.28
        _ShineSoftness ("Shine Softness", Range(0.05, 1)) = 0.45
        _ShineAngle ("Shine Angle", Float) = 135
        _ShineProgress ("Shine Progress", Range(-2, 2)) = 0
        _ShineFade ("Shine Fade", Range(0, 1)) = 1
        _Pulse ("Pulse", Range(0, 1)) = 0.5
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _ShineColor;
            fixed4 _RendererColor;
            half _BaseStrength;
            half _ShineStrength;
            half _ShineWidth;
            half _ShineSoftness;
            half _ShineAngle;
            half _ShineProgress;
            half _ShineFade;
            half _Pulse;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color * _RendererColor;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                half mask = tex.a;
                clip(mask - 0.01);

                // 135° 斜向扫光，复刻 Coffee UIEffect Transition
                half rad = radians(_ShineAngle);
                half2 dir = half2(cos(rad), sin(rad));
                half axis = dot(i.uv - 0.5, dir);
                half halfW = max(_ShineWidth * 0.5, 0.001);
                half d = abs(axis - _ShineProgress);
                half band = 1.0 - saturate(d / halfW);
                band = pow(band, max(_ShineSoftness * 2.0, 0.2));

                // 中心略亮，边缘柔一点，增加镜面感
                half centerBoost = 1.0 - saturate(length(i.uv - 0.5) * 1.4);
                half pulse = saturate(_Pulse);

                half3 baseCol = i.color.rgb * (_BaseStrength * pulse);
                half3 shineCol = _ShineColor.rgb * band * _ShineStrength * _ShineFade * (0.55 + 0.45 * pulse);
                half3 glow = (baseCol + shineCol) * (0.75 + 0.25 * centerBoost);

                return fixed4(glow * mask, 0);
            }
            ENDCG
        }
    }
}

Shader "FishTile/SceneBgCircleReveal"
{
    Properties
    {
        _OldTex ("Old Background", 2D) = "white" {}
        _NewTex ("New Background", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _RevealProgress ("Reveal Progress", Range(0, 1)) = 0
        _EdgeSoftness ("Edge Softness", Range(0, 2)) = 0.04
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
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnitySprites.cginc"

            sampler2D _OldTex;
            sampler2D _NewTex;
            float _RevealProgress;
            float _EdgeSoftness;

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 oldColor = tex2D(_OldTex, IN.texcoord) * _Color;
                fixed4 newColor = tex2D(_NewTex, IN.texcoord) * _Color;

                float2 centered = IN.texcoord - float2(0.5, 0.5);
                float dist = length(centered) * 2.0;
                float radius = _RevealProgress * 1.45;
                float circleMask = 1.0 - smoothstep(radius - _EdgeSoftness, radius, dist);
                float reveal = circleMask * _RevealProgress;

                return lerp(oldColor, newColor, reveal);
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}

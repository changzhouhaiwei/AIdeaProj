Shader "Effect/EFF_UI_Simple"
{
    Properties
    {
        [Header(Rendering)]
        [Enum(Alpha, 10, Add, 1)] _BlendDst ("Render Mode", Float) = 10
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0

        [Header(Main Texture)]
        [PerRendererData] _MainTex ("Image Sprite (Auto)", 2D) = "white" {}
        [Toggle(_USE_CUSTOM_MAIN_TEX)] _UseCustomMainTex ("Use Custom Main Texture", Float) = 0
        _CustomMainTex ("Custom Main Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MainUVStretch ("Main UV Stretch (XY)", Vector) = (1, 1, 0, 0)
        _MainUVRotation ("Main UV Rotation", Range(-180, 180)) = 0
        _MainUVPivot ("Main UV Rotation Pivot (XY)", Vector) = (0.5, 0.5, 0, 0)
        _MainUVSpeed ("Linear Flow Speed (XY)", Vector) = (0, 0, 0, 0)
        [Enum(Linear, 0, RadialIn, 1, RadialOut, 2, CircularCW, 3, CircularCCW, 4)] _MainFlowMode ("Main Flow Mode", Float) = 0
        _MainFlowCenter ("Radial Flow Center (XY)", Vector) = (0.5, 0.5, 0, 0)
        _MainPolarStretch ("Radial UV Stretch (Angle, Radius)", Vector) = (1, 1, 0, 0)
        _MainPolarFlowSpeed ("Radial / Circular Flow Speed", Float) = 0

        [Header(Mask)]
        _MaskTex ("Mask Texture (Alpha)", 2D) = "white" {}
        _MaskUVStretch ("Mask UV Stretch (XY)", Vector) = (1, 1, 0, 0)
        _MaskUVSpeed ("Mask UV Auto Flow (XY)", Vector) = (0, 0, 0, 0)

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
            Name "EFFUISimple"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            #pragma shader_feature_local _USE_CUSTOM_MAIN_TEX

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
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
            sampler2D _CustomMainTex;
            sampler2D _MaskTex;
            float4 _MainTex_ST;
            float4 _CustomMainTex_ST;
            float4 _MaskTex_ST;
            fixed4 _Color;
            float4 _MainUVStretch;
            float _MainUVRotation;
            float4 _MainUVPivot;
            float4 _MainUVSpeed;
            float _MainFlowMode;
            float4 _MainFlowCenter;
            float4 _MainPolarStretch;
            float _MainPolarFlowSpeed;
            float4 _MaskUVStretch;
            float4 _MaskUVSpeed;
            float4 _ClipRect;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            float2 GetFlowUV(float2 sourceUV, float4 textureST, float4 stretch, float4 speed)
            {
                float2 uv = sourceUV * textureST.xy + textureST.zw;
                return uv * stretch.xy + stretch.zw + _Time.y * speed.xy;
            }

            float2 RotateUV(float2 uv, float2 pivot, float angleInDegrees)
            {
                float angleInRadians = angleInDegrees * 0.01745329252;
                float sine = sin(angleInRadians);
                float cosine = cos(angleInRadians);
                float2 centeredUV = uv - pivot;
                return float2(
                    centeredUV.x * cosine - centeredUV.y * sine,
                    centeredUV.x * sine + centeredUV.y * cosine) + pivot;
            }

            float2 GetMainFlowUV(float2 sourceUV, float4 textureST)
            {
                float2 rotatedSourceUV = RotateUV(sourceUV, _MainUVPivot.xy, _MainUVRotation);
                if (_MainFlowMode < 0.5)
                {
                    return GetFlowUV(rotatedSourceUV, textureST, _MainUVStretch, _MainUVSpeed);
                }

                float2 relativeUV = rotatedSourceUV - _MainFlowCenter.xy;
                float angle = atan2(relativeUV.y, relativeUV.x) * 0.15915494 + 0.5;
                float radius = length(relativeUV) * 2.0;
                float2 polarUV = float2(angle, radius) * _MainPolarStretch.xy + _MainPolarStretch.zw;
                float flowTime = _Time.y * _MainPolarFlowSpeed;

                if (_MainFlowMode < 1.5)
                {
                    // Radial In: texture features travel from the outside toward the center.
                    polarUV.y += flowTime;
                }
                else if (_MainFlowMode < 2.5)
                {
                    // Radial Out: texture features travel from the center toward the outside.
                    polarUV.y -= flowTime;
                }
                else if (_MainFlowMode < 3.5)
                {
                    // Circular CW: texture features orbit clockwise around the flow center.
                    polarUV.x += flowTime;
                }
                else
                {
                    // Circular CCW: texture features orbit counter-clockwise around the flow center.
                    polarUV.x -= flowTime;
                }

                return polarUV * textureST.xy + textureST.zw;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 maskUV = GetFlowUV(IN.texcoord, _MaskTex_ST, _MaskUVStretch, _MaskUVSpeed);

                #ifdef _USE_CUSTOM_MAIN_TEX
                float2 mainUV = GetMainFlowUV(IN.texcoord, _CustomMainTex_ST);
                fixed4 mainColor = tex2D(_CustomMainTex, mainUV);
                #else
                float2 mainUV = GetMainFlowUV(IN.texcoord, _MainTex_ST);
                fixed4 mainColor = tex2D(_MainTex, mainUV);
                #endif

                fixed maskAlpha = tex2D(_MaskTex, maskUV).a;
                fixed4 color = mainColor * IN.color;
                color.a *= maskAlpha;

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
}

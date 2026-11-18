Shader "Custom/AlphaBlendFlowWithMaskSimple"
{
    Properties
    {
        // 主贴图
        [Header(Main Texture)]
        _MainTex ("Main Texture", 2D) = "white" {}
        _MainUVScale ("UV Scale", Vector) = (1, 1, 0, 0)
        _MainUVOffset ("UV Offset", Vector) = (0, 0, 0, 0)
        
        // 遮罩贴图
        [Header(Mask Settings)]
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _MaskUVScale ("UV Scale", Vector) = (1, 1, 0, 0)
        _MaskUVOffset ("UV Offset", Vector) = (0, 0, 0, 0)
        [Toggle]_UseMaskAlpha ("Use Mask Alpha", Float) = 0
        _MaskPower ("Mask Power", Range(0.1, 5)) = 1
        
        // 流动效果
        [Header(Flow Effect)]
        _FlowSpeed ("Flow Speed", Vector) = (0.1, 0, 0, 0)
        _FlowIntensity ("Flow Intensity", Range(0, 1)) = 0.1
        
        // 颜色调整
        [Header(Color Settings)]
        _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
        _Brightness ("Brightness", Range(0, 5)) = 1
        _Alpha ("Alpha", Range(0, 1)) = 1
    }
    
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv_main : TEXCOORD0;
                float2 uv_mask : TEXCOORD1;
                float4 color : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _MaskTex;
            float4 _MaskTex_ST;
            
            float2 _MainUVScale;
            float2 _MainUVOffset;
            float2 _MaskUVScale;
            float2 _MaskUVOffset;
            
            float2 _FlowSpeed;
            float _FlowIntensity;
            
            float4 _TintColor;
            float _Brightness;
            float _Alpha;
            float _UseMaskAlpha;
            float _MaskPower;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                
                // 主贴图UV
                float2 mainUV = v.uv * _MainUVScale * _MainTex_ST.xy + _MainUVOffset + _MainTex_ST.zw;
                
                // 遮罩贴图UV
                float2 maskUV = v.uv * _MaskUVScale * _MaskTex_ST.xy + _MaskUVOffset + _MaskTex_ST.zw;
                
                o.uv_main = mainUV;
                o.uv_mask = maskUV;
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // 流动偏移
                float2 flowOffset = _FlowSpeed * _Time.y;
                float2 mainUVWithFlow = i.uv_main + flowOffset * _FlowIntensity;
                
                // 采样贴图
                fixed4 mainColor = tex2D(_MainTex, mainUVWithFlow);
                fixed4 maskColor = tex2D(_MaskTex, i.uv_mask);
                
                // 计算遮罩值
                float maskValue;
                if (_UseMaskAlpha > 0.5)
                {
                    // 使用Alpha通道
                    maskValue = maskColor.a;
                }
                else
                {
                    // 使用RGB平均值作为黑白图的遮罩值
                    maskValue = (maskColor.r + maskColor.g + maskColor.b) / 3.0;
                }
                
                // 应用遮罩强度
                maskValue = pow(maskValue, _MaskPower);
                
                // 合并颜色
                fixed4 finalColor = mainColor * _TintColor * i.color;
                finalColor.rgb *= _Brightness;
                finalColor.a *= maskValue * _Alpha;
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Mobile/Particles/Alpha Blended"
}
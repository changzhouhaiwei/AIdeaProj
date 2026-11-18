Shader "Custom/AdditiveFlowWithMask"
{
    Properties
    {
        // 主贴图
        _MainTex ("Main Texture", 2D) = "white" {}
        // 主贴图UV调整
        _MainUVScale ("Main UV Scale", Vector) = (1, 1, 0, 0)
        _MainUVOffset ("Main UV Offset", Vector) = (0, 0, 0, 0)
        
        // 遮罩贴图
        _MaskTex ("Mask Texture", 2D) = "white" {}
        // 遮罩UV调整
        _MaskUVScale ("Mask UV Scale", Vector) = (1, 1, 0, 0)
        _MaskUVOffset ("Mask UV Offset", Vector) = (0, 0, 0, 0)
        
        // 遮罩参数
        [Toggle]_MaskUseAlpha ("Use Mask Alpha", Float) = 0
        [Enum(Red,0,Green,1,Blue,2,Alpha,3)] _MaskChannel ("Mask Channel", Float) = 0
        _MaskPower ("Mask Power", Range(0.1, 5)) = 1
        _MaskStrength ("Mask Strength", Range(0, 2)) = 1
        
        // 流动参数
        _FlowSpeed ("Flow Speed", Vector) = (0.1, 0.1, 0, 0)
        _FlowIntensity ("Flow Intensity", Range(0, 1)) = 0.1
        
        // 颜色调整
        _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
        _Brightness ("Brightness", Range(0, 5)) = 1
        _Alpha ("Alpha", Range(0, 1)) = 1
        
        // 透明度裁剪
        [Toggle]_UseAlphaClip ("Use Alpha Clip", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
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
        
        // Additive混合模式
        Blend SrcAlpha One
        Cull Off
        Lighting Off
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile __ _USEALPHACLIP_ON
            #pragma multi_compile __ _MASKUSEALPHA_ON
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;  // 添加顶点颜色
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv_main : TEXCOORD0;
                float2 uv_mask : TEXCOORD1;
                float4 vertexColor : TEXCOORD2;  // 添加顶点颜色传递
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            // 属性变量
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
            
            // 遮罩参数
            float _MaskPower;
            float _MaskStrength;
            float _MaskChannel;
            
            // Alpha裁剪
            float _Cutoff;
            
            // 顶点着色器
            v2f vert(appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.vertexColor = v.color;  // 传递顶点颜色
                
                // 计算主贴图UV
                float2 mainUV = v.uv * _MainUVScale * _MainTex_ST.xy + _MainUVOffset + _MainTex_ST.zw;
                
                // 计算遮罩UV
                float2 maskUV = v.uv * _MaskUVScale * _MaskTex_ST.xy + _MaskUVOffset + _MaskTex_ST.zw;
                
                o.uv_main = mainUV;
                o.uv_mask = maskUV;
                
                return o;
            }
            
            // 获取遮罩值
            float GetMaskValue(fixed4 maskColor)
            {
                float maskValue = 0;
                
                // 根据选择的通道获取遮罩值
                if (_MaskChannel < 0.5) // Red通道
                    maskValue = maskColor.r;
                else if (_MaskChannel < 1.5) // Green通道
                    maskValue = maskColor.g;
                else if (_MaskChannel < 2.5) // Blue通道
                    maskValue = maskColor.b;
                else // Alpha通道
                    maskValue = maskColor.a;
                
                // 应用遮罩强度和幂运算
                maskValue = maskValue * _MaskStrength;
                maskValue = pow(maskValue, _MaskPower);
                
                return saturate(maskValue);
            }
            
            // 片段着色器
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                // 计算流动偏移
                float2 flowOffset = _FlowSpeed * _Time.y;
                
                // 应用流动到主贴图UV
                float2 mainUVWithFlow = i.uv_main + flowOffset * _FlowIntensity;
                
                // 采样主贴图（带流动效果）
                fixed4 mainColor = tex2D(_MainTex, mainUVWithFlow);
                
                // 采样遮罩贴图
                fixed4 maskColor = tex2D(_MaskTex, i.uv_mask);
                
                // 获取遮罩值
                float maskValue = GetMaskValue(maskColor);
                
                // 合并颜色（主贴图 * 遮罩值 * 颜色调整 * 顶点颜色 * 亮度）
                fixed4 finalColor = mainColor * _TintColor * i.vertexColor;
                finalColor.rgb *= _Brightness;
                
                // 根据模式选择使用遮罩值还是遮罩贴图的alpha
                #ifdef _MASKUSEALPHA_ON
                    // 使用遮罩贴图的alpha通道
                    finalColor.a = mainColor.a * maskColor.a * _TintColor.a * i.vertexColor.a * _Alpha;
                #else
                    // 使用遮罩值（黑白图的亮度）
                    finalColor.a = mainColor.a * maskValue * _TintColor.a * i.vertexColor.a * _Alpha;
                #endif
                
                // Alpha裁剪
                #ifdef _USEALPHACLIP_ON
                clip(finalColor.a - _Cutoff);
                #endif
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Mobile/Particles/Additive"
}
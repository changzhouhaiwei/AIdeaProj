Shader "Custom/NewAdditiveFlowWithMask"
{
    Properties
    {
        // 主贴图
        _MainTex ("Main Texture", 2D) = "white" {}
        _MainUVScale ("Main UV Scale", Vector) = (1, 1, 0, 0)
        _MainUVOffset ("Main UV Offset", Vector) = (0, 0, 0, 0)
        
        // 遮罩贴图
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _MaskUVScale ("Mask UV Scale", Vector) = (1, 1, 0, 0)
        _MaskUVOffset ("Mask UV Offset", Vector) = (0, 0, 0, 0)
        
        // ========== 新增：世界坐标遮罩选项 ==========
        [Toggle] _MaskWorldSpace ("Mask Use World Space", Float) = 0
        _MaskWorldScale ("World Mask Scale", Range(0.01, 10)) = 1
        _MaskWorldOffset ("World Mask Offset", Vector) = (0, 0, 0, 0)
        // 可选：选择世界坐标平面 (0= XZ, 1= XY, 2= ZY)
        [Enum(XZ Plane,0,XY Plane,1,ZY Plane,2)] _MaskWorldPlane ("World Plane", Int) = 0
        // ===========================================
        
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
            // 新增关键字用于世界坐标遮罩
            #pragma multi_compile __ _MASKWORLDSPACE_ON
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv_main : TEXCOORD0;
                float2 uv_mask : TEXCOORD1;
                float4 vertexColor : TEXCOORD2;
                float3 worldPos : TEXCOORD3;   // 新增：传递世界坐标
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
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
            
            float _MaskPower;
            float _MaskStrength;
            float _MaskChannel;
            
            float _Cutoff;
            
            // 新增变量
            float _MaskWorldScale;
            float3 _MaskWorldOffset;
            int _MaskWorldPlane;
            
            v2f vert(appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.vertexColor = v.color;
                
                // 计算主贴图UV
                float2 mainUV = v.uv * _MainUVScale * _MainTex_ST.xy + _MainUVOffset + _MainTex_ST.zw;
                o.uv_main = mainUV;
                
                // 计算传统遮罩UV（UV坐标模式）
                float2 maskUV = v.uv * _MaskUVScale * _MaskTex_ST.xy + _MaskUVOffset + _MaskTex_ST.zw;
                o.uv_mask = maskUV;
                
                // 计算世界坐标（用于世界空间遮罩）
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                return o;
            }
            
            float GetMaskValue(fixed4 maskColor)
            {
                float maskValue = 0;
                if (_MaskChannel < 0.5) maskValue = maskColor.r;
                else if (_MaskChannel < 1.5) maskValue = maskColor.g;
                else if (_MaskChannel < 2.5) maskValue = maskColor.b;
                else maskValue = maskColor.a;
                
                maskValue = maskValue * _MaskStrength;
                maskValue = pow(maskValue, _MaskPower);
                return saturate(maskValue);
            }
            
            // 新增：根据世界坐标获取遮罩UV
            float2 GetWorldMaskUV(float3 worldPos)
            {
                float2 uv;
                // 根据选择的平面提取坐标分量
                if (_MaskWorldPlane == 0) // XZ平面
                {
                    uv = worldPos.xz;
                }
                else if (_MaskWorldPlane == 1) // XY平面
                {
                    uv = worldPos.xy;
                }
                else // ZY平面
                {
                    uv = worldPos.zy;
                }
                
                // 应用缩放和偏移
                uv = uv * _MaskWorldScale + _MaskWorldOffset.xy;
                return uv;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                // 流动偏移
                float2 flowOffset = _FlowSpeed * _Time.y;
                float2 mainUVWithFlow = i.uv_main + flowOffset * _FlowIntensity;
                fixed4 mainColor = tex2D(_MainTex, mainUVWithFlow);
                
                fixed4 maskColor;
                #ifdef _MASKWORLDSPACE_ON
                    // 使用世界坐标采样遮罩
                    float2 worldUV = GetWorldMaskUV(i.worldPos);
                    maskColor = tex2D(_MaskTex, worldUV);
                #else
                    // 使用传统UV坐标采样遮罩
                    maskColor = tex2D(_MaskTex, i.uv_mask);
                #endif
                
                float maskValue = GetMaskValue(maskColor);
                
                // 颜色计算
                fixed4 finalColor = mainColor * _TintColor * i.vertexColor;
                finalColor.rgb *= _Brightness;
                
                // Alpha 计算
                #ifdef _MASKUSEALPHA_ON
                    finalColor.a = mainColor.a * maskColor.a * _TintColor.a * i.vertexColor.a * _Alpha;
                #else
                    finalColor.a = mainColor.a * maskValue * _TintColor.a * i.vertexColor.a * _Alpha;
                #endif
                
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
// Shader created with Shader Forge v1.38 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.38;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,cgin:,lico:0,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,imps:True,rpth:0,vtps:0,hqsc:False,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,olmd:1,culm:2,bsrc:0,bdst:0,dpts:2,wrdp:False,dith:0,atcv:False,rfrpo:False,rfrpn:Refraction,coma:15,ufog:False,aust:True,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,stcl:False,atwp:False,stva:128,stmr:255,stmw:255,stcp:6,stps:0,stfa:0,stfz:0,ofsf:0,ofsu:0,f2p0:True,fnsp:True,fnfb:False,fsmp:False;n:type:ShaderForge.SFN_Final,id:9361,x:36276,y:32412,varname:node_9361,prsc:2|emission-540-OUT;n:type:ShaderForge.SFN_Tex2d,id:4354,x:33763,y:32403,ptovrint:False,ptlb:Turbulence Tex,ptin:_TurbulenceTex,varname:node_4354,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:3,isnm:False|UVIN-6622-OUT;n:type:ShaderForge.SFN_Add,id:6622,x:33556,y:32420,varname:node_6622,prsc:2|A-2579-UVOUT,B-7588-OUT;n:type:ShaderForge.SFN_TexCoord,id:2579,x:33295,y:32353,varname:node_2579,prsc:2,uv:0,uaff:False;n:type:ShaderForge.SFN_Multiply,id:7588,x:33333,y:32571,varname:node_7588,prsc:2|A-450-OUT,B-4590-T;n:type:ShaderForge.SFN_Append,id:450,x:33057,y:32506,varname:node_450,prsc:2|A-5595-OUT,B-9800-OUT;n:type:ShaderForge.SFN_Time,id:4590,x:33103,y:32696,varname:node_4590,prsc:2;n:type:ShaderForge.SFN_ValueProperty,id:5595,x:32864,y:32506,ptovrint:False,ptlb:Normal Tex panner x,ptin:_NormalTexpannerx,varname:node_5595,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_ValueProperty,id:9800,x:32864,y:32573,ptovrint:False,ptlb:Normal Tex panner y,ptin:_NormalTexpannery,varname:node_9800,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Multiply,id:3557,x:34116,y:32510,varname:node_3557,prsc:2|A-4354-R,B-8893-OUT;n:type:ShaderForge.SFN_ValueProperty,id:8893,x:33763,y:32624,ptovrint:False,ptlb:UV Effect Power,ptin:_UVEffectPower,varname:node_8893,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Add,id:2828,x:34318,y:32751,varname:node_2828,prsc:2|A-3557-OUT,B-3252-OUT;n:type:ShaderForge.SFN_Add,id:3252,x:34047,y:32869,varname:node_3252,prsc:2|A-7838-UVOUT,B-6684-OUT;n:type:ShaderForge.SFN_TexCoord,id:7838,x:33688,y:32774,varname:node_7838,prsc:2,uv:0,uaff:False;n:type:ShaderForge.SFN_Append,id:3334,x:33492,y:32977,varname:node_3334,prsc:2|A-5677-OUT,B-1865-OUT;n:type:ShaderForge.SFN_Multiply,id:6684,x:33769,y:33047,varname:node_6684,prsc:2|A-3334-OUT,B-8612-T;n:type:ShaderForge.SFN_Time,id:8612,x:33471,y:33198,varname:node_8612,prsc:2;n:type:ShaderForge.SFN_ValueProperty,id:5677,x:33242,y:32947,ptovrint:False,ptlb:Main Tex Panner_x,ptin:_MainTexPanner_x,varname:node_5677,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_ValueProperty,id:1865,x:33259,y:33047,ptovrint:False,ptlb:Main Tex Panner_y,ptin:_MainTexPanner_y,varname:node_1865,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Tex2d,id:2848,x:34522,y:32751,ptovrint:False,ptlb:Main Tex,ptin:_MainTex,varname:node_2848,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-2828-OUT;n:type:ShaderForge.SFN_Multiply,id:5276,x:34815,y:32646,varname:node_5276,prsc:2|A-6162-OUT,B-2848-RGB;n:type:ShaderForge.SFN_ValueProperty,id:6162,x:34538,y:32559,ptovrint:False,ptlb:Main Tex Brightness,ptin:_MainTexBrightness,varname:node_6162,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_Tex2d,id:2731,x:34605,y:33528,ptovrint:False,ptlb:Mask Tex,ptin:_MaskTex,varname:node_2731,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-2702-OUT;n:type:ShaderForge.SFN_Add,id:2702,x:34346,y:33669,varname:node_2702,prsc:2|A-4796-UVOUT,B-4878-OUT;n:type:ShaderForge.SFN_TexCoord,id:4796,x:34108,y:33592,varname:node_4796,prsc:2,uv:0,uaff:False;n:type:ShaderForge.SFN_SwitchProperty,id:4878,x:34108,y:33751,ptovrint:False,ptlb:Mask Tex Dynmatic,ptin:_MaskTexDynmatic,varname:node_4878,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,on:False|A-2495-OUT,B-9792-OUT;n:type:ShaderForge.SFN_Multiply,id:2495,x:33836,y:33749,varname:node_2495,prsc:2|A-7584-OUT,B-1492-T;n:type:ShaderForge.SFN_Append,id:9792,x:33942,y:33943,varname:node_9792,prsc:2|A-7138-Z,B-7138-W;n:type:ShaderForge.SFN_Time,id:1492,x:33540,y:33893,varname:node_1492,prsc:2;n:type:ShaderForge.SFN_ValueProperty,id:5909,x:33375,y:33750,ptovrint:False,ptlb:Mask Tex Panner x,ptin:_MaskTexPannerx,varname:node_5909,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_ValueProperty,id:2577,x:33365,y:33828,ptovrint:False,ptlb:Mask Tex Panner y,ptin:_MaskTexPannery,varname:node_2577,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:0;n:type:ShaderForge.SFN_Append,id:7584,x:33540,y:33750,varname:node_7584,prsc:2|A-5909-OUT,B-2577-OUT;n:type:ShaderForge.SFN_TexCoord,id:7138,x:33756,y:33883,varname:node_7138,prsc:2,uv:1,uaff:True;n:type:ShaderForge.SFN_Power,id:7655,x:35054,y:32507,varname:node_7655,prsc:2|VAL-5276-OUT,EXP-5043-OUT;n:type:ShaderForge.SFN_ValueProperty,id:5043,x:34789,y:32376,ptovrint:False,ptlb:Main Tex Contrast,ptin:_MainTexContrast,varname:node_5043,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,v1:1;n:type:ShaderForge.SFN_Multiply,id:7159,x:35340,y:32568,varname:node_7159,prsc:2|A-7655-OUT,B-4660-RGB;n:type:ShaderForge.SFN_Multiply,id:7147,x:35659,y:32448,varname:node_7147,prsc:2|A-9429-RGB,B-7159-OUT;n:type:ShaderForge.SFN_Color,id:9429,x:35259,y:32222,ptovrint:False,ptlb:Main Color,ptin:_MainColor,varname:node_9429,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:1,c3:1,c4:1;n:type:ShaderForge.SFN_VertexColor,id:4660,x:34967,y:32824,varname:node_4660,prsc:2;n:type:ShaderForge.SFN_Multiply,id:5803,x:35296,y:33042,varname:node_5803,prsc:2|A-4660-A,B-8625-OUT;n:type:ShaderForge.SFN_Multiply,id:5493,x:35718,y:32673,varname:node_5493,prsc:2|A-9429-A,B-5803-OUT;n:type:ShaderForge.SFN_Multiply,id:8625,x:34953,y:33067,varname:node_8625,prsc:2|A-2848-A,B-2731-R;n:type:ShaderForge.SFN_Multiply,id:540,x:35948,y:32553,varname:node_540,prsc:2|A-7147-OUT,B-5493-OUT;proporder:9429-2848-6162-5677-1865-5043-2731-5909-2577-4878-4354-8893-5595-9800;pass:END;sub:END;*/

Shader "der Forge/UV_effect_base_Add" {
    Properties {
        _MainColor ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Main Tex", 2D) = "white" {}
        _MainTexBrightness ("Main Tex Brightness", Float ) = 1
        _MainTexPanner_x ("Main Tex Panner_x", Float ) = 0
        _MainTexPanner_y ("Main Tex Panner_y", Float ) = 0
        _MainTexContrast ("Main Tex Contrast", Float ) = 1
        _MaskTex ("Mask Tex", 2D) = "white" {}
        _MaskTexPannerx ("Mask Tex Panner x", Float ) = 0
        _MaskTexPannery ("Mask Tex Panner y", Float ) = 0
        [MaterialToggle] _MaskTexDynmatic ("Mask Tex Dynmatic", Float ) = 0
        _TurbulenceTex ("Turbulence Tex", 2D) = "bump" {}
        _UVEffectPower ("UV Effect Power", Float ) = 0
        _NormalTexpannerx ("Normal Tex panner x", Float ) = 0
        _NormalTexpannery ("Normal Tex panner y", Float ) = 0
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend One One
            Cull Off
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            // #pragma multi_compile_fwdbase
            // #pragma only_renderers d3d9 d3d11 glcore gles gles3 metal d3d11_9x xboxone ps4 psp2 n3ds wiiu 
            #pragma target 2.0
            uniform sampler2D _TurbulenceTex; uniform float4 _TurbulenceTex_ST;
            uniform float _NormalTexpannerx;
            uniform float _NormalTexpannery;
            uniform float _UVEffectPower;
            uniform float _MainTexPanner_x;
            uniform float _MainTexPanner_y;
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform float _MainTexBrightness;
            uniform sampler2D _MaskTex; uniform float4 _MaskTex_ST;
            uniform fixed _MaskTexDynmatic;
            uniform float _MaskTexPannerx;
            uniform float _MaskTexPannery;
            uniform float _MainTexContrast;
            uniform float4 _MainColor;
            
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float4 texcoord1 : TEXCOORD1;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 vertexColor : COLOR;
                
				//-------------------add----------------------
				float3 vpos : TEXCOORD2;
				//-------------------add----------------------
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                //-------------------add----------------------
				o.vpos = v.vertex.xyz;
				//-------------------add----------------------
                
                o.uv0 = v.texcoord0;
                o.uv1 = v.texcoord1;
                o.vertexColor = v.vertexColor;
                o.pos = UnityObjectToClipPos( v.vertex );
                return o;
            }
            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
                float isFrontFace = ( facing >= 0 ? 1 : 0 );
                float faceSign = ( facing >= 0 ? 1 : -1 );
////// Lighting:
////// Emissive:
                float4 node_4590 = _Time;
                float2 node_6622 = (i.uv0+(float2(_NormalTexpannerx,_NormalTexpannery)*node_4590.g));
                float4 _TurbulenceTex_var = tex2D(_TurbulenceTex,TRANSFORM_TEX(node_6622, _TurbulenceTex));
                float4 node_8612 = _Time;
                float2 node_2828 = ((_TurbulenceTex_var.r*_UVEffectPower)+(i.uv0+(float2(_MainTexPanner_x,_MainTexPanner_y)*node_8612.g)));
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(node_2828, _MainTex));
                float4 node_1492 = _Time;
                float2 node_2702 = (i.uv0+lerp( (float2(_MaskTexPannerx,_MaskTexPannery)*node_1492.g), float2(i.uv1.b,i.uv1.a), _MaskTexDynmatic ));
                float4 _MaskTex_var = tex2D(_MaskTex,TRANSFORM_TEX(node_2702, _MaskTex));
                float3 emissive = ((_MainColor.rgb*(pow((_MainTexBrightness*_MainTex_var.rgb),_MainTexContrast)*i.vertexColor.rgb))*(_MainColor.a*(i.vertexColor.a*(_MainTex_var.a*_MaskTex_var.r))));
                float4 finalColor = fixed4(emissive,1);
                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
//    CustomEditor "ShaderForgeMaterialInspector"
}

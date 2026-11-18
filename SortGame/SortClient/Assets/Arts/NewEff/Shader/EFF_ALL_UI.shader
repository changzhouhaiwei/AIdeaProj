// Upgrade NOTE: upgraded instancing buffer 'EffectEFF_ALL_UI' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Effect/EFF_ALL_UI"
{
	Properties
	{
		[Header(BaseMode)][Enum(UnityEngine.Rendering.CullMode)]_Float0("剔除模式", Float) = 0
		[Enum(Add,1,Alpha,10)]_Float1("渲染模式", Float) = 1
		[Enum(Less of Equal,4,Always,8)]_Float2("ZTest", Float) = 4
		[Enum(ON,1,OFF,0)]_Float3("ZWrite", Float) = 0
		[Header(MainTex)]_MainTex("主贴图", 2D) = "white" {}
		_Float21("软粒子Soft", Range( 0 , 10)) = 0
		[Toggle(_KEYWORD1_ON)] _Keyword1("U_Clamp", Float) = 0
		[Toggle(_KEYWORD2_ON)] _Keyword2("V_Clamp", Float) = 0
		[HDR]_Color0("颜色", Color) = (1,1,1,1)
		[Toggle(_CUSTOMDATA1_ON)] _CustomData1("贴图Alpha", Float) = 0
		[Toggle(_CUSTOMDATA_ON)] _CustomData("粒子控制Custom", Float) = 0
		_Float13("主贴图旋转", Range( 0 , 360)) = 0
		_MainTex_U("主纹理_U", Float) = 0
		_MainTex_V("主纹理_V", Float) = 0
		[Header(MaskTex)]_TextureSample1("遮罩贴图", 2D) = "white" {}
		_Float16("遮罩贴图旋转", Range( 0 , 360)) = 0
		_Mask_U("遮罩_U", Float) = 0
		_Mask_V("遮罩_V", Float) = 0
		[Header(NoiseTex)]_TextureSample2("扰动贴图", 2D) = "white" {}
		_Float19("扰动贴图旋转", Range( 0 , 360)) = 0
		_Float6("扰动强度", Range( 0 , 1)) = 0
		_Float5("扰动_V", Float) = 0
		_Float4("扰动_U", Float) = 0
		[Header(DissolveTex)]_TextureSample3("溶解贴图", 2D) = "white" {}
		[Toggle(_KEYWORD0_ON)] _Keyword0("溶解Custom", Float) = 0
		_Float18("溶解贴图旋转", Range( 0 , 360)) = 0
		_Float10("溶解软硬", Range( 0 , 1)) = 0
		_Float9("溶解", Range( 0 , 1)) = 0
		_Float11("溶解_U", Float) = 0
		_Float12("溶解_V", Float) = 0
		[Header(RampTex)]_TextureSample4("渐变贴图", 2D) = "white" {}
		_Float23("贴图旋转", Range( 0 , 360)) = 0
		_Float24("色相变化", Range( 0 , 1)) = 0
		_Float25("渐变_U", Float) = 0
		_Float26("渐变_V", Float) = 0
		_Stencil("_Stencil", Float) = 0
		_StencilOp("_StencilOp", Float) = 0
		_StencilComp("_StencilComp", Float) = 0
		_StencilReadMask("_StencilReadMask", Float) = 0
		_StencilWriteMask("_StencilWriteMask", Float) = 0
		_ColorMask("_ColorMask", Float) = 0
		[HideInInspector] _tex4coord3( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Custom"  "Queue" = "Transparent+0" "IsEmissive" = "true"  }
		Cull [_Float0]
		ZWrite [_Float3]
		ZTest [_Float2]
		Blend SrcAlpha [_Float1]
		
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma shader_feature _KEYWORD1_ON
		#pragma shader_feature _CUSTOMDATA_ON
		#pragma shader_feature _KEYWORD2_ON
		#pragma shader_feature _CUSTOMDATA1_ON
		#pragma shader_feature _KEYWORD0_ON
		#pragma surface surf Unlit keepalpha noshadow noambient novertexlights nolightmap  nodynlightmap nodirlightmap nofog nometa noforwardadd 
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float2 uv_texcoord;
			float4 uv3_tex4coord3;
			float4 vertexColor : COLOR;
			float4 screenPos;
		};

		uniform half _Float1;
		uniform half _Float0;
		uniform half _Float2;
		uniform half _Float3;
		uniform float _Stencil;
		uniform half _ColorMask;
		uniform half _StencilWriteMask;
		uniform half _StencilOp;
		uniform half _StencilComp;
		uniform half _StencilReadMask;
		uniform sampler2D _MainTex;
		uniform half _MainTex_U;
		uniform half _MainTex_V;
		uniform float4 _MainTex_ST;
		uniform half _Float13;
		uniform sampler2D _TextureSample2;
		uniform half _Float4;
		uniform half _Float5;
		uniform float4 _TextureSample2_ST;
		uniform half _Float19;
		uniform half _Float6;
		uniform sampler2D _TextureSample1;
		uniform half _Mask_U;
		uniform half _Mask_V;
		uniform float4 _TextureSample1_ST;
		uniform half _Float16;
		uniform sampler2D _TextureSample4;
		uniform half _Float25;
		uniform half _Float26;
		uniform float4 _TextureSample4_ST;
		uniform half _Float23;
		uniform half4 _Color0;
		uniform half _Float10;
		uniform sampler2D _TextureSample3;
		uniform half _Float11;
		uniform half _Float12;
		uniform float4 _TextureSample3_ST;
		uniform half _Float18;
		uniform half _Float9;
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform half _Float21;

		UNITY_INSTANCING_BUFFER_START(EffectEFF_ALL_UI)
			UNITY_DEFINE_INSTANCED_PROP(half, _Float24)
#define _Float24_arr EffectEFF_ALL_UI
		UNITY_INSTANCING_BUFFER_END(EffectEFF_ALL_UI)


		half3 HSVToRGB( half3 c )
		{
			half4 K = half4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
			half3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
			return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
		}


		half3 RGBToHSV(half3 c)
		{
			half4 K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
			half4 p = lerp( half4( c.bg, K.wz ), half4( c.gb, K.xy ), step( c.b, c.g ) );
			half4 q = lerp( half4( p.xyw, c.r ), half4( c.r, p.yzx ), step( p.x, c.r ) );
			half d = q.x - min( q.w, q.y );
			half e = 1.0e-10;
			return half3( abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
		}

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			half2 appendResult35 = (half2(_MainTex_U , _MainTex_V));
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float cos101 = cos( ( ( _Float13 * UNITY_PI ) / 180.0 ) );
			float sin101 = sin( ( ( _Float13 * UNITY_PI ) / 180.0 ) );
			half2 rotator101 = mul( uv_MainTex - float2( 0.5,0.5 ) , float2x2( cos101 , -sin101 , sin101 , cos101 )) + float2( 0.5,0.5 );
			half2 panner23 = ( 1.0 * _Time.y * appendResult35 + rotator101);
			half2 appendResult47 = (half2(_Float4 , _Float5));
			float2 uv_TextureSample2 = i.uv_texcoord * _TextureSample2_ST.xy + _TextureSample2_ST.zw;
			float cos122 = cos( ( ( _Float19 * UNITY_PI ) / 180.0 ) );
			float sin122 = sin( ( ( _Float19 * UNITY_PI ) / 180.0 ) );
			half2 rotator122 = mul( uv_TextureSample2 - float2( 0.5,0.5 ) , float2x2( cos122 , -sin122 , sin122 , cos122 )) + float2( 0.5,0.5 );
			half2 panner45 = ( 1.0 * _Time.y * appendResult47 + rotator122);
			half4 tex2DNode43 = tex2D( _TextureSample2, panner45 );
			half2 temp_cast_0 = (tex2DNode43.r).xx;
			half2 lerpResult54 = lerp( panner23 , temp_cast_0 , _Float6);
			half2 appendResult24 = (half2(i.uv3_tex4coord3.x , i.uv3_tex4coord3.y));
			half2 temp_cast_1 = (tex2DNode43.r).xx;
			half2 lerpResult69 = lerp( ( panner23 + appendResult24 ) , temp_cast_1 , _Float6);
			#ifdef _CUSTOMDATA_ON
				half2 staticSwitch41 = lerpResult69;
			#else
				half2 staticSwitch41 = lerpResult54;
			#endif
			half temp_output_124_0 = (staticSwitch41).x;
			#ifdef _KEYWORD1_ON
				half staticSwitch130 = saturate( temp_output_124_0 );
			#else
				half staticSwitch130 = temp_output_124_0;
			#endif
			half temp_output_125_0 = (staticSwitch41).y;
			#ifdef _KEYWORD2_ON
				half staticSwitch131 = saturate( temp_output_125_0 );
			#else
				half staticSwitch131 = temp_output_125_0;
			#endif
			half2 appendResult127 = (half2(staticSwitch130 , staticSwitch131));
			half2 MainTexCustom137 = appendResult127;
			half4 tex2DNode42 = tex2D( _MainTex, MainTexCustom137 );
			half2 appendResult36 = (half2(_Mask_U , _Mask_V));
			float2 uv_TextureSample1 = i.uv_texcoord * _TextureSample1_ST.xy + _TextureSample1_ST.zw;
			float cos107 = cos( ( ( _Float16 * UNITY_PI ) / 180.0 ) );
			float sin107 = sin( ( ( _Float16 * UNITY_PI ) / 180.0 ) );
			half2 rotator107 = mul( uv_TextureSample1 - float2( 0.5,0.5 ) , float2x2( cos107 , -sin107 , sin107 , cos107 )) + float2( 0.5,0.5 );
			half2 panner18 = ( 1.0 * _Time.y * appendResult36 + rotator107);
			half4 tex2DNode33 = tex2D( _TextureSample1, panner18 );
			half4 temp_output_17_0 = ( tex2DNode33 * tex2DNode33.a );
			half2 appendResult168 = (half2(_Float25 , _Float26));
			float2 uv_TextureSample4 = i.uv_texcoord * _TextureSample4_ST.xy + _TextureSample4_ST.zw;
			float cos155 = cos( ( ( _Float23 * UNITY_PI ) / 180.0 ) );
			float sin155 = sin( ( ( _Float23 * UNITY_PI ) / 180.0 ) );
			half2 rotator155 = mul( uv_TextureSample4 - float2( 0.5,0.5 ) , float2x2( cos155 , -sin155 , sin155 , cos155 )) + float2( 0.5,0.5 );
			half2 panner167 = ( 1.0 * _Time.y * appendResult168 + rotator155);
			half3 hsvTorgb163 = RGBToHSV( tex2D( _TextureSample4, panner167 ).rgb );
			half _Float24_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float24_arr, _Float24);
			half3 hsvTorgb164 = HSVToRGB( half3(( hsvTorgb163.x + _Float24_Instance ),hsvTorgb163.y,hsvTorgb163.z) );
			half3 Gam161 = hsvTorgb164;
			half4 Color139 = ( tex2DNode42 * temp_output_17_0 * i.vertexColor * half4( Gam161 , 0.0 ) * _Color0 );
			o.Emission = Color139.rgb;
			#ifdef _CUSTOMDATA1_ON
				half staticSwitch34 = 1.0;
			#else
				half staticSwitch34 = 0.0;
			#endif
			half lerpResult20 = lerp( tex2DNode42.r , tex2DNode42.a , staticSwitch34);
			half2 appendResult89 = (half2(_Float11 , _Float12));
			float2 uv_TextureSample3 = i.uv_texcoord * _TextureSample3_ST.xy + _TextureSample3_ST.zw;
			float cos112 = cos( ( ( _Float18 * UNITY_PI ) / 180.0 ) );
			float sin112 = sin( ( ( _Float18 * UNITY_PI ) / 180.0 ) );
			half2 rotator112 = mul( uv_TextureSample3 - float2( 0.5,0.5 ) , float2x2( cos112 , -sin112 , sin112 , cos112 )) + float2( 0.5,0.5 );
			half2 panner88 = ( 1.0 * _Time.y * appendResult89 + rotator112);
			#ifdef _KEYWORD0_ON
				half staticSwitch98 = i.uv3_tex4coord3.z;
			#else
				half staticSwitch98 = _Float9;
			#endif
			half smoothstepResult77 = smoothstep( 0.0 , _Float10 , ( tex2D( _TextureSample3, panner88 ).r + 1.0 + ( -2.0 * staticSwitch98 ) ));
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			half4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float screenDepth143 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			half distanceDepth143 = abs( ( screenDepth143 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( _Float21 ) );
			half SoftParticle152 = saturate( distanceDepth143 );
			half4 Alpha142 = ( lerpResult20 * temp_output_17_0 * smoothstepResult77 * i.vertexColor.a * _Color0.a * tex2DNode42.a * SoftParticle152 );
			o.Alpha = Alpha142.r;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18800
165;68;3426;1364;2876;969;1;True;False
Node;AmplifyShaderEditor.CommentaryNode;10;-4229.501,-430.8463;Inherit;False;2465.924;683.8426;主贴图&CustomData;20;20;42;22;35;24;25;23;34;27;26;37;38;102;104;106;105;101;21;138;162;;0.2122642,0.6695322,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;70;-4225.246,-1031.914;Inherit;False;2820.625;581.9245;扰动;23;137;127;128;129;131;130;125;124;69;54;41;43;50;122;117;44;120;119;121;48;49;47;45;;1,0.504717,0.504717,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;102;-4215.652,-238.3023;Inherit;False;Property;_Float13;主贴图旋转;12;0;Create;False;0;0;0;False;0;False;0;0;0;360;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;117;-4209.215,-835.0496;Inherit;False;Property;_Float19;扰动贴图旋转;20;0;Create;False;0;0;0;False;0;False;0;0;0;360;0;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode;104;-3895.654,-238.3023;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;120;-3912.214,-704.0496;Inherit;False;Constant;_Float20;Float 14;24;0;Create;True;0;0;0;False;0;False;180;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode;119;-3889.214,-835.0496;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;106;-3917.654,-107.3024;Inherit;False;Constant;_Float14;Float 14;24;0;Create;True;0;0;0;False;0;False;180;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;21;-4028.889,-364.3303;Inherit;False;0;42;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;48;-3676.946,-732.8657;Inherit;False;Property;_Float4;扰动_U;23;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;105;-3703.654,-237.3023;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-3617.212,-104.2686;Inherit;False;Property;_MainTex_U;主纹理_U;13;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;38;-3624.291,-13.26847;Inherit;False;Property;_MainTex_V;主纹理_V;14;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;121;-3697.214,-834.0496;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;49;-3671.246,-661.8566;Inherit;False;Property;_Float5;扰动_V;22;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;44;-4204.811,-963.3518;Inherit;False;0;43;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RotatorNode;122;-3569.363,-958.6201;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode;101;-3572.655,-363.3022;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;35;-3429.214,-163.2686;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;22;-4183.106,38.86142;Inherit;False;2;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;47;-3524.49,-731.9355;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;45;-3320.125,-959.157;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;23;-3246.911,-363.0742;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;24;-3388.201,67.79346;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;43;-3126.976,-982.3801;Inherit;True;Property;_TextureSample2;扰动贴图;19;1;[Header];Create;False;1;NoiseTex;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;25;-2958.023,-214.5451;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;160;-4156.136,1859.024;Inherit;False;Property;_Float23;贴图旋转;32;0;Create;False;0;0;0;False;0;False;0;0;0;360;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;50;-3368.128,-565.6445;Inherit;False;Property;_Float6;扰动强度;21;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;69;-2804.949,-667.7253;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;54;-2780.204,-886.5844;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;159;-3889.136,1951.024;Inherit;False;Constant;_Float22;Float 22;31;0;Create;True;0;0;0;False;0;False;180;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode;157;-3877.136,1860.024;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;170;-3674.73,2070.24;Inherit;False;Property;_Float26;渐变_V;35;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;169;-3679.53,1993.64;Inherit;False;Property;_Float25;渐变_U;34;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;158;-3665.136,1860.024;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;100;-4231.603,866.258;Inherit;False;2232.535;693.4153;溶解;19;116;115;114;113;87;112;89;91;90;76;98;77;72;88;71;78;75;74;73;;0.8316493,0.240566,1,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;154;-3777.891,1729.923;Inherit;False;0;156;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;41;-2629.205,-833.7469;Inherit;False;Property;_CustomData;粒子控制Custom;11;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;168;-3515.33,1991.04;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode;155;-3543.109,1730.643;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode;125;-2346.715,-700.0998;Inherit;False;FLOAT;1;1;2;3;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;13;-4224.627,287.0021;Inherit;False;1883.278;549;遮罩;12;19;111;110;109;108;107;33;17;40;39;36;18;;1,0.6556604,0.6556604,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;116;-4222.831,1088.569;Inherit;False;Property;_Float18;溶解贴图旋转;26;0;Create;False;0;0;0;False;0;False;0;0;0;360;0;1;FLOAT;0
Node;AmplifyShaderEditor.SwizzleNode;124;-2344.456,-834.1193;Inherit;False;FLOAT;0;1;2;3;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode;114;-3902.826,1088.569;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;111;-4198.521,504.2084;Inherit;False;Property;_Float16;遮罩贴图旋转;16;0;Create;False;0;0;0;False;0;False;0;0;0;360;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;167;-3331.33,1731.04;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;129;-2176.049,-640.1345;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;115;-3924.825,1219.568;Inherit;False;Constant;_Float17;Float 14;24;0;Create;True;0;0;0;False;0;False;180;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;128;-2184.299,-755.9241;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PiNode;109;-3821.52,508.2084;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;110;-3843.52,639.2082;Inherit;False;Constant;_Float15;Float 14;24;0;Create;True;0;0;0;False;0;False;180;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;90;-3740.903,1255.051;Inherit;False;Property;_Float11;溶解_U;29;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;91;-3738.903,1362.051;Inherit;False;Property;_Float12;溶解_V;30;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;130;-2039.204,-835.7813;Inherit;False;Property;_Keyword1;U_Clamp;7;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;113;-3710.827,1089.569;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;87;-3912.702,941.0912;Inherit;False;0;71;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;156;-3129.309,1706.044;Inherit;True;Property;_TextureSample4;渐变贴图;31;1;[Header];Create;False;1;RampTex;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;131;-2028.773,-703.9681;Inherit;False;Property;_Keyword2;V_Clamp;8;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;151;-1936.246,863.1881;Inherit;False;1050.836;213.5605;软粒子;4;152;145;143;144;;0.6332347,0.495283,1,1;0;0
Node;AmplifyShaderEditor.RotatorNode;112;-3568.526,941.1709;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;40;-3631.134,697.4349;Inherit;False;Property;_Mask_V;遮罩_V;18;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;19;-4197.328,367.043;Inherit;False;0;33;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;39;-3629.134,606.4349;Inherit;False;Property;_Mask_U;遮罩_U;17;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;89;-3527.206,1190.802;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.WireNode;123;-3744.345,834.0526;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;144;-1903.341,943.8734;Inherit;False;Property;_Float21;软粒子Soft;6;0;Create;False;0;0;0;False;0;False;0;0;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.RGBToHSVNode;163;-2836.846,1705.519;Inherit;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;166;-3122.112,1907.019;Inherit;False;InstancedProperty;_Float24;色相变化;33;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;127;-1778.634,-728.0405;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;108;-3629.52,509.2084;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;76;-3432.061,1459.673;Inherit;False;Property;_Float9;溶解;28;0;Create;False;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;137;-1620.16,-726.9803;Inherit;False;MainTexCustom;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;75;-2906.061,1285.673;Inherit;False;Constant;_Float8;Float 8;18;0;Create;True;0;0;0;False;0;False;-2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;143;-1585.34,919.8734;Inherit;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;36;-3381.532,566.7357;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode;107;-3439.12,366.0084;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;165;-2599.111,1854.019;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;88;-3325.241,939.8497;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch;98;-3049.854,1390.841;Inherit;False;Property;_Keyword0;溶解Custom;25;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;71;-3060.229,916.258;Inherit;True;Property;_TextureSample3;溶解贴图;24;1;[Header];Create;False;1;DissolveTex;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;26;-2707.125,-34.46548;Inherit;False;Constant;A_R1;R;0;0;Create;False;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;145;-1324.639,919.8568;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;27;-2708.302,-112.1525;Inherit;False;Constant;A_R;A;1;0;Create;False;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;138;-2604.364,-320.875;Inherit;False;137;MainTexCustom;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;74;-2702.061,1315.673;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;18;-3165.708,365.5021;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.HSVToRGBNode;164;-2445.846,1727.519;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;73;-2748.061,1140.673;Inherit;False;Constant;_Float7;Float 7;18;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;42;-2262.214,-342.7119;Inherit;True;Property;_MainTex;主贴图;5;1;[Header];Create;False;1;MainTex;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;72;-2506.061,940.6317;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;152;-1145.621,919.2828;Inherit;False;SoftParticle;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;161;-2203.536,1728.924;Inherit;False;Gam;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;12;-2269.563,575.8616;Inherit;False;268.0439;269.0549;粒子颜色;1;16;;1,0.3726415,0.3726415,1;0;0
Node;AmplifyShaderEditor.SamplerNode;33;-2895.006,343.0021;Inherit;True;Property;_TextureSample1;遮罩贴图;15;1;[Header];Create;False;1;MaskTex;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;11;-2269.428,290.5002;Inherit;False;267.8679;261.011;Shader颜色;1;28;;1,0.8768814,0.4862745,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;78;-2489.952,1304.335;Inherit;False;Property;_Float10;溶解软硬;27;0;Create;False;0;0;0;False;0;False;0;0.01;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;34;-2399.124,-131.4655;Inherit;False;Property;_CustomData1;贴图Alpha;10;0;Create;False;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;28;-2219.426,340.5003;Inherit;False;Property;_Color0;颜色;9;1;[HDR];Create;False;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;16;-2219.563,625.8618;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-2521.348,341.7472;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;153;-1881.283,-425.2438;Inherit;False;152;SoftParticle;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;20;-1915.786,-269.7008;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;162;-2122.8,-45.56571;Inherit;False;161;Gam;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SmoothstepOpNode;77;-2209.068,943.931;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;14;-1629.091,87.34699;Inherit;False;7;7;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;15;-1538.474,-345.9386;Inherit;False;5;5;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;FLOAT3;0,0,0;False;4;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;142;-1401.291,87.3886;Inherit;False;Alpha;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;139;-1386.572,-346.1962;Inherit;False;Color;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;175;-795.9856,-767.9421;Inherit;False;Property;_StencilReadMask;_StencilReadMask;39;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;174;-798.9856,-856.9421;Inherit;False;Property;_StencilComp;_StencilComp;38;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;140;-1158.559,-418.8049;Inherit;False;139;Color;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;141;-1270.559,-173.8048;Inherit;True;142;Alpha;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;31;-1069.822,-929.6538;Inherit;False;Property;_Float1;渲染模式;2;1;[Enum];Create;False;0;2;Add;1;Alpha;10;0;True;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;32;-1072.859,-1010.104;Inherit;False;Property;_Float0;剔除模式;1;2;[Header];[Enum];Create;False;1;BaseMode;0;1;UnityEngine.Rendering.CullMode;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;30;-1070.822,-771.6542;Inherit;False;Property;_Float3;ZWrite;4;1;[Enum];Create;False;1;Mode;2;ON;1;OFF;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;29;-1072.822,-850.6538;Inherit;False;Property;_Float2;ZTest;3;1;[Enum];Create;False;0;2;Less of Equal;4;Always;8;0;True;0;False;4;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;172;-801.9856,-1001.942;Float;False;Property;_Stencil;_Stencil;36;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;176;-795.6608,-688.2542;Inherit;False;Property;_StencilWriteMask;_StencilWriteMask;40;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;173;-801.9856,-937.9421;Inherit;False;Property;_StencilOp;_StencilOp;37;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;177;-794.6608,-609.2545;Inherit;False;Property;_ColorMask;_ColorMask;41;0;Create;True;0;0;0;True;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;178;-743.697,-455.5766;Half;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Effect/EFF_ALL_UI;False;False;False;False;True;True;True;True;True;True;True;True;False;False;False;False;False;False;False;False;False;Off;2;True;30;0;True;29;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;False;0;False;Custom;;Transparent;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;True;31;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;True;32;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;104;0;102;0
WireConnection;119;0;117;0
WireConnection;105;0;104;0
WireConnection;105;1;106;0
WireConnection;121;0;119;0
WireConnection;121;1;120;0
WireConnection;122;0;44;0
WireConnection;122;2;121;0
WireConnection;101;0;21;0
WireConnection;101;2;105;0
WireConnection;35;0;37;0
WireConnection;35;1;38;0
WireConnection;47;0;48;0
WireConnection;47;1;49;0
WireConnection;45;0;122;0
WireConnection;45;2;47;0
WireConnection;23;0;101;0
WireConnection;23;2;35;0
WireConnection;24;0;22;1
WireConnection;24;1;22;2
WireConnection;43;1;45;0
WireConnection;25;0;23;0
WireConnection;25;1;24;0
WireConnection;69;0;25;0
WireConnection;69;1;43;1
WireConnection;69;2;50;0
WireConnection;54;0;23;0
WireConnection;54;1;43;1
WireConnection;54;2;50;0
WireConnection;157;0;160;0
WireConnection;158;0;157;0
WireConnection;158;1;159;0
WireConnection;41;1;54;0
WireConnection;41;0;69;0
WireConnection;168;0;169;0
WireConnection;168;1;170;0
WireConnection;155;0;154;0
WireConnection;155;2;158;0
WireConnection;125;0;41;0
WireConnection;124;0;41;0
WireConnection;114;0;116;0
WireConnection;167;0;155;0
WireConnection;167;2;168;0
WireConnection;129;0;125;0
WireConnection;128;0;124;0
WireConnection;109;0;111;0
WireConnection;130;1;124;0
WireConnection;130;0;128;0
WireConnection;113;0;114;0
WireConnection;113;1;115;0
WireConnection;156;1;167;0
WireConnection;131;1;125;0
WireConnection;131;0;129;0
WireConnection;112;0;87;0
WireConnection;112;2;113;0
WireConnection;89;0;90;0
WireConnection;89;1;91;0
WireConnection;123;0;22;3
WireConnection;163;0;156;0
WireConnection;127;0;130;0
WireConnection;127;1;131;0
WireConnection;108;0;109;0
WireConnection;108;1;110;0
WireConnection;137;0;127;0
WireConnection;143;0;144;0
WireConnection;36;0;39;0
WireConnection;36;1;40;0
WireConnection;107;0;19;0
WireConnection;107;2;108;0
WireConnection;165;0;163;1
WireConnection;165;1;166;0
WireConnection;88;0;112;0
WireConnection;88;2;89;0
WireConnection;98;1;76;0
WireConnection;98;0;123;0
WireConnection;71;1;88;0
WireConnection;145;0;143;0
WireConnection;74;0;75;0
WireConnection;74;1;98;0
WireConnection;18;0;107;0
WireConnection;18;2;36;0
WireConnection;164;0;165;0
WireConnection;164;1;163;2
WireConnection;164;2;163;3
WireConnection;42;1;138;0
WireConnection;72;0;71;1
WireConnection;72;1;73;0
WireConnection;72;2;74;0
WireConnection;152;0;145;0
WireConnection;161;0;164;0
WireConnection;33;1;18;0
WireConnection;34;1;27;0
WireConnection;34;0;26;0
WireConnection;17;0;33;0
WireConnection;17;1;33;4
WireConnection;20;0;42;1
WireConnection;20;1;42;4
WireConnection;20;2;34;0
WireConnection;77;0;72;0
WireConnection;77;2;78;0
WireConnection;14;0;20;0
WireConnection;14;1;17;0
WireConnection;14;2;77;0
WireConnection;14;3;16;4
WireConnection;14;4;28;4
WireConnection;14;5;42;4
WireConnection;14;6;153;0
WireConnection;15;0;42;0
WireConnection;15;1;17;0
WireConnection;15;2;16;0
WireConnection;15;3;162;0
WireConnection;15;4;28;0
WireConnection;142;0;14;0
WireConnection;139;0;15;0
WireConnection;178;2;140;0
WireConnection;178;9;141;0
ASEEND*/
//CHKSM=4B71215D84E122BE675A3C793F8A79243490BE02
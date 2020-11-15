// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/YangBinShader"
{
	Properties{
		_Tint("Tint", Color)=(1,1,1,1)//颜色不支持"white"{}语法
		_MainTex("Albedo",2D)="white"{}//纹理才支持
		[NoScaleOffset]_NormalTex("Normal",2D)="white"{}
		_Bumpiness("Bumpiness",Float) = 1
		[NoScaleOffset]_DetailNormalTex("Detail Normal",2D) = "white"{}
		_DetailBumpiness("DetailBumpiness",Float) = 1
		_Smoothness("Smoothness",Range(0,1)) = 0.5
		[Gamma]_Metallic("Metallic",Range(0,1)) = 0
		_DetailTex("Detail Texture",2D) = "gray"{}
	}
		CustomEditor "MyShaderGUI"
		SubShader
	{ 
		Pass
		{
		Tags{
		"LightMode"="ForwardBase" 
}
			CGPROGRAM
			#pragma target 3.0
			#pragma multi_compile _ VERTEXLIGHT_ON
		#pragma multi_compile _ SHADOWS_SCREEN
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature _MainTex_ON
			#define FORWARD_BASE_PASS
			#include "MyLightingInclude.cginc"
            ENDCG
        }
			Pass
		{
		
		Tags{
		"LightMode" = "ForwardAdd"
}

			Blend One One
			ZWrite Off
			CGPROGRAM
			#pragma target 3.0

			#pragma multi_compile_fwdadd_fullshadows
			//#pragma multi_compile DIRECTIONAL POINT SPOT POINT_COOKIE DIRECTIONAL_COOKIE
			#pragma vertex vert
			#pragma fragment frag
			#include "MyLightingInclude.cginc"
			ENDCG
		}Pass
		{
		Tags{
		"LightMode" = "ShadowCaster"
}

			CGPROGRAM
			#pragma target 3.0
		#pragma vertex vert
		#pragma fragment frag
		#include "MyShadow.cginc"
		ENDCG
	}
    }
	FallBack "Diffuse"
}

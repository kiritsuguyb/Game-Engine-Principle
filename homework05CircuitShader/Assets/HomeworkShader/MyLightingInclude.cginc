#if !defined(MY_LIGHTING_INCLUDE)
#define	MY_LIGHTING_INCLUDE
#include "UnityStandardUtils.cginc"
#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"


float4 _Tint,_CircuitTint;
sampler2D _MainTex, _NormalTex,_MSO, _Emission,_DetailMask, _DetailTex, _DetailNormalTex;
float2 _NormalTex_TexelSize;
float4 _MainTex_ST,_DetailTex_ST;
float _Smoothness,_Circuit;
float _Eminess;
float _Metallic;
float _Bumpiness;
float _DetailBumpiness;


struct a2v {
	float4 vertex:POSITION;
	float2 uv:TEXCOORD0;
	float3 normal:NORMAL;
	float4 tangent:TANGENT;
};
struct v2f {
	float4 pos:SV_POSITION;
	float4 uv:TEXCOORD0;
	float3 normal:TEXCOORD1;
	float3 worldPos:TEXCOORD2;
	float4 tangent:TEXCOORD3;
	SHADOW_COORDS(4)
#if defined(VERTEXLIGHT_ON)
	float3 vertexLightColor:TEXCOORD5;
#endif
};
//////////////////////////////////
void ComputeVertexLightColor(inout v2f i) {
#if defined(VERTEXLIGHT_ON)
	i.vertexLightColor = Shade4PointLights(
		unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
		unity_LightColor[0].rgb, unity_LightColor[1].rgb,
		unity_LightColor[2].rgb, unity_LightColor[3].rgb,
		unity_4LightAtten0, i.worldPos, i.normal
	);
#endif
}
///////////////////////////////////
v2f vert(a2v v)
{
	v.vertex = v.vertex*abs(cos(v.normal.x*v.normal.y*v.normal.z+_CosTime.w+_Time.y));
	v2f o;
	//unity 这一步默认会拆成M和VP，做两次矩阵乘法；
	//查看编译后的代码是个好习惯
	//没错而且clip空间并不是NDC，vertexshader完事之后紧接着会由硬件做一次齐次除法
	o.pos = UnityObjectToClipPos(v.vertex);
	TRANSFER_SHADOW(o);
	o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
	o.uv.zw = TRANSFORM_TEX(v.uv, _DetailTex);
	o.worldPos = mul(unity_ObjectToWorld, v.vertex);
	//拿到切线空间坐标轴
	o.normal = mul(transpose(unity_WorldToObject), v.normal);
	o.tangent = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
	ComputeVertexLightColor(o);
	return o;
}

UnityLight createLight(v2f i,float occ) {
	UnityLight light;
	//妙，需要multi compile配合
#if defined(POINT)||defined(SPOT)||defined(POINT_COOKIE)
	light.dir = -normalize(i.worldPos - _WorldSpaceLightPos0);
#else
	light.dir = _WorldSpaceLightPos0;
#endif
	UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos);
	light.color = _LightColor0 * attenuation;
	light.color *= occ;
	light.ndotl = DotClamped(i.normal, light.dir);
	return light;
}

float3 boxProjection(float3 position, float3 direction, float4 cubeMapPosition, float3 boxMin, float3 boxMax) {
	boxMin -= position;
	boxMax -= position;
#if UNITY_SPECCUBE_BOX_PROJECTION
	//分支条件只在一个物体的不同fragment走向不同分支的时候会导致性能加倍
	//如果一般情况下所有片元都走同一分支的话，那其实没关系
	UNITY_BRANCH
	if (cubeMapPosition.w > 0) {//cubeMapPosition.w其实是权重
		//下面三句话可以合成一句，用float3处理，但现在为了可读，先这样放着。
		float x = (direction.x > 0 ? boxMax.x : boxMin.x) / direction.x;
		float y = (direction.y > 0 ? boxMax.y : boxMin.y) / direction.y;
		float z = (direction.z > 0 ? boxMax.z : boxMin.z) / direction.z;
		float scalar = min(min(x, y), z);
		direction=direction * scalar + position - cubeMapPosition;
	}
#endif
	return direction;
}
/////////////////////////////////
UnityIndirect createIndirectLight(v2f i,float3 viewDir,float smoothness,float occ,float3 emission) {

	UnityIndirect indirectLight;
	indirectLight.diffuse = 0;
	indirectLight.specular = 0;

#if defined(VERTEXLIGHT_ON)
	indirectLight.diffuse = i.vertexLightColor;
#endif

#if defined(FORWARD_BASE_PASS)
	//如果想要应用于skybox，要把autogenerating打开，因为他要烘焙才能算出来环境光啊
	indirectLight.diffuse += max(0, ShadeSH9(float4(i.normal, 1)));
	float3 reflectDir = reflect(-viewDir, i.normal);
	//float roughness = 1 - _Smoothness;
	//roughness *= 1.7 - 0.7 * roughness;//粗糙度和mipmap采样不是线性的
	//这里要是不用采用LOD的宏的话好像会有artifact
	//float4 envSample= UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflectDir, roughness*UNITY_SPECCUBE_LOD_STEPS);
	//indirectLight.specular = DecodeHDR(envSample, unity_SpecCube0_HDR);
	Unity_GlossyEnvironmentData envData;
	envData.roughness = 1 - smoothness;
	//第一个反射探针的数据
	envData.reflUVW = boxProjection(i.worldPos, reflectDir,unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
	float3 probe0= Unity_GlossyEnvironment(
		UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, envData
	);
#if UNITY_SPECCUBE_BLENDING//如果多个反射探针
	float interpolator = unity_SpecCube0_BoxMin.w;
	UNITY_BRANCH
	if (interpolator < 0.99) {//且反射探针有一定的混合度，则混合
		//第二个反射探针的数据
		envData.reflUVW = boxProjection(i.worldPos, reflectDir, unity_SpecCube1_ProbePosition, unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);
		float3 probe1 = Unity_GlossyEnvironment(
			//第二个有可能不存在，先跟第一个混合一下
			UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), unity_SpecCube1_HDR, envData
		);
		indirectLight.specular = lerp(probe1, probe0, interpolator);
	}
	else {//没有混合度那就用第一个探针
		indirectLight.specular = probe0;
	}
#else
	indirectLight.specular = probe0;
#endif
#endif
	indirectLight.diffuse *= occ * occ;
	indirectLight.specular *= occ*occ;
	indirectLight.diffuse += saturate(emission);
	return indirectLight;
}
////////////////////////////////
void InitializeFragmentNormal(inout v2f i,float detailMask) {
	float3 mainNormal = UnpackScaleNormal(tex2D(_NormalTex, i.uv.xy),_Bumpiness);
	float3 detailNormal = UnpackScaleNormal(tex2D(_DetailNormalTex, i.uv.zw), _DetailBumpiness);
	//没有细节时细节就是mainNormal
	detailNormal=lerp(mainNormal, detailNormal,detailMask);
	//拿到切线空间法线
	float3 tangentSpaceNormal = normalize(BlendNormals(mainNormal,detailNormal));
	//拿到切线空间坐标轴
	float3 binormal = cross(i.normal, i.tangent.xyz)*i.tangent.w* unity_WorldTransformParams.w;
	//坐标变化
	i.normal = normalize(
		tangentSpaceNormal.x*i.tangent +
		tangentSpaceNormal.y*binormal +
		tangentSpaceNormal.z*i.normal
	);

}
float4 frag(v2f v) :SV_TARGET
{
	float detailMask = tex2D(_DetailMask,v.uv.xy).r;
	InitializeFragmentNormal(v, detailMask);
	float3 viewDir = normalize(_WorldSpaceCameraPos - v.worldPos);

#if defined(_MainTex_ON)
	float3 albedo = tex2D(_MainTex, v.uv.xy)*_Tint;
#else
	float3 albedo = 1;
#endif
	float detail = tex2D(_DetailTex, v.uv.zw);
	detail = (detail + 0.5)*(detail + 0.5) / 2;
	//没有细节的时候detail==0.5
	detail=lerp(0.5,detail,detailMask);
	albedo *= detail * unity_ColorSpaceDouble;

	float3 MSO=tex2D(_MSO,v.uv.xy).rag;
	float metallic=MSO.x;
	float smoothness=MSO.y;
	float occlusion=MSO.z;

	float3 emission = tex2D(_Emission, v.uv.xy);
	emission *=_Eminess;
	//自发光处理
	float x = v.worldPos.y*v.worldPos.x*v.worldPos.z;
	float3 randomColor= (0.3*cos(x+ _Time.w*50),0.5*sin(x + _Time.y * 15) ,0.5*sin(x + _Time.z*5 ) + sin(_Time.y)*0.5)+0.7;
	float3 electricColor = saturate(randomColor * _CircuitTint*_Circuit);
	electricColor *= electricColor;
	electricColor *= electricColor;
	electricColor = lerp(float3(0,0,0), electricColor, metallic);

	float3 specularTint;
	float oneMinusReflectivity;
	albedo += float4(electricColor, 1);
	albedo = DiffuseAndSpecularFromMetallic(
		albedo,metallic,/*out*/specularTint,/*out*/oneMinusReflectivity
	);
	//给Unity自带的PBS投喂一些参数，反照率，金属色（specularTint），oneMinusReflectivity是金属度的反面，金属度越高反射越高，albedo的强度越低。
	return UNITY_BRDF_PBS(albedo, specularTint, oneMinusReflectivity, smoothness,
		v.normal,viewDir,createLight(v,occlusion), createIndirectLight(v, viewDir, smoothness, occlusion, emission)
	);
}
#endif

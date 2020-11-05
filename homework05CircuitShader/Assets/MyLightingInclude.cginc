#if !defined(MY_LIGHTING_INCLUDE)
#define	MY_LIGHTING_INCLUDE
#include "UnityStandardUtils.cginc"
#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"


float4 _Tint;
sampler2D _MainTex, _NormalTex, _DetailTex, _DetailNormalTex;
float2 _NormalTex_TexelSize;
float4 _MainTex_ST,_DetailTex_ST;
float _Smoothness;
float _Metallic;
float _Bumpiness;
float _DetailBumpiness;


struct a2v {
	float4 position:POSITION;
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
#if defined(VERTEXLIGHT_ON)
	float3 vertexLightColor:TEXCOORD4;
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
v2f vert(a2v i)
{
	v2f o;
	//unity 这一步默认会拆成M和VP，做两次矩阵乘法；
	//查看编译后的代码是个好习惯
	//没错而且clip空间并不是NDC，vertexshader完事之后紧接着会由硬件做一次齐次除法
	o.pos = UnityObjectToClipPos(i.position);
	o.uv.xy = TRANSFORM_TEX(i.uv, _MainTex);
	o.uv.zw = TRANSFORM_TEX(i.uv, _DetailTex);
	o.worldPos = mul(unity_ObjectToWorld, i.position);
	//拿到切线空间坐标轴
	o.normal = mul(transpose(unity_WorldToObject), i.normal);
	o.tangent = float4(UnityObjectToWorldDir(i.tangent.xyz), i.tangent.w);
	ComputeVertexLightColor(o);
	return o;
}

UnityLight createLight(v2f i) {
	UnityLight light;
	//妙，需要multi compile配合
#if defined(POINT)||defined(SPOT)||defined(POINT_COOKIE)
	light.dir = -normalize(i.worldPos - _WorldSpaceLightPos0);
#else
	light.dir = _WorldSpaceLightPos0;
#endif
	UNITY_LIGHT_ATTENUATION(attenuation, 0, i.worldPos);
	light.color = _LightColor0 * attenuation;
	light.ndotl = DotClamped(i.normal, light.dir);
	return light;
}

/////////////////////////////////
UnityIndirect createIndirectLight(v2f i) {

	UnityIndirect indirectLight;
	indirectLight.diffuse = 0;
	indirectLight.specular = 0;

#if defined(VERTEXLIGHT_ON)
	indirectLight.diffuse = i.vertexLightColor;
#endif

#if defined(FORWARD_BASE_PASS)
	//如果想要应用于skybox，要把autogenerating打开，因为他要烘焙才能算出来环境光啊
	indirectLight.diffuse += max(0, ShadeSH9(float4(i.normal, 1)));
#endif

	return indirectLight;
}
////////////////////////////////
void InitializeFragmentNormal(inout v2f i) {
	float3 mainNormal = UnpackScaleNormal(tex2D(_NormalTex, i.uv.xy),_Bumpiness);
	float3 detailNormal = UnpackScaleNormal(tex2D(_DetailNormalTex, i.uv.zw), _DetailBumpiness);
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
	InitializeFragmentNormal(v);	
	float3 viewDir = normalize(_WorldSpaceCameraPos - v.worldPos);


	float3 albedo = tex2D(_MainTex, v.uv.xy)*_Tint;
	albedo *= tex2D(_DetailTex, v.uv.zw) * unity_ColorSpaceDouble;
	float3 specularTint;
	float oneMinusReflectivity;
	albedo = DiffuseAndSpecularFromMetallic(
		albedo,_Metallic,/*out*/specularTint,/*out*/oneMinusReflectivity
	);

	//给Unity自带的PBS投喂一些参数，反照率，金属色（specularTint），oneMinusReflectivity是金属度的反面，金属度越高反射越高，albedo的强度越低。
	return UNITY_BRDF_PBS(albedo, specularTint, oneMinusReflectivity, _Smoothness,
		v.normal,viewDir,createLight(v), createIndirectLight(v)
	);
}
#endif

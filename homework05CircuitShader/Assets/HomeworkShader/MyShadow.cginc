#if !defined(MY_SHADOW_INCLUDE)
#define	MY_SHADOW_INCLUDE
#include "UnityCG.cginc"


float4 _Tint;
sampler2D _MainTex, _NormalTex, _DetailTex, _DetailNormalTex;
float2 _NormalTex_TexelSize;
float4 _MainTex_ST,_DetailTex_ST;
float _Smoothness;
float _Metallic;
float _Bumpiness;
float _DetailBumpiness;


struct a2v {
	float4 vertex:POSITION;
	float3 normal:NORMAL;
};
struct v2f {
	float4 pos:SV_POSITION;
};
v2f vert(a2v v)
{
	v.vertex = v.vertex*abs(cos(v.normal.x*v.normal.y *v.normal.z + _CosTime.w + _Time.y));
	v2f o;
	//这里应用normalbias，就是会让影子变小的那个
	o.pos = UnityClipSpaceShadowCasterPos(v.vertex.xyz, v.normal);
	//这里应用深度bias，就是会造成摩尔纹的那个
	//不过这里的参数是值传，最后还是要保证o.pos能被赋值
	o.pos=UnityApplyLinearShadowBias(o.pos);
	return o;
}
float4 frag(v2f v) :SV_TARGET
{
	return 0;
}
#endif

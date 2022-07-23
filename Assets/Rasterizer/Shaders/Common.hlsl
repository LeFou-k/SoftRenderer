#ifndef COMMON_H
#define COMMON_H

struct Varyings
{
    float4 positionCS;
    float3 positionWS;
    float3 normalOS;
    float3 normalWS;
    float3 tangentWS;
    float3 bTangentWS;
    float2 uv;
};

struct ShadowVaryings
{
    float4 positionCS;
};

//Inputs:
//parameters:
float4 _ClearColor;
int2 _ScreenSize;
float4x4 _MatrixMVP;
float4x4 _MatrixM;
float4x4 _MatrixM_IT; //Invert and transpose model matrix
float4x4 _MatrixLightMVP;
float4x4 _MatrixLightVP;

float3 _CameraWS;
float3 _LightDirWS;
float4 _LightColor;
float4 _AmbientColor;

//PBR Parameters:
float3 albedo;
float metallic;
float roughness;
float ao;

Texture2D<float3> _Albedo;
SamplerState sampler_Albedo;
Texture2D<float3> _Normal;
SamplerState sampler_Normal;
Texture2D<float> _Metallic;
SamplerState sampler_Metallic;
Texture2D<float> _Roughness;
SamplerState sampler_Roughness;
Texture2D<float> _AO;
SamplerState sampler_AO;

//buffers:
StructuredBuffer<float3> _VertexBuffer;
StructuredBuffer<float3> _NormalBuffer;
StructuredBuffer<float4> _TangentBuffer;
StructuredBuffer<float2> _UVBuffer;
StructuredBuffer<uint3> _TriIndexBuffer;

//outputs:
RWStructuredBuffer<Varyings> _VaryingsBuffer;
RWStructuredBuffer<ShadowVaryings> _ShadowVaryingsBuffer;
RWTexture2D<float4> _ColorTexture;
RWTexture2D<uint> _DepthTexture;
RWTexture2D<uint> _RWShadowMapTexture;

//textures:
Texture2D<float4> _UVTexture;
SamplerState sampler_UVTexture;
Texture2D<float> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;

#define PI 3.1415926f
#define SHADOW_BIAS 0.01f

//possion Disk for soft shadow
static const float2 poissonDisk[16] = { 
    float2( -0.94201624, -0.39906216 ), 
    float2( 0.94558609, -0.76890725 ), 
    float2( -0.094184101, -0.92938870 ), 
    float2( 0.34495938, 0.29387760 ), 
    float2( -0.91588581, 0.45771432 ), 
    float2( -0.81544232, -0.87912464 ), 
    float2( -0.38277543, 0.27676845 ), 
    float2( 0.97484398, 0.75648379 ), 
    float2( 0.44323325, -0.97511554 ), 
    float2( 0.53742981, -0.47373420 ), 
    float2( -0.26496911, -0.41893023 ), 
    float2( 0.79197514, 0.19090188 ), 
    float2( -0.24188840, 0.99706507 ), 
    float2( -0.81409955, 0.91437590 ), 
    float2( 0.19984126, 0.78641367 ), 
    float2( 0.14383161, -0.14100790 ) 
};

static const float2 texelSize = rcp(float2(_ScreenSize));

float random(float3 seed, int i)
{
    float4 seed4 = float4(seed, i);
    float dotProduct = dot(seed4, float4(12.9898,78.233,45.164,94.673));
    return frac(sin(dotProduct) * 43758.5453);
}

float4 WorldPos2LightClipPos(float3 positionWS)
{
    float4 positionCS = mul(_MatrixLightVP, float4(positionWS, 1.0f));
    positionCS = positionCS * 0.5f + 0.5f;

    return positionCS;
}

bool FrustumClipping(float4 v[3])
{
    float4 v0 = v[0], v1 = v[1], v2 = v[2];
    float w0 = abs(v0.w), w1 = abs(v1.w), w2 = abs(v2.w);
    
    bool left = step(v0.x, -w0) && step(v1.x, -w1) && step(v2.x, -w2);
    bool right = step(w0, v0.x) && step(w1, v1.x) && step(w2, v2.x);
    bool down = step(v0.y, -w0) && step(v1.y, -w1) && step(v2.y, -w2);
    bool top = step(w0, v0.y) && step(w1, v1.y) && step(w2, v2.y);
    bool near = step(v0.z, -w0) && step(v1.z, -w1) && step(v2.z, -w2);
    bool far = step(w0, v0.z) && step(w1, v1.z) && step(w2, v2.z);
    
    return left || right || down || top || near || far;
}

float3 Get2DBarycentric(float x, float y, float4 v[3])
{
    float c1 = (x * (v[1].y - v[2].y) + (v[2].x - v[1].x) * y + v[1].x * v[2].y - v[2].x * v[1].y) / (v[0].x * (v[1].y - v[2].y) + (v[2].x - v[1].x) * v[0].y + v[1].x * v[2].y - v[2].x * v[1].y);
    float c2 = (x * (v[2].y - v[0].y) + (v[0].x - v[2].x) * y + v[2].x * v[0].y - v[0].x * v[2].y) / (v[1].x * (v[2].y - v[0].y) + (v[0].x - v[2].x) * v[1].y + v[2].x * v[0].y - v[0].x * v[2].y);
    float c3 = (x * (v[0].y - v[1].y) + (v[1].x - v[0].x) * y + v[0].x * v[1].y - v[1].x * v[0].y) / (v[2].x * (v[0].y - v[1].y) + (v[1].x - v[0].x) * v[2].y + v[0].x * v[1].y - v[1].x * v[0].y);                        
    return float3(c1, c2, c3);
}

#endif

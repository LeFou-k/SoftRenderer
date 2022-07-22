#ifndef COMMON_H
#define COMMON_H

struct Varyings
{
    float4 positionCS;
    float3 positionWS;
    float3 normalOS;
    float3 normalWS;
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

//buffers:
StructuredBuffer<float3> _VertexBuffer;
StructuredBuffer<float3> _NormalBuffer;
StructuredBuffer<float2> _UVBuffer;
StructuredBuffer<uint3> _TriIndexBuffer;



//outputs:
RWStructuredBuffer<Varyings> _VaryingsBuffer;
RWStructuredBuffer<ShadowVaryings> _ShadowVaryingsBuffer;

RWTexture2D<float4> _ColorTexture;
RWTexture2D<uint> _DepthTexture;

RWTexture2D<uint> _RWShadowMapTexture;

Texture2D<float> _ShadowMapTexture;
SamplerState sampler_ShadowMapTexture;

//textures:
Texture2D<float4> _UVTexture;
SamplerState sampler_UVTexture;

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

float VisibleCompare(float2 uv, float curDepth)
{
    float occluDepth = _ShadowMapTexture.SampleLevel(sampler_ShadowMapTexture, uv, 0);
    return step(occluDepth, curDepth + SHADOW_BIAS);
}

float VisibleCompare(float2 uv, float curDepth, out float occluDepth)
{
    occluDepth = _ShadowMapTexture.SampleLevel(sampler_ShadowMapTexture, uv, 0);
    return step(occluDepth, curDepth + SHADOW_BIAS);
}

float4 WorldPos2LightClipPos(float3 positionWS)
{
    float4 positionCS = mul(_MatrixLightVP, float4(positionWS, 1.0f));
    positionCS = positionCS * 0.5f + 0.5f;

    return positionCS;
}

float GetOccluDepth(float3 positionCS)
{
    const int radius = 20;
    const int BLOCK_SEARCH_SAMPLES = 16;
    int flag = 0;
    float cnt = 0.f, avgDepth = 0.f;
    for(int i = 0; i < BLOCK_SEARCH_SAMPLES; ++i)
    {
        float2 sampleCoord = float2(radius, radius) * poissonDisk[i] * texelSize + positionCS.xy;
        float d;
        float vis = VisibleCompare(sampleCoord.xy, positionCS.z, d);
        if(vis > 0.f)
        {
            cnt += 1.0f;
            flag = 1;
            avgDepth += d;
        }
    }

    if(flag == 1)
    {
        return avgDepth / cnt;
    }
    return 1.0f;
}

float GetHardShadow(float3 positionWS)
{
    float4 positionCS = WorldPos2LightClipPos(positionWS);
    return VisibleCompare(positionCS.xy, positionCS.z);
}

float GetPCF(float3 positionCS, float radius)
{
    const int SAMPLES = 16;
    float vis = 0.f;
    for(int i = 0; i < SAMPLES; ++i)
    {
        float2 sampleCoord = float2(radius, radius) * poissonDisk[i] * texelSize + positionCS.xy;
        vis += VisibleCompare(sampleCoord, positionCS.z);
    }

    return vis * rcp(SAMPLES);
}

float GetPCSS(float3 positionCS)
{
    float avgOccluDepth = GetOccluDepth(positionCS);
    const float lightWidth = 50.f;
    float radius = max(positionCS.z - avgOccluDepth, 0.f) / avgOccluDepth * lightWidth;
    return GetPCF(positionCS, radius);
}

float GetSoftShadow(float3 positionWS)
{
    float4 positionCS = WorldPos2LightClipPos(positionWS);
    return GetPCF(positionCS.xyz, 4);
    // return GetPCSS(positionCS.xyz);
    // return GetOccluDepth(positionCS);
}

//Fragment Shader:
float4 FragmentPhong(Varyings varyings)
{
    float4 textureColor = _UVTexture.SampleLevel(sampler_UVTexture, varyings.uv, 0);
    float4 ks = float4(0.7937f, 0.7937f, 0.7937f, 1.0f);

    float NoL = dot(varyings.normalWS, _LightDirWS);
    float4 diffuse = textureColor * _LightColor * saturate(NoL);
    float3 viewDir = normalize(_CameraWS - varyings.positionWS);
    float3 halfDir = normalize(viewDir + _LightDirWS);

    float NoH = dot(halfDir, varyings.normalWS);
    float4 specular = ks * _LightColor * pow(saturate(NoH), 50);

    return saturate(_AmbientColor + (diffuse + specular) * GetSoftShadow(varyings.positionWS));
    return saturate(_AmbientColor + (diffuse + specular));
    
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

void Rasterization(uint3 idx, float4 v[3])
{
    float4 v0 = v[0], v1 = v[1], v2 = v[2];
    float2 pointLB, pointRT;
    
    pointLB = min(v0.xy, min(v1.xy, v2.xy));
    pointRT = max(v0.xy, max(v1.xy, v2.xy));

    uint2 screenLB = clamp(floor(pointLB), uint2(0, 0), _ScreenSize);
    uint2 screenRT = clamp(ceil(pointRT), uint2(0, 0), _ScreenSize);

    Varyings vary0 = _VaryingsBuffer[idx.x];
    Varyings vary1 = _VaryingsBuffer[idx.y];
    Varyings vary2 = _VaryingsBuffer[idx.z];

    const float EPSILON = -0.0005f;

    for(uint y = screenLB.y; y < screenRT.y; ++y)
    {
        for(uint x = screenLB.x; x < screenRT.x; ++x)
        {
            float3 c = Get2DBarycentric(x, y, v);
            float alpha = c.x, beta = c.y, gamma = c.z;
            if(alpha < EPSILON || beta < EPSILON || gamma < EPSILON)
            {
                continue;
            }

            //Project correction: z in camera space
            float z = 1.0f / (alpha / v0.w + beta / v1.w + gamma / v2.w);
            float zp = (alpha * v0.z / v0.w + beta * v1.z / v1.w + gamma * v2.z / v2.w) * z;
            
            
            //calculate depth in light view spac
            uint preDepth;
            uint curDepth = asuint(zp);
            InterlockedMax(_DepthTexture[uint2(x, y)], curDepth, preDepth);
            if(curDepth > preDepth)
            {
                float2 uvP = (alpha * vary0.uv / v0.w + beta * vary1.uv / v1.w + gamma * vary2.uv / v2.w) * z;
                float3 normalP = (alpha * vary0.normalOS / v0.w + beta * vary1.normalOS / v1.w + gamma * vary2.normalOS / v2.w) * z;
                float3 worldPosP = (alpha * vary0.positionWS / v0.w + beta * vary1.positionWS / v1.w + gamma * vary2.positionWS / v2.w) * z;
                float3 worldNormalP = (alpha * vary0.normalWS / v0.w + beta * vary1.normalWS / v1.w + gamma * vary2.normalWS / v2.w) * z;

                Varyings varyings;
                varyings.uv = uvP;
                varyings.normalOS = normalP;
                varyings.positionWS = worldPosP;
                varyings.normalWS = worldNormalP;
                
                _ColorTexture[uint2(x, y)] = FragmentPhong(varyings);
            }
        }
    }
    
}

void ShadowRasterization(float4 v[3])
{
    float4 v0 = v[0], v1 = v[1], v2 = v[2];
    float2 pointLB, pointRT;
    
    pointLB = min(v0.xy, min(v1.xy, v2.xy));
    pointRT = max(v0.xy, max(v1.xy, v2.xy));

    uint2 screenLB = clamp(floor(pointLB), uint2(0, 0), _ScreenSize);
    uint2 screenRT = clamp(ceil(pointRT), uint2(0, 0), _ScreenSize);
    
    const float EPSILON = -0.0005f;

    for(uint y = screenLB.y; y < screenRT.y; ++y)
    {
        for(uint x = screenLB.x; x < screenRT.x; ++x)
        {
            float3 c = Get2DBarycentric(x, y, v);
            float alpha = c.x, beta = c.y, gamma = c.z;
            if(alpha < EPSILON || beta < EPSILON || gamma < EPSILON)
            {
                continue;
            }

            float zp = alpha * v0.z + beta * v1.z + gamma * v2.z;
            
            uint curDepth = asuint(zp);
            InterlockedMax(_RWShadowMapTexture[uint2(x, y)], curDepth);
        }
    }
}

#endif

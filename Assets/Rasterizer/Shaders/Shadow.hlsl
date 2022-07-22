#ifndef SHADOW
#define SHADOW

#include "Common.hlsl"

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
}
#endif
#ifndef RASTERIZATION
#define RASTERIZATION

#include "Common.hlsl"
#include "Fragment.hlsl"
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
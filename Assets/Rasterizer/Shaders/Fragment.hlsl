#ifndef FRAGMENT
#define FRAGMENT

#include "Common.hlsl"
#include "Shadow.hlsl"

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

    #if BLIN_PHONG
    return saturate(_AmbientColor + (diffuse + specular) * GetSoftShadow(varyings.positionWS));
    #elif PBR
    return 1.0f;
    #endif
}

#endif
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
    
    return saturate(_AmbientColor + (diffuse + specular) * GetSoftShadow(varyings.positionWS));
}

float3 fresnelSchlick(float cosTheta, float3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0); 
}

float DistributionGGX(float3 N, float3 H, float roughness)
{
    float a      = roughness*roughness;
    float a2     = a*a;
    float NdotH  = max(dot(N, H), 0.0);
    float NdotH2 = NdotH*NdotH;
	
    float num   = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;
	
    return num / denom;
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float num   = NdotV;
    float denom = NdotV * (1.0 - k) + k;
	
    return num / denom;
}

float GeometrySmith(float3 N, float3 V, float3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2  = GeometrySchlickGGX(NdotV, roughness);
    float ggx1  = GeometrySchlickGGX(NdotL, roughness);
	
    return ggx1 * ggx2;
}

float4 PBRShading(float3 positionWS, float3 normal, float3 albedo, float metallic, float roughness, float ao)
{
    float3 N = normalize(normal);
    float3 V = normalize(_CameraWS - positionWS);

    float3 F0 = float3(0.04f, 0.04f, 0.04f);
    F0 = lerp(F0, albedo, metallic);

    float3 L = normalize(_LightDirWS);
    float3 H = normalize(L + V);
    float NDF = DistributionGGX(N, H, roughness);
    float G = GeometrySmith(N, V, L, roughness);
    float3 F = fresnelSchlick(max(dot(H, V), 0.f), F0);

    float3 kS = F;
    float3 kD = 1.0f - kS;

    float3 numerator = NDF * G * F;
    float denominator = 4.0f * max(dot(N, V), 0.0) * max(dot(N, L), 0.0) + 0.0001f;
    float3 specular = numerator / denominator;

    float NdotL = max(dot(N, L), 0.f);

    float3 Lo = (kD * albedo / PI + specular) * _LightColor.rgb * NdotL;
    float3 ambient = float3(0.03f, 0.03f, 0.03f) * albedo * ao;
    
    return float4(ambient + Lo, 1.0f);
}

float3 UnpackNormalMap(float3 normal)
{
    return normal * 2.0f - 1.0f;
}

float4 Shadings(Varyings varyings)
{
    
#if PBR
    return PBRShading(varyings.positionWS, varyings.normalWS, albedo, metallic, roughness, ao);
    // return float4(0.1f, 0.2f, 0.9f, 1.0f);
#elif PBR_TEXTURE
    float3 _albedo = _Albedo.SampleLevel(sampler_Albedo, varyings.uv, 0).rgb;
    float3 _normal = UnpackNormalMap(_Normal.SampleLevel(sampler_Normal, varyings.uv, 0));
    float _metallic = _Metallic.SampleLevel(sampler_Metallic, varyings.uv, 0).r;
    float _roughness = _Roughness.SampleLevel(sampler_Roughness, varyings.uv, 0).r;
    float _ao = _AO.SampleLevel(sampler_AO, varyings.uv, 0).r;
    //transform normal to world space:
    float3x3 TBN = float3x3(varyings.tangentWS, varyings.bTangentWS, varyings.normalWS);
    TBN = transpose(TBN);
    float3 normalWS = mul(TBN, _normal);
    return PBRShading(varyings.positionWS, normalWS, _albedo, _metallic, _roughness, _ao);
    
#else
    return FragmentPhong(varyings);
#endif
}



#endif
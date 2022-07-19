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

//Inputs:
//parameters:
float4 _ClearColor;
int2 _ScreenSize;
float4x4 _MatrixMVP;
float4x4 _MatrixM;
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
RWTexture2D<float4> _ColorTexture;
RWTexture2D<float1> _DepthTexture;


float4 Fragment(Varyings varyings)
{
    return float4(1.0f, 1.0f, 1.0f, 1.0f);
}

#endif

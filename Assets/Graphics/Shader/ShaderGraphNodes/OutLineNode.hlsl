#ifndef OUTLINENODE_INCLUDED
#define OUTLINENODE_INCLUDED

#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#endif

#ifndef SHADERGRAPH_PREVIEW


float4 TransformHClipToNormalizedScreenPos(float4 PositionCS)
{
    float4 o = PositionCS * 0.5f;
    o.xy = float2(o.x, o.y * _ProjectionParams.x) + o.w;
    o.zw = PositionCS.zw;
    return o / o.w;
}

half SampleOffsetDepth(float3 PositionVS, float2 Offset)
{
    // カメラとの距離やカメラのFOVで見た目上の輪郭の太さが変わらないように、オフセットをViewSpaceで計算する
    float3 samplePositionVS = float3(PositionVS.xy + Offset, PositionVS.z);
    float4 samplePositionCS = TransformWViewToHClip(samplePositionVS);
    float4 samplePositionVP = TransformHClipToNormalizedScreenPos(samplePositionCS);
        
    float offsetDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, samplePositionVP).r;
    return offsetDepth;
}

#endif

void SobelFilter_float(float3 PositionWS, float Thickness, float OutLineThreshold, out float4 Out)
{
    #ifdef SHADERGRAPH_PREVIEW
    Out = float4(0.5, 0.5, 0.5, 1);

    #else
    float3x3 sobel_x = float3x3(-1, 0, 1, -2, 0, 2, -1, 0, 1);
    float3x3 sobel_y = float3x3(-1, -2, -1, 0, 0, 0, 1, 2, 1);

    float edgeX = 0;
    float edgeY = 0;

    float3 positionVS = TransformWorldToView(PositionWS);

    UNITY_UNROLL
    for (int x = -1; x <= 1; x++)
    {
        UNITY_UNROLL
        for (int y = -1; y <= 1; y++)
        {
            float2 offset = float2(x,y) * Thickness;
            half depth = SampleOffsetDepth(positionVS, offset);
            depth = LinearEyeDepth(depth, _ZBufferParams);
                
            float intensity = depth;
            edgeX += intensity * sobel_x[x + 1][y + 1];
            edgeY += intensity * sobel_y[x + 1][y + 1];
        }
    }

    // エッジの強度を計算
    float edgeStrength = length(float2(edgeX, edgeY));
    edgeStrength = step(OutLineThreshold, edgeStrength);
    Out = float4(edgeStrength, edgeStrength, edgeStrength, 1);
    #endif
}

#endif

#ifndef MYLIGHTNODE_INCLUDED
#define MYLIGHTNODE_INCLUDED

#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
#pragma multi_compile _ _SHADOWS_SOFT

#endif

void GetMainLightParams_float(float3 PositionWS, out half3 Direction, out half3 Color, out float DistanceAttenuation,
                              out half ShadowAttenuation)
{
    #if defined(SHADERGRAPH_PREVIEW)
    Direction = float3(0.5, 0.5, 0);
    Color = 1;
    DistanceAttenuation = 1;
    ShadowAttenuation = 1;
    
    #else
    float4 shadowCoord = TransformWorldToShadowCoord(PositionWS);
    Light mainLight = GetMainLight(shadowCoord);
    Direction = mainLight.direction;
    Color = mainLight.color;
    DistanceAttenuation = mainLight.distanceAttenuation;
    ShadowAttenuation = MainLightRealtimeShadow(shadowCoord);

    #endif
}

void GetAdditionalLight_float(float3 PositionWS, float3 Normal, out half3 Color)
{
    #ifdef SHADERGRAPH_PREVIEW
    Color = half3(0.5, 0.5, 0.5);
    
    #else
    uint lightCount = GetAdditionalLightsCount();

    for (uint lightIndex = 0u; lightIndex < lightCount; ++lightIndex)
    {
        //ライトの取得
        Light light = GetAdditionalLight(lightIndex, PositionWS);
        //ライティングの計算
        half3 lightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
        Color += LightingLambert(lightColor, light.direction, Normal);
    }

    #endif
}

void GetHalfVector_float(float3 ViewVector, out float3 HalfVector)
{
    #ifdef SHADERGRAPH_PREVIEW
    HalfVector = half3(0.5, 0.5, 0);
    #else
    HalfVector = normalize(_MainLightPosition + ViewVector);
    #endif
}

void GetReceiveShadow_float(float ShadowAlpha, float3 PositionWS, out half ShadowAttenuation)
{
    #ifdef SHADERGRAPH_PREVIEW
    ShadowAttenuation = 1.0;
    
    #else
    half4 shadowCoord = TransformWorldToShadowCoord(PositionWS);
    Light mainLight = GetMainLight(shadowCoord);
    half shadow = mainLight.shadowAttenuation;
    int pixelLightCount = GetAdditionalLightsCount();

    for (int i = 0; i < pixelLightCount; i++)
    {
        Light AddLight0 = GetAdditionalLight(i, PositionWS, 1);
        half shadow0 = AddLight0.shadowAttenuation;
        shadow *= shadow0;
    }

    ShadowAttenuation = shadow * ShadowAlpha;

    #endif
}



#endif

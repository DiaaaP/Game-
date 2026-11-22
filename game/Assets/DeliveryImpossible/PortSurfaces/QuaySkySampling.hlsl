#ifndef NORTH_QUAY_SKY_SAMPLING
#define NORTH_QUAY_SKY_SAMPLING

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

TEXTURE2D(_Panorama);
SAMPLER(sampler_Panorama);
float4 _Panorama_HDR;
float4 _HorizonColor;
float _SkyRotation;
float _SkyExposure;

float3 QuaySky(float3 direction)
{
    float angle = radians(_SkyRotation);
    float2 axis = float2(cos(angle), sin(angle));
    float longitude = atan2(dot(direction.xz, float2(axis.y, axis.x)),
                            dot(direction.xz, float2(axis.x, -axis.y)));
    float2 uv = float2(frac(0.5 - longitude * INV_TWO_PI), 1 - acos(clamp(direction.y, -1, 1)) * INV_PI);
    float3 cloud = DecodeHDREnvironment(SAMPLE_TEXTURE2D_LOD(_Panorama, sampler_Panorama, uv, 0), _Panorama_HDR);
    float blend = smoothstep(-0.022, 0.12, direction.y);
    return lerp(_HorizonColor.rgb, cloud * _SkyExposure * float3(0.88, 0.95, 1.02), blend);
}

#endif

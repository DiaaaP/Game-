Shader "North Quay/Harbor Sky"
{
    Properties
    {
        [NoScaleOffset] _Panorama ("Cloud Panorama", 2D) = "gray" {}
        _HorizonColor ("Horizon", Color) = (0.48,0.55,0.60,1)
        _SkyRotation ("Rotation", Float) = 105
        _SkyExposure ("Exposure", Float) = 0.42
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex SkyVertex
            #pragma fragment SkyFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "QuaySkySampling.hlsl"

            struct SkyPoint
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            SkyPoint SkyVertex(float4 positionOS : POSITION)
            {
                SkyPoint result;
                result.positionCS = TransformObjectToHClip(positionOS.xyz);
                result.direction = positionOS.xyz;
                return result;
            }

            half4 SkyFragment(SkyPoint input) : SV_Target
            {
                return half4(QuaySky(normalize(input.direction)), 1);
            }
            ENDHLSL
        }
    }
}

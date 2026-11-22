Shader "North Quay/Harbor Water"
{
    Properties
    {
        _Deep ("Deep Water", Color) = (0.035,0.09,0.105,1)
        [NoScaleOffset] _Panorama ("Cloud Panorama", 2D) = "gray" {}
        _HorizonColor ("Horizon", Color) = (0.48,0.55,0.60,1)
        _SkyRotation ("Rotation", Float) = 105
        _SkyExposure ("Exposure", Float) = 0.42
        _SunDirection ("Sun Direction", Vector) = (0.45,0.27,0.83,0)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex WaterVertex
            #pragma fragment WaterFragment
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "QuaySkySampling.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Deep;
                float4 _SunDirection;
            CBUFFER_END

            struct Input { float4 positionOS : POSITION; };
            struct WaterPoint
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            WaterPoint WaterVertex(Input input)
            {
                WaterPoint output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 WaterFragment(WaterPoint input) : SV_Target
            {
                float2 p = input.positionWS.xz;
                float time = _Time.y;
                float distanceToEye = distance(_WorldSpaceCameraPos, input.positionWS);
                float a = dot(p, float2(1.18, 0.56)) + time * 1.12;
                float b = dot(p, float2(-0.72, 1.64)) - time * 1.53;
                float c = dot(p, float2(2.8, 0.95)) + time * 2.1;
                float d = dot(p, float2(-0.4, 0.37)) - time * 0.58;
                float detail = 1 - smoothstep(0.35, 2.8, fwidth(c));
                float2 slope = float2(cos(a + sin(d)) * 0.105, cos(b) * 0.12);
                slope += float2(cos(c), sin(c - a)) * 0.082 * detail;
                slope += float2(sin(d), cos(d)) * 0.058;
                float3 normal = normalize(float3(slope.x, 1, slope.y));
                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 reflection = reflect(-view, normal);
                half fresnel = 0.10 + 0.90 * pow(1 - saturate(dot(normal, view)), 4);
                float3 sky = QuaySky(float3(reflection.x, max(0.04, abs(reflection.y)), reflection.z));
                float glint = pow(saturate(dot(reflection, normalize(_SunDirection.xyz))), 88);
                float ripples = 0.88 + 0.12 * sin(a + sin(b) * 0.6);
                float3 color = lerp(_Deep.rgb * ripples, sky * 0.80, fresnel);
                color += glint * float3(0.62, 0.33, 0.13) * lerp(0.35, 1, detail);
                return half4(MixFog(color, ComputeFogFactorZ0ToFar(distanceToEye)), 1);
            }
            ENDHLSL
        }
    }
}

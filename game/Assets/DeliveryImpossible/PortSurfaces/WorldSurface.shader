Shader "North Quay/World Surface"
{
    Properties
    {
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
        _TileSize ("Meters Per Tile", Float) = 2
        [Normal] _BumpMap ("Surface Relief", 2D) = "bump" {}
        _BumpScale ("Relief Strength", Range(0,1)) = 0.45
        _Smoothness ("Smoothness", Range(0,1)) = 0.3
        [HideInInspector] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex SurfaceVertex
            #pragma fragment SurfaceFragment
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _TileSize;
                half _BumpScale;
                half _Smoothness;
            CBUFFER_END
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            struct MeshInput
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct QuayVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            QuayVaryings SurfaceVertex(MeshInput input)
            {
                QuayVaryings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 SurfaceFragment(QuayVaryings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 uv = input.positionWS / max(0.01, _TileSize);
                half3 weights = pow(abs(n), 5);
                weights /= max(dot(weights, half3(1,1,1)), 0.0001);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.zy).rgb * weights.x +
                               SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xz).rgb * weights.y +
                               SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy).rgb * weights.z;
                half2 onX = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv.zy)).xy;
                half2 onY = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv.xz)).xy;
                half2 onZ = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv.xy)).xy;
                half3 relief = half3(0, onX.y, onX.x) * weights.x +
                               half3(onY.x, 0, onY.y) * weights.y +
                               half3(onZ.x, onZ.y, 0) * weights.z;
                n = normalize(n + _BumpScale * relief);
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half glossPower = lerp(14, 94, _Smoothness);
                Light main = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 light = max(SampleSH(n), half3(0.19,0.21,0.23)) +
                    main.color * (saturate(dot(n, main.direction)) * main.shadowAttenuation);
                half3 specular = main.color * main.shadowAttenuation * saturate(dot(n, main.direction)) *
                    pow(saturate(dot(n, normalize(main.direction + view))), glossPower);
                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                uint lightsCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightsCount)
                    Light localLight = GetAdditionalLight(lightIndex, input.positionWS);
                    half3 radiance = localLight.color * localLight.distanceAttenuation;
                    light += radiance * saturate(dot(n, localLight.direction));
                    specular += radiance * saturate(dot(n, localLight.direction)) *
                        pow(saturate(dot(n, normalize(localLight.direction + view))), glossPower);
                LIGHT_LOOP_END
                #endif
                float3 p = input.positionWS;
                half weathering = 0.93 + 0.07 * sin(p.x * 0.41 + p.z * 0.27) * sin(p.y * 0.33 - p.z * 0.56);
                half3 color = albedo * _BaseColor.rgb * light * weathering;
                color += specular * lerp(0.025, 0.21, _Smoothness);
                half fog = ComputeFogFactorZ0ToFar(distance(_WorldSpaceCameraPos, p));
                return half4(MixFog(color, fog), 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}

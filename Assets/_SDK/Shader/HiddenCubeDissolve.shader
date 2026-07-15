Shader "Custom/URP/HiddenCubeDissolve"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)

        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.5

        _ShadowColor("Shadow Color", Color) = (0.2,0.2,0.35,1)
        _ShadowStrength("Shadow Strength", Range(0,1)) = 1
        _ShadowThreshold("Shadow Threshold", Range(0,1)) = 0.5
        _ShadowSoftness("Shadow Softness", Range(0.001,1)) = 0.15

        _DissolveAmount("Dissolve Amount", Range(0,1)) = 0
        _NoiseScale("Noise Scale", Float) = 8
        _EdgeWidth("Edge Width", Range(0,0.15)) = 0.05
        _EdgeColor("Edge Color", Color) = (1,0.6,0,1)
        _EdgeEmission("Edge Emission", Float) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS      : SV_POSITION;
                float2 uv              : TEXCOORD0;
                float3 positionWS      : TEXCOORD1;
                float3 normalWS        : TEXCOORD2;
                float4 shadowCoord     : TEXCOORD3;
                float3 viewDirWS       : TEXCOORD4;
                float fogFactor        : TEXCOORD5;
                float3 positionOS      : TEXCOORD6;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _ShadowColor;
                float  _Metallic;
                float  _Smoothness;
                float  _ShadowStrength;
                float  _ShadowThreshold;
                float  _ShadowSoftness;
                float  _DissolveAmount;
                float  _NoiseScale;
                float  _EdgeWidth;
                float4 _EdgeColor;
                float  _EdgeEmission;
            CBUFFER_END

            // Simple 3D value noise (hash-based, no texture needed)
            float hash3D(float3 p)
            {
                p = frac(p * float3(443.8975, 397.2973, 491.1871));
                p += dot(p, p.yxz + 19.19);
                return frac((p.x + p.y) * p.z);
            }

            float noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep

                return lerp(
                    lerp(lerp(hash3D(i + float3(0,0,0)), hash3D(i + float3(1,0,0)), f.x),
                         lerp(hash3D(i + float3(0,1,0)), hash3D(i + float3(1,1,0)), f.x), f.y),
                    lerp(lerp(hash3D(i + float3(0,0,1)), hash3D(i + float3(1,0,1)), f.x),
                         lerp(hash3D(i + float3(0,1,1)), hash3D(i + float3(1,1,1)), f.x), f.y),
                    f.z);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInput = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = posInput.positionCS;
                OUT.positionWS = posInput.positionWS;
                OUT.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.shadowCoord = GetShadowCoord(posInput);
                OUT.viewDirWS = GetWorldSpaceViewDir(posInput.positionWS);
                OUT.fogFactor = ComputeFogFactor(posInput.positionCS.z);
                OUT.positionOS = IN.positionOS.xyz;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Procedural noise for dissolve pattern (object space for stable result)
                float noiseVal = noise3D(IN.positionOS * _NoiseScale);

                // Clip pixels based on dissolve amount
                float dissolve = _DissolveAmount;
                clip(noiseVal - dissolve);

                // Edge glow: bright emission near dissolve boundary
                // Suppress glow when dissolve hasn't started (avoids orange dots at noise=0)
                float distToBoundary = noiseVal - dissolve;
                float edgeMask = dissolve > 0.001 ? 1.0 - smoothstep(0, _EdgeWidth, distToBoundary) : 0;

                // Base surface
                float4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 albedo = baseTex.rgb * _BaseColor.rgb;
                float alpha = baseTex.a * _BaseColor.a;

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalize(IN.normalWS);
                inputData.viewDirectionWS = SafeNormalize(IN.viewDirWS);
                inputData.shadowCoord = IN.shadowCoord;
                inputData.fogCoord = IN.fogFactor;
                inputData.vertexLighting = half3(0,0,0);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = half4(1,1,1,1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.alpha = alpha;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = half3(0.2, 0.2, 0.2);
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0,0,1);
                surfaceData.emission = _EdgeColor.rgb * edgeMask * _EdgeEmission;
                surfaceData.occlusion = 1;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                // Shadow color tinting (same as LitShadowColor)
                Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
                float NdotL = saturate(dot(inputData.normalWS, mainLight.direction));
                float litAmount = NdotL * mainLight.shadowAttenuation;
                float shadowMask = 1.0 - smoothstep(
                    _ShadowThreshold - _ShadowSoftness,
                    _ShadowThreshold + _ShadowSoftness,
                    litAmount
                );
                float3 tinted = lerp(color.rgb, color.rgb * _ShadowColor.rgb, _ShadowStrength);
                color.rgb = lerp(color.rgb, tinted, shadowMask);

                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }
    }
}

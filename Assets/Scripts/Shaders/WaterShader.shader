Shader "Custom/WaterShader"
{
    Properties
    {
        _Depth ("Depth", Float) = 1
        _ShallowColor ("ShallowColor", Color) = (1,1,1,1)
        _DeepColor ("DeepColor", Color) = (0,0,0,1)

        _RefractionSpeed ("RefractionSpeed", Float) = 1
        _RefractionScale ("RefractionScale", Vector) = (1,1,0,0)
        _RefractionStrength ("RefractionStrength", Float) = 1
        _RefractionTexture ("RefractionTexture", 2D) = "white" {}

        _FoamSpeed ("FoamSpeed", Float) = 1
        _FoamScale ("FoamScale", Float) = 1
        _FoamAmount ("FoamAmount", Float) = 1
        _FoamCutoff ("FoamCutoff", Float) = 1
        _FoamColor ("FoamColor", Color) = (0,0,0,1)

        _NormalStrength ("NormalStrength", Float) = 1
        _Smoothness ("Smoothness", Float) = 0.5
        _Specular ("Specular", Float) = 0.2

        _WaveSpeed ("WaveSpeed", Float) = 0
        _WaveAmplitude ("WaveAmplitude", Float) = 0

        _fogOffset ("fogOffset", Float) = 0
        _fogDensity ("fogDensity", Float) = 0
        _fogColor ("fogColor", Color) = (1,1,1,1)
        _fogActive ("fogActive", Float) = 0
    }
    SubShader
    {
        Pass
        {
            Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "Lightmode"="UniversalForward" }
            LOD 200
            ZWrite Off
            Cull Off
            ZTest LEqual 
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            // This line defines the name of the vertex shader.
            #pragma vertex vert
            // This line defines the name of the fragment shader.
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _LIGHT_COOKIES
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Assets/Resources/Compute Shaders/FastNoiseLite.hlsl"

            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object space.
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float2 posRefractionUV : TEXCOORD4;
                float2 negRefractionUV : TEXCOORD5;
                float2 foamUV : TEXCOORD6;
                float3 worldTangent : TEXCOORD7;
                float3 worldBitangent : TEXCOORD8;
            };

            float _Depth;
            float4 _ShallowColor;
            float4 _DeepColor;

            float _RefractionSpeed;
            float4 _RefractionScale;
            float _RefractionStrength;
            TEXTURE2D(_RefractionTexture);
            SAMPLER(sampler_RefractionTexture);

            float _FoamSpeed;
            float _FoamScale;
            float _FoamAmount;
            float _FoamCutoff;
            float4 _FoamColor;

            float _NormalStrength;
            float _Smoothness;
            float _Specular;

            float _WaveSpeed;
            float _WaveAmplitude;

            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            float _fogOffset;
            float _fogDensity;
            float4 _fogColor;
            float _fogActive;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                worldPos.y += sin(worldPos.x * _WaveSpeed + _Time.z) * _WaveAmplitude;
                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                OUT.worldPos = worldPos;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);

                float2 refractionOffset1 = _Time.z * float2(_RefractionSpeed, _RefractionSpeed * 0.5);
                float2 refractionOffset2 = _Time.z * float2(-_RefractionSpeed * 0.3, -_RefractionSpeed * 0.4);
                OUT.posRefractionUV = worldPos.xz * _RefractionScale.xy + refractionOffset1;
                OUT.negRefractionUV = worldPos.xz * _RefractionScale.xy + refractionOffset2;

                float2 foamOffset = _Time.z * float2(_FoamSpeed, _FoamSpeed);
                OUT.foamUV = (worldPos.xz * _FoamScale) + foamOffset;

                OUT.worldTangent = TransformObjectToWorldDir(IN.tangentOS.xyz);
                OUT.worldBitangent = cross(OUT.worldNormal, OUT.worldTangent) * IN.tangentOS.w;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                OUT.shadowCoord = GetShadowCoord(vertexInput);
                #endif

                return OUT;
            }

            float2 unity_gradientNoise_dir(float2 p)
            {
                p = p % 289;
                float x = (34 * p.x + 1) * p.x % 289 + p.y;
                x = (34 * x + 1) * x % 289;
                x = frac(x / 41) * 2 - 1;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }

            float unity_gradientNoise(float2 p)
            {
                float2 ip = floor(p);
                float2 fp = frac(p);
                float d00 = dot(unity_gradientNoise_dir(ip), fp);
                float d01 = dot(unity_gradientNoise_dir(ip + float2(0, 1)), fp - float2(0, 1));
                float d10 = dot(unity_gradientNoise_dir(ip + float2(1, 0)), fp - float2(1, 0));
                float d11 = dot(unity_gradientNoise_dir(ip + float2(1, 1)), fp - float2(1, 1));
                fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
                return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x);
            }

            float FogFadeFactor(float fogOffset, float fogDensity, float3 worldPos) {
                float dst = distance(_WorldSpaceCameraPos, worldPos);
                float densityOffsetDst = max(0, dst - fogOffset) * fogDensity;
                float exp = -2.0 * densityOffsetDst * densityOffsetDst;
                float factor = saturate(pow(2, exp));

                return factor;
            }

            float DepthDifference(float2 uv, float3 worldPos) {
                float rawSceneDepth = SampleSceneDepth(uv);
                float eyeSceneDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                
                float eyeObjectDepth = -TransformWorldToView(worldPos).z;
                float depthDifference = distance(eyeSceneDepth, eyeObjectDepth);
                // float depthDifference = eyeSceneDepth - eyeObjectDepth;
                return depthDifference;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(IN.positionHCS);
                // float2 screenUV = IN.positionHCS.xy / IN.positionHCS.w;
                // screenUV = screenUV;
                // return float4(screenUV.x, screenUV.y, 0, 1);
                // float3 N = normalize(IN.worldNormal);
                // float3 T = normalize(IN.worldTangent);
                // float3 B = cross(N, T) * IN.tangentOS.w;
                // float3x3 TBN = float3x3(T, B, N);
                float3x3 TBN = float3x3(IN.worldTangent, IN.worldBitangent, IN.worldNormal);
                float aspectRatioCorrection = _ScreenParams.y / _ScreenParams.x;

                // Refraction
                float3 tRefractionPos = 2 * UnpackNormal(SAMPLE_TEXTURE2D(_RefractionTexture, sampler_RefractionTexture, IN.posRefractionUV)) - 1;
                float3 tRefractionNeg = 2 * UnpackNormal(SAMPLE_TEXTURE2D(_RefractionTexture, sampler_RefractionTexture, IN.negRefractionUV)) - 1;

                float3 wRefractionPos = normalize(mul(TBN, tRefractionPos));
                float3 wRefractionNeg = normalize(mul(TBN, tRefractionNeg));
                float3 blendedNormal = normalize(wRefractionPos * wRefractionNeg);
                float2 strengthAdjustedNormal = (_RefractionStrength * 0.05) * blendedNormal.xy;
                // strengthAdjustedNormal.x *= aspectRatioCorrection;
                // return float4(screenUV + strengthAdjustedNormal, 0, 1);

                float4 refractionColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV + strengthAdjustedNormal);

                // Color depth
                float depthDifference = DepthDifference(screenUV + strengthAdjustedNormal, IN.worldPos);

                float colorFadeFactor = pow(saturate(depthDifference / _Depth), 0.1);
                float4 waterColor = lerp(_ShallowColor, _DeepColor, colorFadeFactor);

                //Foam
                float foamDepthDifference = DepthDifference(screenUV, IN.worldPos);
                float foamFadeFactor = saturate(foamDepthDifference / _FoamAmount);
                float foam = foamFadeFactor * _FoamCutoff;
                
                float gradientNoise = lerp(0.25, 1, unity_gradientNoise(IN.foamUV));
                float steppedNoise = step(foam, gradientNoise) * _FoamColor.a;

                float4 foamWaterColor = lerp(waterColor, _FoamColor, steppedNoise);

                float4 color = lerp(refractionColor, foamWaterColor, colorFadeFactor);
                // color.a = saturate(depthDifference / 0.25);

                //Normals
                float normalStrength = lerp(0, _NormalStrength, 1 - steppedNoise);
                float3 normal = normalize(float3(blendedNormal * normalStrength + IN.worldNormal));
                if (dot(IN.worldNormal, normalize(_WorldSpaceCameraPos - IN.worldPos)) < 0)
                {
                    normal = -normal;
                }

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.worldPos;
                inputData.normalWS = normal;
                inputData.viewDirectionWS = normalize(_WorldSpaceCameraPos - IN.worldPos);
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.worldPos);
                inputData.fogCoord = 0;
                inputData.bakedGI = 0;
                inputData.vertexLighting = 0;
                inputData.normalizedScreenSpaceUV = screenUV;
                inputData.shadowMask = 1;

                SurfaceData surfaceData;
                surfaceData.albedo = color.rgb;
                surfaceData.alpha = color.a;
                surfaceData.metallic = 0.0;
                surfaceData.specular = _Specular;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = float3(0,1,0);
                surfaceData.emission = 0.0;
                surfaceData.occlusion = 1.0;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                float4 finalColor = UniversalFragmentPBR(inputData, surfaceData);

                if (_fogActive == 1)
                    finalColor.rgb = lerp(_fogColor.rgb, finalColor.rgb, FogFadeFactor(_fogOffset, _fogDensity, IN.worldPos));

                return finalColor;
            }
            ENDHLSL
        }
        // UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}

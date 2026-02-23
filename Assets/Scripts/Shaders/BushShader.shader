Shader "Custom/BushShader"
{
    Properties
    {
        _BushTexture ("BushTexture", 2D) = "white" {}
        _BaseColor ("BaseColor", Color) = (0,0,0,1)
        _WindDir ("WindDir", Vector) = (1,1,0,0)
        _WindStrength ("WindStrength", Float) = 0.3
    }
    SubShader
    {
        Pass
            {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthNormals" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Scripts/Shaders/Common/Helpers.hlsl"

            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object space.
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            TEXTURE2D(_BushTexture);
            SAMPLER(sampler_BushTexture);
            float2 _WindDir;
            float _WindStrength;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 local = IN.positionOS.xyz;

                float3 worldPos = TransformObjectToWorld(local);
                float2 windDir = normalize(_WindDir);
                float windStr = (GradientNoiseDeterministicfloat(worldPos.xz * 0.5 + _Time.z * 0.2, 1) * 2 - 1) * _WindStrength;

                float3 bend = float3(windDir.x, 0, windDir.y) * windStr;
                worldPos += bend;

                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.worldPos = worldPos;

                OUT.worldNormal = normalize(TransformObjectToWorldNormal(IN.normalOS));

                OUT.uv = IN.uv;

                return OUT;
            }

            float4 frag(Varyings IN, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_BushTexture, sampler_BushTexture, IN.uv);

                // Alpha clipping
                clip(tex.a - 0.5);

                float3 viewDirectionWS = normalize(_WorldSpaceCameraPos - IN.worldPos);

                float3 dx = ddx(IN.worldPos);
                float3 dy = ddy(IN.worldPos);
                float3 geoNormal = normalize(cross(dx, dy));
                float facingCamera = abs(dot(geoNormal, viewDirectionWS));
                float fade = smoothstep(0.1, 0.6, facingCamera);

                float2 screenUV = GetNormalizedScreenSpaceUV(IN.positionHCS);
                float2 pixel = screenUV * _ScreenParams.xy;
                float noise = lerp(0, 0.6, GradientNoiseDeterministicfloat(pixel, 1));
                clip(fade - noise);

                float3 normalWS = normalize(IN.worldNormal);
                return float4(NormalizeNormalPerPixel(normalWS), 0);
            }
            ENDHLSL
        }
        Pass
            {
            Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" "Lightmode"="UniversalForward"}
            LOD 200
            Cull Off
            ZWrite On
            ZTest LEqual

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
            #pragma multi_compile_instancing
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Assets/Scripts/Shaders/Common/Helpers.hlsl"

            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object space.
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float2 uv : TEXCOORD4;
            };

            TEXTURE2D(_BushTexture);
            SAMPLER(sampler_BushTexture);
            float4 _BaseColor;
            float2 _WindDir;
            float _WindStrength;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 local = IN.positionOS.xyz;

                float3 worldPos = TransformObjectToWorld(local);
                float2 windDir = normalize(_WindDir);
                float windStr = (GradientNoiseDeterministicfloat(worldPos.xz * 0.5 + _Time.z * 0.2, 1) * 2 - 1) * _WindStrength;

                float3 bend = float3(windDir.x, 0, windDir.y) * windStr;
                worldPos += bend;

                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                OUT.worldPos = worldPos;

                OUT.worldNormal = normalize(TransformObjectToWorldNormal(IN.normalOS));

                OUT.shadowCoord = TransformWorldToShadowCoord(worldPos);

                OUT.uv = IN.uv;

                return OUT;
            }

            float4 frag(Varyings IN, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_BushTexture, sampler_BushTexture, IN.uv);

                // Alpha clipping
                clip(tex.a - 0.5);
                
                float3 normalWS = normalize(IN.worldNormal);
                float3 viewDirectionWS = normalize(_WorldSpaceCameraPos - IN.worldPos);

                // Get geometry normal and calculate angle top camera
                float3 dx = ddx(IN.worldPos);
                float3 dy = ddy(IN.worldPos);
                float3 geoNormal = normalize(cross(dx, dy));
                float facingCamera = abs(dot(geoNormal, viewDirectionWS));
                float fade = smoothstep(0.1, 0.6, facingCamera);

                float2 screenUV = GetNormalizedScreenSpaceUV(IN.positionHCS);
                float2 pixel = screenUV * _ScreenParams.xy;
                float noise = distance(_WorldSpaceCameraPos, IN.worldPos) < 30 ? lerp(0, 0.6, GradientNoiseDeterministicfloat(pixel, 1)) : 0;

                clip(fade - noise);
                tex *= _BaseColor;

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.worldPos;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirectionWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.worldPos);
                inputData.fogCoord = 0;
                inputData.bakedGI = saturate(SampleSH(inputData.normalWS) + float3(0.2, 0.2, 0.2));
                inputData.vertexLighting = 0;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionHCS);
                inputData.shadowMask = 1;

                SurfaceData surfaceData;
                surfaceData.albedo = tex;
                surfaceData.alpha = 1;
                surfaceData.metallic = 0.0;
                surfaceData.specular = 0.5;
                surfaceData.smoothness = 0.2;
                surfaceData.normalTS = float3(0,0,1);
                surfaceData.emission = 0.0;
                surfaceData.occlusion = 1.0;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                float4 finalColor = UniversalFragmentPBR(inputData, surfaceData);

                return finalColor;
                // return float4(facingCamera, facingCamera, facingCamera, 1);
            }
            ENDHLSL
        }
    }
}
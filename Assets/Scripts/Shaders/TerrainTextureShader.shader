// This shader fills the mesh shape with a color predefined in the code.
Shader "Custom/TerrainTextureShader"
{
    // The properties block of the Unity shader. In this example this block is empty
    // because the output color is predefined in the fragment shader code.
    Properties
    { 
        _TextureArray("Texture Array", 2DArray) = "white" {}

        _Scale("Texture Scale", Float) = 0.2
        _SlopeBlendSharpness("Slope Blend Sharpness", Float) = 1
        _HeightBlendSharpness("Height Blend Sharpness", Float) = 1
    }

    // The SubShader block containing the Shader code.
    SubShader
    { 
        Pass
        {
            // SubShader Tags define when and under which conditions a SubShader block or
            // a pass is executed.
            Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" "Lightmode"="UniversalForward" }
            LOD 200
            ZWrite On
            ZTest LEqual    
            // The HLSL code block. Unity SRP uses the HLSL language.
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

            // The Core.hlsl file contains definitions of frequently used HLSL
            // macros and functions, and also contains #include references to other
            // HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

            // The structure definition defines which variables it contains.
            // This example uses the Attributes structure as an input structure in
            // the vertex shader.
            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object space.
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            // The vertex shader definition with properties defined in the Varyings
            // structure. The type of the vert function must match the type (struct)
            // that it returns.
            #define MAX_TEXTURES 20
            CBUFFER_START(UnityPerMaterial)
                float _Scale;
                int _LayerCount;
                
                float _UseHeightsArray[MAX_TEXTURES];
                float _HeightStartsArray[MAX_TEXTURES];
                float _HeightEndsArray[MAX_TEXTURES];
                float _UseSlopesArray[MAX_TEXTURES];
                float _SlopeStartsArray[MAX_TEXTURES];
                float _SlopeEndsArray[MAX_TEXTURES];

                float _LowestStartHeight;
                float _GreatestEndHeight;

                float _SlopeBlendSharpness;
                float _HeightBlendSharpness;
            CBUFFER_END
            TEXTURE2D_ARRAY(_TextureArray); SAMPLER(sampler_TextureArray);

            // struct TriangleVertex
            // {
            //     float3 position;
            //     float3 normal;
            // };

            // StructuredBuffer<TriangleVertex> _Triangles;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                OUT.worldPos = worldPos;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                OUT.shadowCoord = GetShadowCoord(vertexInput);
                #endif

                return OUT;
            }
            // Varyings vert(uint vertexID : SV_VertexID)
            // {
            //     Varyings OUT;
            //     TriangleVertex triangleVertex = _Triangles[vertexID];

            //     float3 worldPos = triangleVertex.position;
            //     OUT.positionHCS = TransformWorldToHClip(worldPos);
            //     OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
            //     OUT.worldPos = worldPos;
            //     OUT.worldNormal = normalize(triangleVertex.normal);

            //     VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
            //     #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            //     OUT.shadowCoord = GetShadowCoord(vertexInput);
            //     #endif

            //     return OUT;
            // }

            float4 TriplanarSample(TEXTURE2D_ARRAY_PARAM(textureArray, samplerArray), float3 worldPos, float3 normal, float scale, int layer) {
                float3 absNormal = abs(normal);
                float3 blend = absNormal / (absNormal.x + absNormal.y + absNormal.z);

                float2 xz = worldPos.yz * scale;
                float2 yz = worldPos.xz * scale;
                float2 xy = worldPos.xy * scale;

                float4 xTex = SAMPLE_TEXTURE2D_ARRAY(textureArray, samplerArray, xz, layer);
                float4 yTex = SAMPLE_TEXTURE2D_ARRAY(textureArray, samplerArray, yz, layer);
                float4 zTex = SAMPLE_TEXTURE2D_ARRAY(textureArray, samplerArray, xy, layer);

                return xTex * blend.x + yTex * blend.y + zTex * blend.z;
            }

            // The fragment shader definition.
            float4 frag(Varyings IN) : SV_Target
            {
                float4 albedo = float4(0, 0, 0, 0);
                // float weights[MAX_TEXTURES];
                float totalWeight = 0;
                float height = IN.worldPos.y;
                float3 normal = normalize(IN.worldNormal);
                float slope = 1 - dot(normal, float3(0, 1, 0));


                height = clamp(height, _LowestStartHeight, _GreatestEndHeight);

                // [unroll]
                // for(int i = 0; i < _LayerCount; i++) {
                //     float heightWeight = smoothstep(_HeightStartsArray[i], _HeightEndsArray[i], height) * (i != _LayerCount - 1 ? (1 - smoothstep(_HeightStartsArray[i+1], _HeightEndsArray[i+1], height)) : 1);
                //     float slopeWeight = smoothstep(_SlopeStartsArray[i], _SlopeEndsArray[i], slope) * (i != _LayerCount - 1 ? (1 - smoothstep(_SlopeStartsArray[i+1], _SlopeEndsArray[i+1], slope)) : 1);
                //     float weight = 0;
                //     if(_UseHeightsArray[i] == 1 || _UseSlopesArray[i] == 1) {
                //         if(_UseHeightsArray[i] == 1 && _UseSlopesArray[i] == 1) {
                //             weight = heightWeight * slopeWeight;
                //         }
                //         else if(_UseHeightsArray[i] == 1) {
                //             weight = heightWeight;
                //         }
                //         else {
                //             weight = slopeWeight;
                //         }
                //     }

                //     if(weight > 0.001) {
                //         float4 triplanarSample = TriplanarSample(_TextureArray, sampler_TextureArray, IN.worldPos, normal, _Scale, i);
                //         albedo += triplanarSample * weight;
                //         totalWeight += weight;
                //     }
                // }
                for(int i = 0; i < _LayerCount; i++) {
                    float heightMid = 0.5 * (_HeightStartsArray[i] + _HeightEndsArray[i]);
                    float heightHalfWidth = 0.5 * abs(_HeightEndsArray[i] - _HeightStartsArray[i]);
                    float heightWeight = saturate(1 - distance(height, heightMid) / heightHalfWidth);

                    float slopeMid = 0.5 * (_SlopeStartsArray[i] + _SlopeEndsArray[i]);
                    float slopeHalfWidth = 0.5 * abs(_SlopeEndsArray[i] - _SlopeStartsArray[i]);
                    float slopeWeight = saturate(1 - distance(slope, slopeMid) / slopeHalfWidth);

                    float weight = 0;
                    if(_UseHeightsArray[i] == 1 && _UseSlopesArray[i] == 1) {
                        weight = saturate(heightWeight) * saturate(slopeWeight);
                    }
                    else if(_UseHeightsArray[i] == 1) {
                        weight = saturate(heightWeight);
                    }
                    else if(_UseSlopesArray[i] == 1) {
                        weight = saturate(slopeWeight);
                    }
                    else {
                        weight = 0;
                    }

                    if(weight > 0.001) {
                        float4 triplanarSample = TriplanarSample(_TextureArray, sampler_TextureArray, IN.worldPos, normal, _Scale, i);
                        albedo += triplanarSample * weight;
                        totalWeight += weight;
                    }
                }

                albedo /= max(totalWeight, 0.001);

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.worldPos;
                inputData.normalWS = normalize(IN.worldNormal);
                inputData.viewDirectionWS = normalize(_WorldSpaceCameraPos - IN.worldPos);
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.worldPos);
                inputData.fogCoord = 0;
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.vertexLighting = 0;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionHCS);
                inputData.shadowMask = 1;

                SurfaceData surfaceData;
                surfaceData.albedo = albedo;
                surfaceData.alpha = 1.0;
                surfaceData.metallic = 0.0;
                surfaceData.specular = 0.0;
                surfaceData.smoothness = 0.1;
                surfaceData.normalTS = float3(0, 0, 1);
                surfaceData.emission = 0.0;
                surfaceData.occlusion = 1.0;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                float4 color = UniversalFragmentPBR(inputData, surfaceData);

                color.rgb = MixFog(color.rgb, IN.fogFactor);

                return color;

                // float3 finalTexture = float3(0, 0, 0);

                // // Get main directional light info
                // Light mainLight = GetMainLight();
                // #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                // IN.shadowCoord = IN.shadowCoord;
                // #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                // IN.shadowCoord = TransformWorldToShadowCoord(IN.worldPos);
                // #else
                // IN.shadowCoord = float4(0, 0, 0, 0);
                // #endif
                // float shadowAttenuation = MainLightRealtimeShadow(IN.shadowCoord);

                // // Calculate diffuse lighting
                // float NdotL = max(0, dot(normal, mainLight.direction));

                // // Optional ambient light (from Unity's shading environment)
                // // Brighter ambient for surface areas
                // float surfaceAmbient = 0.1;
                // float caveAmbient = 0.015;

                // // Threshold for cave vs surface
                // float caveThreshold = 0.0;
                // float ambientStrength = lerp(caveAmbient, surfaceAmbient, saturate((IN.worldPos.y - caveThreshold) * 0.02));

                // float shadowBoost = 1.0 - shadowAttenuation;
                // finalTexture += albedo.rgb * mainLight.color.rgb * ambientStrength * shadowBoost;

                // float3 mainLightColor = albedo.rgb * mainLight.color.rgb * NdotL * shadowAttenuation;
                // finalTexture += mainLightColor;

                // uint maxVisibleLights = 20;
                // UNITY_LOOP for (uint i = 0; i < maxVisibleLights; i++)
                // {
                //     Light light = GetAdditionalLight(i, IN.worldPos);
                //     float3 lightDir = normalize(light.direction);
                //     float NdotLAdd = max(0, dot(normal, lightDir));

                //     #if defined(_ADDITIONAL_LIGHTS_COOKIE)
                //         float4 cookie = SampleAdditionalLightCookie(i, IN.worldPos);
                //         light.color *= cookie.rgb;
                //     #endif

                //     finalTexture += albedo.rgb * light.color.rgb * NdotLAdd * pow(light.distanceAttenuation, 0.6);
                // }

                // finalTexture = MixFog(finalTexture, IN.fogFactor);

                // return float4(finalTexture, 1.0);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
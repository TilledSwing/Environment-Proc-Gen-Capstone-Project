// This shader fills the mesh shape with a color predefined in the code.
Shader "Custom/TerrainTextureShader"
{
    // The properties block of the Unity shader. In this example this block is empty
    // because the output color is predefined in the fragment shader code.
    Properties
    { 
        _TextureArray("Texture Array", 2DArray) = "white" {}
        // _HeightStartsArray("Height Starts Array", Float) = 0
        // _HeightEndsArray("Height Ends Array", Float) = 0
        // _SlopeStartsArray("Slope Starts Array", Float) = 0
        // _SlopeEndsArray("Slope Ends Array", Float) = 0

        // _UnderwaterTexture("Underwater Texture", 2D) = "white" {}
        // _UnderwaterTexHeightStart("Underwater Texture Height Start", Float) = 0
        // _UnderwaterTexHeightEnd("Underwater Texture Height End", Float) = 0

        // _Texture1("Texture1", 2D) = "white" {}
        // _Tex1HeightStart("Texture 1 Height Start", Float) = 0
        // _Tex1HeightEnd("Texture 1 Height End", Float) = 40
        // _Tex1SlopeStart("Texture 1 Slope Start", Float) = 0.0
        // _Tex1SlopeEnd("Texture 1 Slope End", Float) = 0.3

        // _Texture2("Texture2", 2D) = "white" {}
        // _Tex2HeightStart("Texture 2 Height Start", Float) = 35
        // _Tex2HeightEnd("Texture 2 Height End", Float) = 45
        // _Tex2SlopeStart("Texture 2 Slope Start", Float) = 0.3
        // _Tex2SlopeEnd("Texture 2 Slope End", Float) = 0.8

        // _Texture3("Texture3", 2D) = "white" {}
        // _Tex3HeightStart("Texture 3 Height Start", Float) = 40
        // _Tex3HeightEnd("Texture 3 Height End", Float) = 50
        // _Tex3SlopeStart("Texture 3 Slope Start", Float) = 0.8
        // _Tex3SlopeEnd("Texture 3 Slope End", Float) = 1.0

        // _Texture4("Texture4", 2D) = "white" {}
        // _Tex4HeightStart("Texture 4 Height Start", Float) = 50
        // _Tex4HeightEnd("Texture 4 Height End", Float) = 70
        // _Tex4SlopeStart("Texture 4 Slope Start", Float) = 0.8
        // _Tex4SlopeEnd("Texture 4 Slope End", Float) = 1.0

        _Scale("Texture Scale", Float) = 10
        _SlopeBlendSharpness("Slope Blend Sharpness", Float) = 1
    }

    // The SubShader block containing the Shader code.
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Lightmode" = "UniversalForward" }
        LOD 200

        Pass
        {
            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader.
            #pragma vertex vert
            // This line defines the name of the fragment shader.
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _FORWARD_PLUS

            // The Core.hlsl file contains definitions of frequently used HLSL
            // macros and functions, and also contains #include references to other
            // HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

            // The structure definition defines which variables it contains.
            // This example uses the Attributes structure as an input structure in
            // the vertex shader.
            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object
                // space.
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float4 shadowCoord : TEXCOORD3;
            };

            // The vertex shader definition with properties defined in the Varyings
            // structure. The type of the vert function must match the type (struct)
            // that it returns.
            #define MAX_TEXTURES 5
            CBUFFER_START(UnityPerMaterial)
                float _Scale;
                int _LayerCount;

                float _HeightStartsArray[MAX_TEXTURES];
                float _HeightEndsArray[MAX_TEXTURES];
                float _SlopeStartsArray[MAX_TEXTURES];
                float _SlopeEndsArray[MAX_TEXTURES];

                // float _UnderwaterTexHeightStart;
                // float _UnderwaterTexHeightEnd;
                // float _UnderwaterTexSlopeStart;
                // float _UnderwaterTexSlopeEnd;

                // float _Tex1HeightStart;
                // float _Tex1HeightEnd;
                // float _Tex1SlopeStart;
                // float _Tex1SlopeEnd;

                // float _Tex2HeightStart;
                // float _Tex2HeightEnd;
                // float _Tex2SlopeStart;
                // float _Tex2SlopeEnd;

                // float _Tex3HeightStart;
                // float _Tex3HeightEnd;
                // float _Tex3SlopeStart;
                // float _Tex3SlopeEnd;

                // float _Tex4HeightStart;
                // float _Tex4HeightEnd;
                // float _Tex4SlopeStart;
                // float _Tex4SlopeEnd;

                float _SlopeBlendSharpness;
            CBUFFER_END
            TEXTURE2D_ARRAY(_TextureArray); SAMPLER(sampler_TextureArray);

            // TEXTURE2D(_UnderwaterTexture); SAMPLER(sampler_UnderwaterTexture);
            // TEXTURE2D(_Texture1); SAMPLER(sampler_Texture1);
            // TEXTURE2D(_Texture2); SAMPLER(sampler_Texture2);
            // TEXTURE2D(_Texture3); SAMPLER(sampler_Texture3);
            // TEXTURE2D(_Texture4); SAMPLER(sampler_Texture4);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.worldPos = worldPos;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                OUT.shadowCoord = GetShadowCoord(vertexInput);
                #endif

                return OUT;
            }

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

            // float4 TriplanarSample(TEXTURE2D_PARAM(tex, samp), float3 worldPos, float3 normal, float scale) {
            //     float3 absNormal = abs(normal);
            //     float3 blend = absNormal / (absNormal.x + absNormal.y + absNormal.z);

            //     float2 xz = worldPos.yz * scale;
            //     float2 xy = worldPos.xy * scale;
            //     float2 yz = worldPos.xz * scale;

            //     float4 xTex = SAMPLE_TEXTURE2D(tex, samp, xz);
            //     float4 yTex = SAMPLE_TEXTURE2D(tex, samp, yz);
            //     float4 zTex = SAMPLE_TEXTURE2D(tex, samp, xy);

            //     return xTex * blend.x + yTex * blend.y + zTex * blend.z;
            // }

            // The fragment shader definition.
            float4 frag(Varyings IN) : SV_Target
            {
                float4 albedo = float4(0, 0, 0, 0);
                float totalWeight = 0;
                float height = IN.worldPos.y;
                float3 normal = normalize(IN.worldNormal);
                float slope = 1 - dot(normal, float3(0, 1, 0));

                [unroll]
                for(int i = 0; i < _LayerCount; i++) {
                    float heightWeight = smoothstep(_HeightStartsArray[i], _HeightEndsArray[i], height) * (i != _LayerCount - 1 ? (1 - smoothstep(_HeightStartsArray[i+1], _HeightEndsArray[i+1], height)) : 1);
                    float slopeWeight = smoothstep(_SlopeStartsArray[i], _SlopeEndsArray[i], slope) * (i != _LayerCount - 1 ? (1 - smoothstep(_SlopeStartsArray[i+1], _SlopeEndsArray[i+1], slope)) : 1);
                    float weight = heightWeight;

                    if(weight > 0.001) {
                        float4 triplanarSample = TriplanarSample(_TextureArray, sampler_TextureArray, IN.worldPos, normal, _Scale, i);
                        albedo += triplanarSample * weight;
                        totalWeight += weight;
                    }
                }

                albedo /= max(totalWeight, 0.001);

                // float height = IN.worldPos.y;
                // if(height < _UnderwaterTexHeightStart) {
                //     height = _UnderwaterTexHeightStart + 1;
                // }
                // if(height > max(max(_Tex1HeightEnd, _Tex2HeightEnd), max(_Tex3HeightEnd, _Tex4HeightEnd))) {
                //     height = max(max(_Tex1HeightEnd, _Tex2HeightEnd), max(_Tex3HeightEnd, _Tex4HeightEnd)) - 1;
                // }
                // float slope = 1 - dot(normalize(IN.worldNormal), float3(0, 1, 0));
                // float3 normal = normalize(IN.worldNormal);

                // float underwaterTexClampedHeight = smoothstep(_UnderwaterTexHeightStart, _UnderwaterTexHeightEnd, height) * (1 - smoothstep(_Tex1HeightStart, _Tex1HeightEnd, height));
                // float underwaterTexClampedSlope = smoothstep(_UnderwaterTexSlopeStart, _UnderwaterTexSlopeEnd, slope) * (1 - smoothstep(_Tex1SlopeStart, _Tex1SlopeEnd, slope));

                // float tex1ClampedHeight = smoothstep(_Tex1HeightStart, _Tex1HeightEnd, height) * (1 - smoothstep(_Tex2HeightStart, _Tex2HeightEnd, height));
                // float tex1ClampedSlope = smoothstep(_Tex1SlopeStart, _Tex1SlopeEnd, slope) * (1 - smoothstep(_Tex2SlopeStart, _Tex2SlopeEnd, slope));

                // float tex2ClampedHeight = smoothstep(_Tex2HeightStart, _Tex2HeightEnd, height) * (1 - smoothstep(_Tex3HeightStart, _Tex3HeightEnd, height));
                // float tex2ClampedSlope = smoothstep(_Tex2SlopeStart, _Tex2SlopeEnd, slope) * (1 - smoothstep(_Tex3SlopeStart, _Tex3SlopeEnd, slope));

                // float tex3ClampedHeight = smoothstep(_Tex3HeightStart, _Tex3HeightEnd, height) * (1 - smoothstep(_Tex4HeightStart, _Tex4HeightEnd, height));
                // float tex3ClampedSlope = smoothstep(_Tex3SlopeStart, _Tex3SlopeEnd, slope) * (1 - smoothstep(_Tex4SlopeStart, _Tex4SlopeEnd, slope));

                // float tex4ClampedHeight = smoothstep(_Tex4HeightStart, _Tex4HeightEnd, height);
                // float tex4ClampedSlope = smoothstep(_Tex4SlopeStart, _Tex4SlopeEnd, slope);

                // float underwaterTexWeight = underwaterTexClampedHeight;
                // float tex1Weight = tex1ClampedHeight;
                // float tex2Weight = tex2ClampedHeight;
                // float tex3Weight = tex3ClampedHeight;
                // float tex4Weight = tex4ClampedHeight;

                // float total = underwaterTexWeight + tex1Weight + tex2Weight + tex3Weight + tex4Weight;
                // underwaterTexWeight /= total;
                // tex1Weight /= total;
                // tex2Weight /= total;
                // tex3Weight /= total;
                // tex4Weight /= total;

                // float4 underwaterTex = TriplanarSample(_UnderwaterTexture, sampler_UnderwaterTexture, IN.worldPos, normal, _Scale);
                // float4 tex1 = TriplanarSample(_Texture1, sampler_Texture1, IN.worldPos, normal, _Scale);
                // float4 tex2 = TriplanarSample(_Texture2, sampler_Texture2, IN.worldPos, normal, _Scale);
                // float4 tex3 = TriplanarSample(_Texture3, sampler_Texture3, IN.worldPos, normal, _Scale);
                // float4 tex4 = TriplanarSample(_Texture4, sampler_Texture4, IN.worldPos, normal, _Scale);

                // // Texture blend
                // float4 albedo = underwaterTex * underwaterTexWeight + tex1 * tex1Weight + tex2 * tex2Weight + tex3 * tex3Weight + tex4 * tex4Weight;

                float3 finalTexture = float3(0, 0, 0);

                // Get main directional light info
                Light mainLight = GetMainLight();
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                IN.shadowCoord = IN.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                IN.shadowCoord = TransformWorldToShadowCoord(IN.worldPos);
                #else
                IN.shadowCoord = float4(0, 0, 0, 0);
                #endif
                float shadowAttenuation = MainLightRealtimeShadow(IN.shadowCoord);

                // Calculate diffuse lighting
                float NdotL = max(0, dot(normal, mainLight.direction));
                float3 mainLightColor = mainLight.color.rgb * NdotL * shadowAttenuation;
                finalTexture += albedo.rgb * mainLightColor;

                uint maxVisibleLights = 50;
                UNITY_LOOP for (uint i = 0; i < maxVisibleLights; i++)
                {
                    Light light = GetAdditionalLight(i, IN.worldPos);
                    float3 lightDir = normalize(light.direction);
                    float NdotLAdd = max(0, dot(normal, lightDir));
                    finalTexture += albedo.rgb * light.color.rgb * NdotLAdd * pow(light.distanceAttenuation, 0.6);
                }

                // Optional ambient light (from Unity's shading environment)
                // Brighter ambient for surface areas
                float surfaceAmbient = 0.1;
                float caveAmbient = 0.015;

                // Threshold for cave vs surface
                float caveThreshold = 0.0;
                float ambientStrength = lerp(caveAmbient, surfaceAmbient, saturate((IN.worldPos.y - caveThreshold) * 0.02));

                float shadowBoost = 1.0 - shadowAttenuation;
                finalTexture += albedo.rgb * ambientStrength * shadowBoost;

                return float4(finalTexture, 1.0);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
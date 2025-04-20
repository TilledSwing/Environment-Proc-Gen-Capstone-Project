// This shader fills the mesh shape with a color predefined in the code.
Shader "Custom/TerrainTextureShader"
{
    // The properties block of the Unity shader. In this example this block is empty
    // because the output color is predefined in the fragment shader code.
    Properties
    { 
        _Texture1("Texture1", 2D) = "white" {}
        _Tex1HeightStart("Texture 1 Height Start", Float) = 0
        _Tex1HeightEnd("Texture 1 Height End", Float) = 40
        _Tex1SlopeStart("Texture 1 Slope Start", Float) = 0.0
        _Tex1SlopeEnd("Texture 1 Slope End", Float) = 0.3

        _Texture2("Texture2", 2D) = "white" {}
        _Tex2HeightStart("Texture 2 Height Start", Float) = 35
        _Tex2HeightEnd("Texture 2 Height End", Float) = 50
        _Tex2SlopeStart("Texture 2 Slope Start", Float) = 0.3
        _Tex2SlopeEnd("Texture 2 Slope End", Float) = 0.8

        _Texture3("Texture3", 2D) = "white" {}
        _Tex3HeightStart("Texture 3 Height Start", Float) = 45
        _Tex3HeightEnd("Texture 3 Height End", Float) = 80
        _Tex3SlopeStart("Texture 3 Slope Start", Float) = 0.8
        _Tex3SlopeEnd("Texture 3 Slope End", Float) = 1.0

        _Scale("Texture Scale", Float) = 10
        _SlopeBlendSharpness("Slope Blend Sharpness", Float) = 1
    }

    // The SubShader block containing the Shader code.
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
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

            // The Core.hlsl file contains definitions of frequently used HLSL
            // macros and functions, and also contains #include references to other
            // HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

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

            CBUFFER_START(UnityPerMaterial)
                float _Scale;
                float _Tex1HeightStart;
                float _Tex1HeightEnd;
                float _Tex1SlopeStart;
                float _Tex1SlopeEnd;

                float _Tex2HeightStart;
                float _Tex2HeightEnd;
                float _Tex2SlopeStart;
                float _Tex2SlopeEnd;

                float _Tex3HeightStart;
                float _Tex3HeightEnd;
                float _Tex3SlopeStart;
                float _Tex3SlopeEnd;

                float _SlopeBlendSharpness;
            CBUFFER_END
            TEXTURE2D(_Texture1); SAMPLER(sampler_Texture1);
            TEXTURE2D(_Texture2); SAMPLER(sampler_Texture2);
            TEXTURE2D(_Texture3); SAMPLER(sampler_Texture3);

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

            float4 TriplanarSample(TEXTURE2D_PARAM(tex, samp), float3 worldPos, float3 normal, float scale) {
                float3 absNormal = abs(normal);
                float3 blend = absNormal / (absNormal.x + absNormal.y + absNormal.z);

                float2 xz = worldPos.yz * scale;
                float2 xy = worldPos.xy * scale;
                float2 yz = worldPos.xz * scale;

                float4 xTex = SAMPLE_TEXTURE2D(tex, samp, xz);
                float4 yTex = SAMPLE_TEXTURE2D(tex, samp, yz);
                float4 zTex = SAMPLE_TEXTURE2D(tex, samp, xy);

                return xTex * blend.x + yTex * blend.y + zTex * blend.z;
            }

            // The fragment shader definition.
            float4 frag(Varyings IN) : SV_Target
            {
                float height = IN.worldPos.y;
                if(height < min(min(_Tex1HeightStart, _Tex2HeightStart), _Tex3HeightStart)) {
                    height = min(min(_Tex1HeightStart, _Tex2HeightStart), _Tex3HeightStart) + 1;
                }
                if(height > max(max(_Tex1HeightEnd, _Tex2HeightEnd), _Tex3HeightEnd)) {
                    height = max(max(_Tex1HeightEnd, _Tex2HeightEnd), _Tex3HeightEnd) - 1;
                }
                float slope = 1 - dot(normalize(IN.worldNormal), float3(0, 1, 0));
                float3 normal = normalize(IN.worldNormal);

                float tex1ClampedHeight = smoothstep(_Tex1HeightStart, _Tex1HeightEnd, height) * (1 - smoothstep(_Tex2HeightStart, _Tex2HeightEnd, height));
                float tex1ClampedSlope = smoothstep(_Tex1SlopeStart, _Tex1SlopeEnd, slope) * (1 - smoothstep(_Tex2SlopeStart, _Tex2SlopeEnd, slope));

                float tex2ClampedHeight = smoothstep(_Tex2HeightStart, _Tex2HeightEnd, height) * (1 - smoothstep(_Tex3HeightStart, _Tex3HeightEnd, height));
                float tex2ClampedSlope = smoothstep(_Tex2SlopeStart, _Tex2SlopeEnd, slope) * (1 - smoothstep(_Tex3SlopeStart, _Tex3SlopeEnd, slope));

                float tex3ClampedHeight = smoothstep(_Tex3HeightStart, _Tex3HeightEnd, height);
                float tex3ClampedSlope = smoothstep(_Tex3SlopeStart, _Tex3SlopeEnd, slope);

                float tex1Weight = tex1ClampedHeight;
                float tex2Weight = tex2ClampedHeight;
                float tex3Weight = tex3ClampedHeight;

                float total = tex1Weight + tex2Weight + tex3Weight;
                tex1Weight /= total;
                tex2Weight /= total;
                tex3Weight /= total;

                float4 tex1 = TriplanarSample(_Texture1, sampler_Texture1, IN.worldPos, normal, _Scale);
                float4 tex2 = TriplanarSample(_Texture2, sampler_Texture2, IN.worldPos, normal, _Scale);
                float4 tex3 = TriplanarSample(_Texture3, sampler_Texture3, IN.worldPos, normal, _Scale);

                // Texture blend
                float4 albedo = tex1 * tex1Weight + tex2 * tex2Weight + tex3 * tex3Weight;

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

                uint additionalLightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < additionalLightCount; ++i)
                {
                    Light light = GetAdditionalLight(i, IN.worldPos);
                    float3 lightDir = normalize(light.direction);
                    float NdotLAdd = max(0, dot(normal, lightDir));
                    finalTexture += albedo.rgb * light.color.rgb * NdotLAdd * light.distanceAttenuation;
                }

                // Optional ambient light (from Unity's shading environment)
                finalTexture += albedo.rgb * 0.15; // tweak ambient strength as needed

                return float4(finalTexture, 1.0);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
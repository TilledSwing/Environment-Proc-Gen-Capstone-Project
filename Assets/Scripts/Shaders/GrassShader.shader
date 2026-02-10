Shader "Custom/GrassShader"
{
    Properties
    {
        _MinHeight("Min Height", Float) = 30.0
        _MaxHeight("Max Height", Float) = 60.0
    }
    SubShader
    {
        // Pass
        // {
        //     Name "DepthOnly"
        //     Tags { "LightMode" = "DepthOnly" }

        //     Cull Back
        //     ZWrite On
        //     ColorMask 0

        //     HLSLPROGRAM
        //     #pragma vertex vert
        //     #pragma fragment frag
        //     #pragma multi_compile_instancing

        //     #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        //     struct Attributes
        //     {
        //         // The positionOS variable contains the vertex positions in object space.
        //         float4 positionOS : POSITION;
        //         uint instanceID : SV_InstanceID;
        //     };

        //     struct Varyings
        //     {
        //         // The positions in this struct must have the SV_POSITION semantic.
        //         float4 positionHCS : SV_POSITION;
        //         float3 worldPos : TEXCOORD0;
        //         float minHeightCutoff : TEXCOORD1;
        //         float maxHeightCutoff : TEXCOORD2;
        //     };

        //     struct Vertex
        //     {
        //         float3 position;
        //         float3 normal;
        //     };

        //     StructuredBuffer<Vertex> _Positions;
        //     float _MinHeight;
        //     float _MaxHeight;

        //     Varyings vert(Attributes IN)
        //     {
        //         Varyings OUT;
        //         uint instanceID = IN.instanceID;
        //         float3 instanceOffset = _Positions[instanceID].position;

        //         float3 worldPos = IN.positionOS.xyz * 2 + instanceOffset;
        //         OUT.positionHCS = TransformWorldToHClip(worldPos);
        //         OUT.worldPos = worldPos;

        //         OUT.minHeightCutoff = instanceOffset.y - (_MinHeight + 0);
        //         OUT.maxHeightCutoff = (_MaxHeight - 8) - instanceOffset.y;

        //         return OUT;
        //     }

        //     float4 frag(Varyings IN) : SV_Target
        //     {
        //         clip(IN.minHeightCutoff);
        //         clip(IN.maxHeightCutoff);

        //         return 0;
        //     }
        //     ENDHLSL
        // }
        Pass
            {
            Tags { "RenderType"="Opaque" "Queue"="Opaque" "RenderPipeline"="UniversalPipeline" "Lightmode"="UniversalForward"}
            LOD 200
            Cull Off
            ZWrite Off

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

            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object space.
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float minHeightCutoff : TEXCOORD4;
                float maxHeightCutoff : TEXCOORD5;
            };

            struct Vertex
            {
                float3 position;
                float3 normal;
            };

            StructuredBuffer<Vertex> _Positions;
            float _MinHeight;
            float _MaxHeight;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                uint instanceID = IN.instanceID;
                float3 instanceOffset = _Positions[instanceID].position;

                float3 worldPos = IN.positionOS.xyz * 2 + instanceOffset;
                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                OUT.worldPos = worldPos;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                OUT.shadowCoord = GetShadowCoord(vertexInput);
                #endif

                OUT.minHeightCutoff = instanceOffset.y - (_MinHeight + 0);
                OUT.maxHeightCutoff = (_MaxHeight - 8) - instanceOffset.y;

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                clip(IN.minHeightCutoff);
                clip(IN.maxHeightCutoff);

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.worldPos;
                inputData.normalWS = IN.worldNormal;
                inputData.viewDirectionWS = normalize(_WorldSpaceCameraPos - IN.worldPos);
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.worldPos);
                inputData.fogCoord = 0;
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.vertexLighting = 0;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionHCS);
                inputData.shadowMask = 1;

                SurfaceData surfaceData;
                surfaceData.albedo = float3(0,1,0);
                surfaceData.alpha = 1;
                surfaceData.metallic = 0.0;
                surfaceData.specular = 0.2;
                surfaceData.smoothness = 0.5;
                surfaceData.normalTS = float3(0,1,0);
                surfaceData.emission = 0.0;
                surfaceData.occlusion = 1.0;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                float4 finalColor = UniversalFragmentPBR(inputData, surfaceData);

                return finalColor;
            }
            ENDHLSL
        }
    }
}
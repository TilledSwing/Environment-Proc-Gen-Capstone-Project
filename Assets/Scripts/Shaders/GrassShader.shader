Shader "Custom/GrassShader"
{
    Properties
    {
        _InstanceTexture ("InstanceTexture", 2D) = "white" {}
        _BaseColor ("BaseColor", Color) = (0,0,0,1)
        _TipColor ("TipColor", Color) = (0,0,0,1)
        _WindDir ("WindDir", Vector) = (1,1,0,0)
        _WindStrength ("WindStrength", Float) = 0.5
        _WindOscillation ("WindOscillation", Float) = 0.8
        _AOStrength ("AOStrength", Float) = 0.5
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
            #pragma shader_feature _UNIFORM_SCALE

            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Scripts/Shaders/Common/Helpers.hlsl"

            struct Attributes
            {
                // The positionOS variable contains the vertex positions in object space.
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                uint instanceID : SV_InstanceID;
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

            struct GrassBlade
            {
                float3 position;
                float rotation;
                float height;
                float curve;
                float3 terrainNormal;
            };
            
            TEXTURE2D(_InstanceTexture);
            SAMPLER(sampler_InstanceTexture);
            StructuredBuffer<GrassBlade> _Positions;
            float2 _WindDir;
            float _WindStrength;
            float _WindOscillation;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                InitIndirectDrawArgs(0);
                uint instanceID = IN.instanceID;
                GrassBlade grassBlade = _Positions[instanceID];
                float3 instanceOffset = grassBlade.position;
                float sinRot = sin(grassBlade.rotation);
                float cosRot = cos(grassBlade.rotation);

                // Y-axis rotation
                float3 local = IN.positionOS.xyz;
                local.z += pow(local.y, 2) * grassBlade.curve;
                float3 rotated;
                rotated.x = local.x * cosRot - local.z * sinRot;
                rotated.z = local.x * sinRot + local.z * cosRot;
                rotated.y = local.y;
                #ifdef _UNIFORM_SCALE
                    rotated.xyz *= grassBlade.height;
                #else
                    rotated.y *= grassBlade.height;
                #endif

                float3 worldPos = rotated + instanceOffset;
                float2 windDir = normalize(_WindDir);
                float wave = sin((_Time.z * _WindOscillation) + (worldPos.x * 0.15) + (worldPos.z * 0.15));
                float windStr = ((GradientNoiseDeterministicfloat(worldPos.xz * 0.5 + instanceID, 1) * 2 - 1) + wave) * _WindStrength;

                float3 bend = float3(windDir.x, 0, windDir.y) * windStr * rotated.y;
                float dist = distance(_WorldSpaceCameraPos, worldPos);
                float blend = saturate((dist - 80) / (120 - 80));
                worldPos += bend * (1 - blend);

                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.worldPos = worldPos;

                float3 normalLocal = IN.normalOS;
                normalLocal.z += 2 * IN.positionOS.y * grassBlade.curve;
                float3 rotatedNormal;
                rotatedNormal.x = normalLocal.x * cosRot - normalLocal.z * sinRot;
                rotatedNormal.z = normalLocal.x * sinRot + normalLocal.z * cosRot;
                rotatedNormal.y = normalLocal.y;

                float3 blendedNormal = normalize(lerp(normalize(rotatedNormal), grassBlade.terrainNormal, blend));
                OUT.worldNormal = blendedNormal;

                OUT.uv = IN.uv;

                return OUT;
            }

            float4 frag(Varyings IN, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_InstanceTexture, sampler_InstanceTexture, IN.uv);
                clip(tex.a - 0.5);
                float3 normalWS = normalize(IN.worldNormal);
                if (!isFrontFace) 
                {
                    normalWS = -normalWS;
                }
                return float4(NormalizeNormalPerPixel(normalWS), 0);
            }
            ENDHLSL
        }
        Pass
            {
            Tags { "RenderType"="Opaque" "Queue"="Opaque" "RenderPipeline"="UniversalPipeline" "Lightmode"="UniversalForward"}
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
            #pragma shader_feature _UNIFORM_SCALE

            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"
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
                float grassHeight : TEXCOORD4;
                float2 uv : TEXCOORD5;
            };

            struct GrassBlade
            {
                float3 position;
                float rotation;
                float height;
                float curve;
                float3 terrainNormal;
            };

            TEXTURE2D(_InstanceTexture);
            SAMPLER(sampler_InstanceTexture);
            StructuredBuffer<GrassBlade> _Positions;
            float4 _BaseColor;
            float4 _TipColor;
            float2 _WindDir;
            float _WindStrength;
            float _WindOscillation;
            float _AOStrength;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                InitIndirectDrawArgs(0);
                uint instanceID = IN.instanceID;
                GrassBlade grassBlade = _Positions[instanceID];
                float3 instanceOffset = grassBlade.position;
                float sinRot = sin(grassBlade.rotation);
                float cosRot = cos(grassBlade.rotation);

                // Y-axis rotation
                float3 local = IN.positionOS.xyz;
                local.z += pow(local.y, 2) * grassBlade.curve;
                float3 rotated;
                rotated.x = local.x * cosRot - local.z * sinRot;
                rotated.z = local.x * sinRot + local.z * cosRot;
                rotated.y = local.y;
                #ifdef _UNIFORM_SCALE
                    rotated.xyz *= grassBlade.height;
                #else
                    rotated.y *= grassBlade.height;
                #endif

                float3 worldPos = rotated + instanceOffset;
                float2 windDir = normalize(_WindDir);
                float wave = sin((_Time.z * _WindOscillation) + (worldPos.x * 0.15) + (worldPos.z * 0.15));
                float windStr = ((GradientNoiseDeterministicfloat(worldPos.xz * 0.5 + instanceID, 1) * 2 - 1) + wave) * _WindStrength;

                float3 bend = float3(windDir.x, 0, windDir.y) * windStr * rotated.y;
                float dist = distance(_WorldSpaceCameraPos, worldPos);
                float blend = saturate((dist - 80) / (120 - 80));
                worldPos += bend * (1 - blend);

                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                OUT.worldPos = worldPos;

                float3 normalLocal = IN.normalOS;
                normalLocal.z += 2 * IN.positionOS.y * grassBlade.curve;
                float3 rotatedNormal;
                rotatedNormal.x = normalLocal.x * cosRot - normalLocal.z * sinRot;
                rotatedNormal.z = normalLocal.x * sinRot + normalLocal.z * cosRot;
                rotatedNormal.y = normalLocal.y;

                float3 blendedNormal = normalize(lerp(normalize(rotatedNormal), grassBlade.terrainNormal, blend));
                OUT.worldNormal = blendedNormal;

                OUT.grassHeight = IN.positionOS.y;
                OUT.uv = IN.uv;

                return OUT;
            }

            float4 frag(Varyings IN, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_InstanceTexture, sampler_InstanceTexture, IN.uv);
                clip(tex.a - 0.5);
                float height01 = saturate(IN.grassHeight);

                float3 albedo = lerp(_BaseColor, _TipColor, height01);
                float ao = pow(1 - height01, 2);
                albedo *= lerp(1, _AOStrength, ao);
                albedo *= tex.xyz;

                float3 normalWS = normalize(IN.worldNormal);
                if (!isFrontFace) 
                {
                    normalWS = -normalWS;
                }

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.worldPos;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = normalize(_WorldSpaceCameraPos - IN.worldPos);
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.worldPos);
                inputData.fogCoord = 0;
                inputData.bakedGI = saturate(SampleSH(inputData.normalWS) + float3(0.02, 0.02, 0.02));
                inputData.vertexLighting = 0;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionHCS);
                inputData.shadowMask = 1;

                SurfaceData surfaceData;
                surfaceData.albedo = albedo;
                surfaceData.alpha = 1;
                surfaceData.metallic = 0.0;
                surfaceData.specular = 0.5;
                surfaceData.smoothness = 0.5;
                surfaceData.normalTS = float3(0,0,1);
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
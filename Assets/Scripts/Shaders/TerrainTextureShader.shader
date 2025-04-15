// This shader fills the mesh shape with a color predefined in the code.
Shader "Custom/TerrainTextureShader"
{
    // The properties block of the Unity shader. In this example this block is empty
    // because the output color is predefined in the fragment shader code.
    Properties
    { 
        _Texture1("Texture1", 2D) = "white" {}
        _Texture2("Texture2", 2D) = "white" {}
        _Texture3("Texture3", 2D) = "white" {}
        _Scale("Texture Scale", Float) = 10
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

            // The Core.hlsl file contains definitions of frequently used HLSL
            // macros and functions, and also contains #include references to other
            // HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
            };

            // The vertex shader definition with properties defined in the Varyings
            // structure. The type of the vert function must match the type (struct)
            // that it returns.

            CBUFFER_START(UnityPerMaterial)
                float _Scale;
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
                float slope = 1 - dot(normalize(IN.worldNormal), float3(0, 1, 0));
                float3 normal = normalize(IN.worldNormal);

                float tex1Weight = saturate(1 - slope) * saturate(1 - height / 50);
                float tex2Weight = slope;
                float tex3Weight = saturate(height / 100);

                float total = tex1Weight + tex2Weight + tex3Weight;
                tex1Weight /= total;
                tex2Weight /= total;
                tex3Weight /= total;

                float4 tex1 = TriplanarSample(_Texture1, sampler_Texture1, IN.worldPos, normal, _Scale);
                float4 tex2 = TriplanarSample(_Texture2, sampler_Texture2, IN.worldPos, normal, _Scale);
                float4 tex3 = TriplanarSample(_Texture3, sampler_Texture3, IN.worldPos, normal, _Scale);

                return tex1 * tex1Weight + tex2 * tex2Weight + tex3 * tex3Weight;
            }
            ENDHLSL
        }
    }
}
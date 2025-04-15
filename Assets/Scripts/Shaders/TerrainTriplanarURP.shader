Shader "Custom/TerrainTriplanarURP"
{
    Properties
    {
        _GrassTex("Grass Texture", 2D) = "white" {}
        _Scale("Texture Scale", Float) = 10
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalRenderPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Scale;
            CBUFFER_END

            TEXTURE2D(_GrassTex); SAMPLER(sampler_GrassTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.worldPos = worldPos;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            float4 TriplanarSample(TEXTURE2D_PARAM(tex, samp), float3 worldPos, float3 normal, float scale)
            {
                float3 blend = pow(abs(normal), 4.0);
                blend /= (blend.x + blend.y + blend.z);

                float2 uvX = worldPos.yz * scale;
                float2 uvY = worldPos.xz * scale;
                float2 uvZ = worldPos.xy * scale;

                float4 colX = SAMPLE_TEXTURE2D(tex, samp, uvX);
                float4 colY = SAMPLE_TEXTURE2D(tex, samp, uvY);
                float4 colZ = SAMPLE_TEXTURE2D(tex, samp, uvZ);

                return colX * blend.x + colY * blend.y + colZ * blend.z;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 normal = normalize(IN.worldNormal);
                float4 tex = TriplanarSample(_GrassTex, sampler_GrassTex, IN.worldPos, normal, _Scale);
                return tex;
            }
            ENDHLSL
        }
    }
}

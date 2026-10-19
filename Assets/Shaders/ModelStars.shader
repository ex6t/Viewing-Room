Shader "Viewing Room/Model Stars"
{
    Properties
    {
        _Stars("Inherited Star Cubemap", Cube) = "" {}
        _Exposure("Brightness", Range(0, 4)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest LEqual
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURECUBE(_Stars); SAMPLER(sampler_Stars);
            CBUFFER_START(UnityPerMaterial)
                float4x4 _WorldToStars;
                float _Exposure;
            CBUFFER_END
            struct Input { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Output { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Output Vertex(Input input)
            {
                Output output = (Output)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = TransformObjectToWorldDir(input.positionOS.xyz, false);
                return output;
            }
            half4 Fragment(Output input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 direction = mul((float3x3)_WorldToStars, normalize(input.direction));
                if (dot(direction, direction) < 0.001) direction = normalize(input.direction);
                // Retain the inherited cube's verified horizontal face correction.
                float3 size = abs(direction);
                if (size.x >= size.y && size.x >= size.z) direction.z = -direction.z;
                else direction.x = -direction.x;
                return half4(SAMPLE_TEXTURECUBE(_Stars, sampler_Stars, direction).rgb * _Exposure, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

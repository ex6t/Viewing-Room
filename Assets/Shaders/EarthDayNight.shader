Shader "Viewing Room/Earth Day Night"
{
    Properties
    {
        _BaseMap("Inherited Earth Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1,1,1,1)
        _NightBrightness("Night Visibility", Range(0,0.2)) = 0.035
        _DayBrightness("Day Brightness", Range(0,3)) = 1.4
        _ModelSunDirection("Direction to the model Sun", Vector) = (-1,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _ModelSunDirection;
                float _NightBrightness;
                float _DayBrightness;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }
            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 surface = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                // This globe is a diagram above the same ground sky, so its light uses
                // the research Sun in the model frame rather than the room's directional light.
                float sunlight = max(0, dot(normalize(input.normalWS), normalize(_ModelSunDirection.xyz)));
                return half4(surface * (_NightBrightness + sunlight * _DayBrightness), 1);
            }
            ENDHLSL
        }
    }
}

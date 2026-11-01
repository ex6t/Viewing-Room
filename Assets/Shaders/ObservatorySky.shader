Shader "Viewing Room/Observatory Sky"
{
    Properties
    {
        _Stars("Existing Star Cubemap", Cube) = "" {}
        _StarsExposure("Star Brightness", Range(0, 4)) = 1
        _SunDirection("Sun Direction (East, Up, North)", Vector) = (0, 0.5, 0.8660254, 0)
        _GroundView("Ground View", Range(0, 1)) = 1
        _DayZenithColor("Day Sky", Color) = (0.14, 0.38, 0.75, 1)
        _DayHorizonColor("Day Horizon", Color) = (0.62, 0.78, 0.92, 1)
        _NightZenithColor("Night Sky", Color) = (0.002, 0.005, 0.015, 1)
        _NightHorizonColor("Night Horizon", Color) = (0.015, 0.02, 0.045, 1)
        _TwilightColor("Twilight Tint", Color) = (0.86, 0.27, 0.10, 1)
        _ModelColor("Model Backdrop", Color) = (0.002, 0.004, 0.01, 1)
        [HDR] _SunColor("Sun Color", Color) = (3.8, 2.9, 1.7, 1)
        _SunRadiusDegrees("Sun Angular Radius", Range(0.1, 1)) = 0.266
        _HaloStrength("Sun Halo", Range(0, 1)) = 0.15
        _ShowSunPath("Show Daily Sun Path", Range(0, 1)) = 0
        _ObserverLatitude("Observer Latitude (Radians)", Float) = 0
        _SolarDeclination("Solar Declination (Radians)", Float) = 0
        _RevealBelowHorizon("Reveal Below Horizon", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "ObservatorySky"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex SkyVertex
            #pragma fragment SkyFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURECUBE(_Stars);
            SAMPLER(sampler_Stars);

            CBUFFER_START(UnityPerMaterial)
                float4x4 _WorldToSky;
                float4x4 _StarsRotation;
                float4 _SunDirection;
                half4 _DayZenithColor;
                half4 _DayHorizonColor;
                half4 _NightZenithColor;
                half4 _NightHorizonColor;
                half4 _TwilightColor;
                half4 _ModelColor;
                half4 _SunColor;
                float _StarsExposure;
                float _GroundView;
                float _SunRadiusDegrees;
                float _HaloStrength;
                float _ShowSunPath;
                float _ObserverLatitude;
                float _SolarDeclination;
                float _RevealBelowHorizon;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 directionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings SkyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // Unity's skybox cube supplies directions, independent of camera translation.
                output.directionWS = TransformObjectToWorldDir(input.positionOS.xyz, false);
                return output;
            }

            float3 RotateDirection(float4x4 rotation, float3 direction)
            {
                float3 rotated = mul((float3x3)rotation, direction);
                float lengthSquared = dot(rotated, rotated);
                // Matrix uniforms have no ShaderLab default; keep the asset preview usable
                // before the observation model initializes its two rotation matrices.
                return lengthSquared > 0.001 ? rotated * rsqrt(lengthSquared) : direction;
            }

            half4 SkyFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 worldDirection = normalize(input.directionWS);
                float3 localDirection = RotateDirection(_WorldToSky, worldDirection);
                float3 starDirection = RotateDirection(_StarsRotation, localDirection);
                // The inherited cubemap stores every face horizontally mirrored.
                // Correct the face coordinates while leaving the celestial frame intact.
                float3 faceSize = abs(starDirection);
                if (faceSize.x >= faceSize.y && faceSize.x >= faceSize.z)
                    starDirection.z = -starDirection.z;
                else
                    starDirection.x = -starDirection.x;
                float3 sunDirection = normalize(_SunDirection.xyz);
                float sunAltitude = sunDirection.y;
                float sunAlignment = dot(localDirection, sunDirection);

                // These inexpensive colors approximate atmosphere. The Sun and stars
                // still use the observation model's calculated astronomical directions.
                float daylight = smoothstep(-0.08, 0.16, sunAltitude);
                float heightBlend = smoothstep(0.0, 0.75, localDirection.y);
                half3 daySky = lerp(_DayHorizonColor.rgb, _DayZenithColor.rgb, heightBlend);
                half3 nightSky = lerp(_NightHorizonColor.rgb, _NightZenithColor.rgb, heightBlend);
                half3 groundSky = lerp(nightSky, daySky, daylight);

                float nearHorizon = 1.0 - saturate(localDirection.y);
                float towardSun = saturate(sunAlignment * 0.5 + 0.5);
                float twilight = 1.0 - smoothstep(0.02, 0.28, abs(sunAltitude));
                groundSky += _TwilightColor.rgb * twilight * nearHorizon * nearHorizon *
                    (0.2 + 0.8 * towardSun * towardSun);

                float aboveHorizon = smoothstep(-0.015, 0.02, localDirection.y);
                float reveal = _ShowSunPath * _RevealBelowHorizon;
                //below the horizon is a diagram view, with a darker background
                groundSky = lerp(groundSky, half3(0.012, 0.022, 0.045), reveal * (1.0 - aboveHorizon));
                float starsVisible = 1.0 - smoothstep(-0.18, 0.02, sunAltitude);
                half3 stars = SAMPLE_TEXTURECUBE(_Stars, sampler_Stars, starDirection).rgb;
                groundSky += stars * _StarsExposure * starsVisible * aboveHorizon;

                // Use full precision near the small disc; half precision loses its edge.
                float separation = max(0.0, 1.0 - sunAlignment);
                float discEdge = 1.0 - cos(radians(_SunRadiusDegrees));
                float edgeWidth = max(fwidth(separation), 0.0000001);
                float disc = 1.0 - smoothstep(discEdge - edgeWidth, discEdge + edgeWidth, separation);
                float halo = saturate(1.0 - separation / 0.0015);
                groundSky += _SunColor.rgb * (disc + halo * halo * _HaloStrength) * lerp(aboveHorizon, 1.0, reveal);

                if (_ShowSunPath > 0.5)
                {
                    //SolGeoVis v1.1 section 2.2: one daily circle at the current declination
                    //our local frame is east, up, north; the pole is tilted by latitude
                    float3 pole = float3(0.0, sin(_ObserverLatitude), cos(_ObserverLatitude));
                    float pathAngle = asin(clamp(dot(localDirection, pole), -1.0, 1.0));
                    float pathError = abs(pathAngle - _SolarDeclination);
                    float pathWidth = max(fwidth(pathAngle), 0.0004);
                    float path = 1.0 - smoothstep(0.0025, 0.0025 + pathWidth, pathError);
                    float3 equator = float3(0.0, cos(_ObserverLatitude), -sin(_ObserverLatitude));
                    float hourAngle = atan2(-localDirection.x, dot(localDirection, equator));
                    //dashes and color both identify the part below the horizon
                    float dash = step(0.35, frac((hourAngle + PI) * (48.0 / TWO_PI)));
                    float below = step(localDirection.y, 0.0);
                    float visiblePath = path * lerp(1.0, reveal * dash, below);
                    half3 pathColor = lerp(half3(1.0, 0.76, 0.18), half3(0.3, 0.85, 1.0), below);
                    groundSky = lerp(groundSky, pathColor, visiblePath);
                    float horizon = 1.0 - smoothstep(0.0015, 0.0015 + max(fwidth(localDirection.y), 0.0004), abs(localDirection.y));
                    groundSky = lerp(groundSky, half3(0.8, 0.92, 1.0), horizon);
                }

                // A uniform dark backdrop avoids implying a second celestial viewpoint
                // when the same observer clock is shown through the enlarged Earth model.
                return half4(lerp(_ModelColor.rgb, groundSky, saturate(_GroundView)), 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

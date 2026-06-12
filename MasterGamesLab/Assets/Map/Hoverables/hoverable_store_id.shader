Shader "Hidden/HoverableIdFromUv1"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "UniversalForward"
            // We use LightMode UniversalForward so the render feature catches it if required
            Tags
            {
                "LightMode"="UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Map/Shaders/azimuthal_equidistant_projection.hlsl"

            float _PlanetRadius;
            float _ProjectionFactor;
            float3 _ProjectionCenter;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 uv1 : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float d: TEXCOORD0;

                // 'nointerpolation' guarantees the ID stays exactly the same across 
                // the whole face of the triangle, preventing blended garbage IDs.
                nointerpolation float tileId : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 world_pos = TransformObjectToWorld(input.positionOS.xyz);
                float3 world_normal = float3(0, 0, 1);

                float3 projected_pos;
                float3 projected_normal;
                float out_d;

                azimuthal_equidistant_projection_float(
                    world_pos,
                    world_normal,
                    _ProjectionCenter,
                    _PlanetRadius,
                    _ProjectionFactor,
                    projected_pos,
                    projected_normal,
                    out_d
                );

                output.positionCS = TransformWorldToHClip(projected_pos);
                output.d = out_d;
                output.tileId = input.uv1.x;

                return output;
            }

            // Output a uint to match the R32_UInt texture format
            uint frag(Varyings input) : SV_Target
            {
                float out0 = (0.5 < _ProjectionFactor) ? 1.0 : 0.0;
                float out1 = input.d * out0;
                clip(out1 - (-0.9));

                // Add 0.5 before casting to fix float-precision errors 
                // (e.g., ensuring 14.9999 becomes 15, not 14)
                return (uint)(input.tileId + 0.5);
            }
            ENDHLSL
        }
    }
}
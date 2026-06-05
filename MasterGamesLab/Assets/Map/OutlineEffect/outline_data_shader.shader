Shader "Hidden/OutlineDataShader"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _InnerColor ("Inner Color", Color) = (1,0,0,0.5)
        _TextureId ("Texture ID", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "OutlineData"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Required so the MaterialPropertyBlock on your objects works
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Map/Shaders/azimuthal_equidistant_projection.hlsl"

            float _PlanetRadius;
            float _ProjectionFactor;
            float3 _ProjectionCenter;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL; // Fixed: changed to float3
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float d : TEXCOORD0; // Fixed: changed to float to match out_d
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Declare our properties so they can be changed per-object
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _OutlineColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InnerColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _TextureId)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 world_pos = TransformObjectToWorld(input.positionOS.xyz);
                float3 world_normal = TransformObjectToWorldNormal(input.normalOS);

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

                output.positionCS = TransformObjectToHClip(projected_pos);
                output.d = out_d;
                return output;
            }

            // This struct allows us to output to 3 Render Targets at once
            struct FragmentOutput
            {
                float4 color0 : SV_Target0;
                float4 color1 : SV_Target1;
                float4 color2 : SV_Target2;
            };

            FragmentOutput frag(Varyings input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                FragmentOutput output;

                // --- Shader Graph Logic Translation ---

                // 1. Comparison Node (A: 0.5, B: _ProjectionFactor)
                // Assuming "Greater Or Equal" (A >= B). 
                // If your node uses Less, Greater, Equal, etc., adjust the operator here!
                float out0 = (0.5 < _ProjectionFactor) ? 1.0 : 0.0;

                // 2. Multiply Node (A: d, B: Out0)
                float out1 = input.d * out0;

                // 3. Alpha Clip Threshold (Alpha: Out1, Threshold: -0.9)
                // Shader Graph executes: clip(Alpha - Threshold)
                clip(out1 - (-0.9)); // Simplified logically to clip(out1 + 0.9);

                // --------------------------------------

                // Target 0: Outline Color (RGBA)
                output.color0 = UNITY_ACCESS_INSTANCED_PROP(Props, _OutlineColor);

                // Target 1: Inner Color (RGBA)
                output.color1 = UNITY_ACCESS_INSTANCED_PROP(Props, _InnerColor);

                // Target 2: R = TextureID, G = 1.0 (Our Mask), B = 0, A = 0
                float texID = UNITY_ACCESS_INSTANCED_PROP(Props, _TextureId);
                output.color2 = float4(texID / 255.0, 1.0, 0.0, 0.0);

                return output;
            }
            ENDHLSL
        }
    }
}
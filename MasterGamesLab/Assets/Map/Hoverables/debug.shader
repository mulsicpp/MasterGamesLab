Shader "Hidden/TileID_DebugBlit"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"
        }
        Pass
        {
            Name "DebugBlit"
            ZTest Always ZWrite Off Cull Off
            // Enable basic transparency so you can still faintly see the game behind the debug overlay
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Declare it again but as uint to ensure correct loading
            Texture2D<uint> _TileIdTexture;

            float3 hash(uint n)
            {
                // Hash function to convert an ID to a random vibrant color
                n = (n << 13U) ^ n;
                n = n * (n * n * 15731U + 789221U) + 1376312589U;
                return float3((n & 255U) / 255.0, ((n >> 8U) & 255U) / 255.0, ((n >> 16U) & 255U) / 255.0);
            }

            float4 frag(Varyings input) : SV_Target
            {
                // input.texcoord is provided by the Unity Blitter
                uint2 pixelCoord = uint2(input.texcoord.xy * _BlitTexture_TexelSize.zw);
                uint id = _TileIdTexture.Load(int3(pixelCoord, 0));

                if (id == 0)
                    return float4(0, 0, 0, 0); // Transparent if no tile is here

                return float4(hash(id), 0.7); // 70% opacity random color
            }
            ENDHLSL
        }
    }
}
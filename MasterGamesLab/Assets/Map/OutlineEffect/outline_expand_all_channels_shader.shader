Shader "Hidden/OutlineExpandAllChannels"
{
    Properties
    {
        _BlurSize ("Blur Radius (Pixels)", Float) = 5.0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        ZWrite Off ZTest Always Blend Off Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float _BlurSize;

        float4 ExpandAllChannels(float radius, float2 uv, float2 texelOffset)
        {
            float4 currentMax = 0.0; // Initialize RGBA to 0

            // Loop through every pixel within the radius and find the maximum RGBA value
            for (int i = -radius; i <= radius; i++)
            {
                float2 sampleUV = uv + texelOffset * i;
                currentMax = max(currentMax, SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampleUV));
            }

            return currentMax;
        }

        float4 ChannelBoxExpand(float2 uv, float2 texelOffset)
        {
            float4 centerCol = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            int radius = (int)ceil(_BlurSize);

            // If radius is 0 or less, just return the exact pixel
            if (radius <= 0) return centerCol;

            // Run the expand over RGBA
            return ExpandAllChannels(radius, uv, texelOffset);
        }
        ENDHLSL

        Pass // 0: Horizontal Expand
        {
            Name "BoxExpand_Horizontal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            float4 frag(Varyings input): SV_Target
            {
                float2 texelOffset = float2(1.0 / _ScreenParams.x, 0.0);
                return ChannelBoxExpand(input.texcoord, texelOffset);
            }
            ENDHLSL
        }

        Pass // 1: Vertical Expand
        {
            Name "BoxExpand_Vertical"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            float4 frag(Varyings input) : SV_Target
            {
                float2 texelOffset = float2(0.0, 1.0 / _ScreenParams.y);
                return ChannelBoxExpand(input.texcoord, texelOffset);
            }
            ENDHLSL
        }
    }
}
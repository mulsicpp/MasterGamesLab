Shader "Hidden/OutlineExpandDepth"
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

        // CRITICAL FOR DEPTH: Do not write to color, force depth write on.
        ColorMask 0
        ZWrite On
        ZTest Always
        Blend Off
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float _BlurSize;

        // Depth should always be sampled with Point clamping
        //SAMPLER(sampler_PointClamp);

        float ExpandDepth(float radius, float2 uv, float2 texelOffset)
        {
            // 0.0 is the far plane in Unity's Reversed-Z
            float currentMax = 0.0;

            for (int i = -radius; i <= radius; i++)
            {
                float2 sampleUV = uv + texelOffset * i;
                // Sample the raw depth value
                float depth = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, sampleUV).r;

                // Keep the value closest to the camera
                currentMax = max(currentMax, depth);
            }

            return currentMax;
        }

        float ChannelBoxExpandDepth(float2 uv, float2 texelOffset)
        {
            int radius = (int)ceil(_BlurSize);

            // If radius is 0, just return the exact depth pixel
            if (radius <= 0) return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).r;

            return ExpandDepth(radius, uv, texelOffset);
        }
        ENDHLSL

        Pass // 0: Horizontal Expand
        {
            Name "BoxExpand_Horizontal_Depth"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            // Notice we return SV_Depth, not SV_Target!
            float frag(Varyings input) : SV_Depth
            {
                float2 texelOffset = float2(1.0 / _ScreenParams.x, 0.0);
                return ChannelBoxExpandDepth(input.texcoord, texelOffset);
            }
            ENDHLSL
        }

        Pass // 1: Vertical Expand
        {
            Name "BoxExpand_Vertical_Depth"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            float frag(Varyings input) : SV_Depth
            {
                float2 texelOffset = float2(0.0, 1.0 / _ScreenParams.y);
                return ChannelBoxExpandDepth(input.texcoord, texelOffset);
            }
            ENDHLSL
        }
    }
}
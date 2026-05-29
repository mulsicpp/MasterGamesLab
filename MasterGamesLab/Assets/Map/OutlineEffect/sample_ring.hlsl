#ifndef SAMPLE_RING_INCLUDED
#define SAMPLE_RING_INCLUDED

void sample_ring_float(
    float2 uv,
    float thickness,
    float sample_count,
    float2 screen_size,
    UnityTexture2D outline_color_tex,
    UnityTexture2D outline_texture_idx_tex,
    UnityTexture2D outline_depth_tex,
    UnitySamplerState ring_sampler,
    out float4 out_outline_color,
    out float out_depth)
{
    out_outline_color = float4(0.0, 0.0, 0.0, 0.0);
    out_depth = 0.0;

    float pi_over_two = 1.570796327;
    float2 texel_size = 1.0 / screen_size.xy;

    for (int i = 0; i < (int)sample_count; i++)
    {
        // Calculate rotational offset for this iteration
        // By dividing a 90-degree span by the sample count, our rotated cross 
        // patterns will eventually form a perfectly distributed circle.
        float angle_offset = ((float)i / sample_count) * pi_over_two;

        // Base angles for Top, Left, Right, Bottom
        // Top:    pi/2   (1.570796)
        // Left:   pi     (3.141592)
        // Right:  0      (0.0)
        // Bottom: 3pi/2  (4.712389)
        float4 angles = float4(
            1.570796327,
            3.141592654,
            0.0,
            4.712388980
        ) + angle_offset;

        // Sample the 4 directions
        for (int j = 0; j < 4; j++)
        {
            float angle = angles[j];
            float2 offset = float2(cos(angle), sin(angle)) * thickness * texel_size;
            float2 sample_uv = uv + offset;

            // FIX: Use .SampleLevel(..., 0) instead of .Sample()
            // This stops the compiler from trying to calculate Mipmaps and allows dynamic loops!
            float4 d3 = outline_texture_idx_tex.tex.SampleLevel(ring_sampler.samplerstate, sample_uv, 0);

            if (d3.g > 0.1)
            {
                out_outline_color = outline_color_tex.tex.SampleLevel(ring_sampler.samplerstate, sample_uv, 0);
                out_depth = outline_depth_tex.tex.SampleLevel(ring_sampler.samplerstate, sample_uv, 0).r;
                return;
            }
        }
    }
}
#endif

#ifndef ID_TO_COLOR_INCLUDED
#define ID_TO_COLOR_INCLUDED

void id_to_color_float(float id_bits, out float3 id_color)
{
    uint id = (uint)round(id_bits);
    float r = (float)((id >> 16) & 0xFF) / 255.0;
    float g = (float)((id >> 8) & 0xFF) / 255.0;
    float b = (float)(id & 0xFF) / 255.0;

    id_color = float3(r, g, b);
}

#endif // ID_TO_COLOR_INCLUDED

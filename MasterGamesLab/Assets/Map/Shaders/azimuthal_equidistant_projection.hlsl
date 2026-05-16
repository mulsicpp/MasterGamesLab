#ifndef AZIMUTHAL_EQUIDISTANT_PROJECTION_INCLUDED
#define AZIMUTHAL_EQUIDISTANT_PROJECTION_INCLUDED

// SphereProjection.hlsl
void azimuthal_equidistant_projection_float(float3 world_pos, float3 world_normal, float3 projection_center,
                                            float sphere_radius, float projection_factor, out float3 out_position,
                                            out float3 out_normal)
{
    // 1. Calculate projection mapping
    float3 p_norm = normalize(world_pos); // Assuming planet center is at (0,0,0)
    float d = dot(projection_center, p_norm);
    d = clamp(d, -1.0, 1.0);
    float angle = acos(d);

    // Distance along the surface of the sphere
    float arc_length = sphere_radius * angle;

    // Direction outward from the focus point on the tangent plane
    float3 to_point = p_norm - projection_center * d;
    float length_to_point = length(to_point);

    float3 flat_pos;
    if (length_to_point < 0.0001)
    {
        flat_pos = projection_center * sphere_radius; // Center point
    }
    else
    {
        float3 dir_on_plane = to_point / length_to_point;
        flat_pos = (projection_center * sphere_radius) + (dir_on_plane * arc_length);
    }

    // If it's a mountain, we need to preserve its height above the sphere
    // Find how high this vertex is above the base radius
    float elevation = length(world_pos) - sphere_radius;
    flat_pos += projection_center * elevation; // Add elevation straight "up" from the flat plane

    // 2. Blend Position
    out_position = lerp(world_pos, flat_pos, projection_factor);

    // 3. Blend Normal (Crucial for lighting)
    // The normal of the flat plane is simply the focusDir. 
    out_normal = normalize(lerp(world_normal, projection_center, projection_factor));
}

#endif // AZIMUTHAL_EQUIDISTANT_PROJECTION_INCLUDED

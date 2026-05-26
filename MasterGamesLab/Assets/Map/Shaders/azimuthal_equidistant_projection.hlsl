#ifndef AZIMUTHAL_EQUIDISTANT_PROJECTION_INCLUDED
#define AZIMUTHAL_EQUIDISTANT_PROJECTION_INCLUDED

void azimuthal_equidistant_projection_float(float3 world_pos, float3 world_normal, float3 projection_center,
                                            float sphere_radius, float projection_factor, out float3 out_position,
                                            out float3 out_normal, out float out_d) 
{
    float3 p_norm = normalize(world_pos);
    float d = dot(projection_center, p_norm);
    d = clamp(d, -1.0, 1.0);
    float angle = acos(d);
    
    // Output 'd' so we can use it to clip stretched pixels in the fragment shader
    out_d = d; 

    float arc_length = sphere_radius * angle;
    float3 to_point = p_norm - projection_center * d;
    float length_to_point = length(to_point);

    float3 flat_pos;
    
    // FIX: Differentiate between the North Pole and South Pole
    if (length_to_point < 0.0001)
    {
        if (d > 0.0) 
        {
            // North pole maps to the center
            flat_pos = projection_center * sphere_radius; 
        } 
        else 
        {
            // South pole maps to the outer ring. We create an arbitrary tangent direction 
            // so it doesn't snap back to the center of the map.
            float3 arbitrary_up = abs(projection_center.y) > 0.9 ? float3(1,0,0) : float3(0,1,0);
            float3 dir_on_plane = normalize(cross(projection_center, arbitrary_up));
            flat_pos = (projection_center * sphere_radius) + (dir_on_plane * arc_length);
        }
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

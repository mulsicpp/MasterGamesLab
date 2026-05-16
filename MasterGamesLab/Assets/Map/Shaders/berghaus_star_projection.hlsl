#ifndef BERGHAUS_STAR_PROJECTION_INCLUDED
#define BERGHAUS_STAR_PROJECTION_INCLUDED

void berghaus_star_projection_float(float3 world_pos, float3 world_normal, float3 projection_center,
                                    float sphere_radius, float lobes, float projection_factor,
                                    out float3 out_position, out float3 out_normal)
{
    float pi = 3.14159265359;

    // 1. Calculate distance from origin to preserve the height offset later
    float dist = length(world_pos); // Assuming planet center is at (0,0,0)

    // Normalize safely to avoid Division by Zero
    float3 p_norm = dist > 0.000001 ? (world_pos / dist) : float3(0, 1, 0);

    // Ensure the focus direction is normalized (acting as the plane's normal)
    float3 c_norm = normalize(projection_center);

    // 2. Establish a local orthonormal basis (East, North, Center) at the focus_dir
    float3 np = float3(0, 1, 0); // Global North Pole
    float3 n; // Local North

    // Handle singularity near the poles
    if (abs(c_norm.y) > 0.9999)
    {
        n = c_norm.y > 0.0 ? float3(0, 0, -1) : float3(0, 0, 1);
    }
    else
    {
        float d = dot(np, c_norm);
        n = normalize(np - (c_norm * d));
    }

    // Local East
    float3 E = normalize(cross(n, c_norm));

    // 3. Project the input point onto the local basis
    float vx = dot(p_norm, E);
    float vy = dot(p_norm, n);
    float vz = dot(p_norm, c_norm);

    // Clamp vz to [-1, 1] to strictly avoid NaNs in acos()
    vz = clamp(vz, -1.0, 1.0);

    // 4. Calculate polar coordinates (Azimuthal Equidistant base)
    float len = sqrt(vx * vx + vy * vy);
    float c = acos(vz); // Angular distance from center
    float r = c; // Radial distance on the map
    float theta;

    if (len < 0.0001)
    {
        theta = pi / 2.0;
    }
    else
    {
        theta = atan2(vy, vx);
    }

    // 5. Berghaus star modification exclusively for the back hemisphere
    if (vz < 0.0)
    {
        float half_pi = pi / 2.0;
        float k_lobe = 2.0 * pi / max(3.0, lobes);

        float lobe_index = floor((theta - half_pi) / k_lobe + 0.5);
        float theta0 = k_lobe * lobe_index + half_pi;

        float d_theta = theta - theta0;

        float alpha = atan2(sin(d_theta), 2.0 - cos(d_theta));

        // Clamp asin input to prevent rendering artifacts (NaNs) in HLSL
        float asin_input = clamp((pi / r) * sin(alpha), -1.0, 1.0);
        theta = theta0 + asin(asin_input) - alpha;
    }

    // 6. Convert to 2D Cartesian coordinates mapped on the surface flat plane
    float map_x = r * cos(theta) * sphere_radius;
    float map_y = r * sin(theta) * sphere_radius;

    // 7. Place on 3D tangent plane & re-apply height
    float3 tangent_origin = c_norm * sphere_radius;
    float3 flat_pos = tangent_origin + (E * map_x) + (n * map_y);

    // Find how high this vertex is above the base radius (mountains/valleys)
    float elevation = dist - sphere_radius;

    // Add elevation straight "up" from the flat plane
    flat_pos += c_norm * elevation;

    // 8. Blend Position
    out_position = lerp(world_pos, flat_pos, projection_factor);

    // 9. Blend Normal
    // The normal of the fully flat plane is simply the normalized focus_dir (cNorm). 
    out_normal = normalize(lerp(world_normal, c_norm, projection_factor));
}

#endif // BERGHAUS_STAR_PROJECTION_INCLUDED

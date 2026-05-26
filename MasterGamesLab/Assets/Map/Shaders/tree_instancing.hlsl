#ifndef TREE_INSTANCING_INCLUDED
#define TREE_INSTANCING_INCLUDED

// The exact same struct we will define in C#
struct tree_data
{
    float3 Position; // Position on the sphere
    float3 Normal; // Normal of the sphere at that position
    float Scale;
    float Yaw; // Random rotation around its own up-axis
    float Random; // Random float for color variation
};

// The memory buffer containing all our trees
StructuredBuffer<tree_data> _TreeBuffer;

void get_tree_world_transform_float(float instance_id, float3 local_pos, float3 local_norm, float3 projection_center,
                                    out float3 out_world_pos, out float3 out_world_norm, out float out_random)
{
    #ifdef SHADERGRAPH_PREVIEW
    // Default preview behavior
    out_world_pos = local_pos;
    out_world_norm = local_norm;
    out_random = 0.0;
    #else
    // 1. Fetch the data for this specific tree
    tree_data tree = _TreeBuffer[(uint)instance_id];

    float dot_p = dot(normalize(tree.Position), normalize(projection_center));
    if (dot_p < -0.98)
    {
        tree.Scale = 0.0;
    }

    // 2. Build the axes to align the tree's local Up (Y) to the Sphere Normal
    float3 up = normalize(tree.Normal);

    // Create an arbitrary orthogonal basis (Right and Forward)
    float3 right = abs(up.y) > 0.999 ? float3(1, 0, 0) : cross(float3(0, 1, 0), up);
    right = normalize(right);
    float3 forward = cross(up, right);

    // 3. Apply the random Yaw rotation
    float s, c;
    sincos(tree.Yaw, s, c);
    float3 rot_right = right * c - forward * s;
    float3 rot_forward = right * s + forward * c;

    // 4. Apply Scale
    rot_right *= tree.Scale;
    up *= tree.Scale;
    rot_forward *= tree.Scale;

    // 5. Calculate final World Position on the Sphere
    out_world_pos = tree.Position + (rot_right * local_pos.x) + (up * local_pos.y) + (rot_forward * local_pos.z);

    // 6. Calculate final World Normal on the Sphere
    out_world_norm = (rot_right * local_norm.x) + (up * local_norm.y) + (rot_forward * local_norm.z);
    out_world_norm = normalize(out_world_norm);
    out_random = tree.Random;
    #endif
}
#endif

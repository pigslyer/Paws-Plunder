shader_type spatial;
render_mode cull_disabled, unshaded;

uniform float fog_distance : hint_range(1.0, 1000.0) = 100.0;
uniform float fog_fade_range : hint_range(1.0, 1000.0) = 50.0;
uniform vec4 fog_color : hint_color = vec4(0.5, 0.5, 0.5, 1.0);

// Only use in QuadMesh with size (2, 2), because NDC is (-1, -1) to (1, 1)
// This shader is used to render a full screen quad
void vertex()
{
    POSITION = vec4(VERTEX, 1.0); // Bypass built-in transformations, set vertex position directly
}
void fragment() 
{
    vec3 screen_color = texture(SCREEN_TEXTURE, SCREEN_UV).rgb;
    float depth = texture(DEPTH_TEXTURE, SCREEN_UV).r; // depth from 0 to 1
    vec3 ndc = vec3(SCREEN_UV, depth * 2.0 - 1.0); // Convert to NDC (in OpenGL, depth is -1 to 1)
    vec4 view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0); // Convert to view space, z gives us distance to point
    view.xyz /= view.w; // Perspective division
    float linear_depth = -view.z; // Camera is facing -z

    // Calculate fog factor
    float depth_mask_inv = clamp((linear_depth - (fog_distance - fog_fade_range)) / fog_fade_range, 0.0, 1.0);

    // Mix fog color with fragment color
    ALBEDO = mix(screen_color.rgb, fog_color.rgb, depth_mask_inv);
}

// https://docs.godotengine.org/en/3.5/tutorials/shaders/advanced_postprocessing.html

// Avoid frustum culling
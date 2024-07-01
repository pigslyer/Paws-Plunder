shader_type canvas_item;

uniform float vignette_intensity : hint_range(0.0, 1.0) = 0.4;
uniform float vignette_opacity : hint_range(0.0, 1.0) = 0.5;
uniform vec4 vignette_rgb : hint_color = vec4(0.8, 0.0, 0.0, 1.0);
uniform bool enable = true;

void fragment() 
{
    vec3 screen_color = textureLod(SCREEN_TEXTURE, SCREEN_UV, 0.0).rgb;
    
    if (!enable) {
        COLOR = vec4(screen_color, 1.0);
    } else {
        vec2 uv = SCREEN_UV;
        uv *= 1.0 - uv.xy;
        float vignette = uv.x * uv.y * 15.0;
        vignette = pow(vignette, vignette_intensity);
        vignette = 1.0 - vignette;
        
        vec3 vignette_color = mix(screen_color, vignette_rgb.rgb, vignette * vignette_opacity);
        COLOR = vec4(vignette_color, 1.0);
    }
}
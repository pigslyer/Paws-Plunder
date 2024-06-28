shader_type canvas_item;

uniform float projector_power : hint_range(0,1) = 0.5;
uniform sampler2D distortionTexture;
uniform float vignette_param: hint_range(0,500)=20.0;

//vars related to the passing lines


//noise for the projector flickering
float noise(vec2 input){
	return fract(sin(dot(input,vec2(3.1415,8952.37)*12.29))*93.116);
}

void fragment() {
	//vignette-related:
	// float vignette_param2 =vignette_param + 0.5*(noise(vec2(TIME/6.0,TIME/5.0)));
    float vignette_param2 = vignette_param;
	float vig=-vignette_param2*((UV.x-0.5)*(UV.x-0.5)+(UV.y-0.5)*(UV.y-0.5));
	vec4 vignette=vec4(vig,vig,vig,1.0);

	//grayscale-related:
	vec4 screen_color = texture(SCREEN_TEXTURE, SCREEN_UV);
	

    COLOR = mix(screen_color, vignette, projector_power);

}
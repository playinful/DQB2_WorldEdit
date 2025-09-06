shader_type canvas_item;

uniform float texture_scale = 1.0;

void fragment() {
	vec2 texture_size = 1.0 / TEXTURE_PIXEL_SIZE * texture_scale;
	vec2 screen_size = 1.0 / SCREEN_PIXEL_SIZE;
	
	vec2 screen_position = screen_size * SCREEN_UV;
	
	float modx = mod(screen_position.x, texture_size.x);
	float mody = mod(screen_position.y, texture_size.y);
	
	COLOR = texture(TEXTURE, vec2(modx, mody) / texture_size);
}

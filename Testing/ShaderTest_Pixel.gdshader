shader_type canvas_item;

uniform float pixel_size = 1.0;

void fragment() {
	COLOR = texture(TEXTURE, UV - mod(UV, TEXTURE_PIXEL_SIZE * pixel_size));
}
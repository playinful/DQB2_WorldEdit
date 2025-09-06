shader_type canvas_item;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture;

void fragment() {
	vec3 screen_color = texture(SCREEN_TEXTURE, SCREEN_UV).rgb;
	COLOR.r = COLOR.g = COLOR.b = (screen_color.r + screen_color.g + screen_color.b) / 3.0;
}

shader_type canvas_item;

void fragment() {
	vec3 base_color = COLOR.rgb;
	COLOR.r = base_color.g + base_color.b / 2.0;
	COLOR.g = base_color.r + base_color.b / 2.0;
	COLOR.b = base_color.r + base_color.g / 2.0;
}
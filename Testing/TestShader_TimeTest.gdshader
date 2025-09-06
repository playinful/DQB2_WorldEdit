shader_type canvas_item;

void fragment() {
	COLOR.r = abs(mod(TIME, 2.0) - 1.0) * texture(TEXTURE, UV).r;
	COLOR.g = abs(mod(TIME, 4.0) / 2.0 - 1.0) * texture(TEXTURE, UV).g;
	COLOR.b = abs(mod(TIME, 8.0) / 4.0 - 1.0) * texture(TEXTURE, UV).b;
}

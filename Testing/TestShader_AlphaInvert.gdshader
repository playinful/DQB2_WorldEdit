shader_type canvas_item;

void fragment() {
	COLOR.a = 1.0-COLOR.a;
}
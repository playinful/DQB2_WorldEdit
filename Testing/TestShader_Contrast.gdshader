shader_type canvas_item;

uniform float contrast = 1.0;

void fragment() {
	COLOR.xyz = (COLOR.xyz - 0.5) * contrast + 0.5;
}
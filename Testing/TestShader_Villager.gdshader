shader_type canvas_item;

uniform sampler2D mask;
uniform vec3 hair_color : source_color;
uniform vec3 skin_color : source_color;
uniform vec3 eye_color : source_color;

void fragment(){
	vec3 mask_color = texture(mask, UV).rgb;
	COLOR.rgb = (COLOR.rgb * (1.0 - mask_color.r)) + (COLOR.rgb * mask_color.r * skin_color);
	COLOR.rgb = (COLOR.rgb * (1.0 - mask_color.g)) + (COLOR.rgb * mask_color.g * hair_color);
	COLOR.rgb = (COLOR.rgb * (1.0 - mask_color.b)) + (COLOR.rgb * mask_color.b * eye_color);
}
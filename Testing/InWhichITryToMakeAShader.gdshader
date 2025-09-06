shader_type canvas_item;

uniform bool grayscale = false;

void fragment() {
	if (grayscale)
	{
		float graycolor = (texture(TEXTURE, UV).r + texture(TEXTURE, UV).g + texture(TEXTURE, UV).b) / 3.0;
		COLOR.r = COLOR.g = COLOR.b = graycolor;
	}
	else
	{
		COLOR = texture(TEXTURE,UV);
	}
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}

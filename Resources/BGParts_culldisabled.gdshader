shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_disabled, diffuse_burley, specular_schlick_ggx;

uniform sampler2D albedo_texture : source_color, filter_linear_mipmap, repeat_enable;
uniform vec4 color : source_color = vec4(1, 1, 1, 1);
uniform sampler2D color_mask : hint_default_white, filter_linear_mipmap, repeat_enable;
uniform float alpha_scissor_threshold : hint_range(0.0, 1.0, 0.001);
uniform sampler2D overlay : hint_default_transparent, source_color, filter_linear_mipmap, repeat_enable;


void fragment() {
	vec2 base_uv = UV;

	vec4 albedo_tex = texture(albedo_texture, base_uv);
	ALBEDO = albedo_tex.rgb;

	// Detail: Enabled
	vec4 detail_mask_tex = texture(color_mask, base_uv);

	// Detail Blend Mode: Multiply
	vec3 detail = mix(ALBEDO.rgb, ALBEDO.rgb * color.rgb, color.a);

	ALBEDO.rgb = mix(ALBEDO.rgb, detail, detail_mask_tex.r);
	
	vec4 overlay_tex = texture(overlay, vec2(base_uv.x * 2.0, base_uv.y));
	
	ALBEDO = mix(ALBEDO.rgb, overlay_tex.rgb, overlay_tex.a);

	ALPHA = albedo_tex.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_scissor_threshold;
}

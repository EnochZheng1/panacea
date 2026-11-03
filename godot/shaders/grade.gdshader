// Final grade: a period photograph's vignette, warm paper cast, slight lens fringing and moving film grain.
shader_type canvas_item;
uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;
uniform float grain = 0.055;
uniform float vignette = 0.6;
uniform float desat = 0.12;
float hash(vec2 p) { vec3 p3 = fract(vec3(p.xyx) * 0.1031); p3 += dot(p3, p3.yzx + 33.33); return fract((p3.x + p3.y) * p3.z); }
void fragment() {
	vec2 uv = SCREEN_UV;
	vec2 d = uv - 0.5;
	float ca = 0.004 * dot(d, d) * 4.0;
	vec3 c = vec3(texture(screen_tex, uv + d * ca).r, texture(screen_tex, uv).g, texture(screen_tex, uv - d * ca).b);
	float l = dot(c, vec3(0.299, 0.587, 0.114));
	c = mix(c, vec3(l), desat);
	c *= vec3(1.04, 0.99, 0.90);
	c = mix(c, c * c * (3.0 - 2.0 * c), 0.25);                     // gentle S-curve
	c *= mix(1.0, smoothstep(0.95, 0.25, length(d * vec2(1.25, 1.0))), vignette);
	float g = hash(floor(uv / SCREEN_PIXEL_SIZE) + fract(TIME * 7.3) * 517.0) - 0.5;
	c += g * grain * (1.0 - 0.6 * l);
	COLOR = vec4(c, 1.0);
}

// The street outside: wet night blue, a gas lamp haloed through fog, a faint lit window across the way.
shader_type spatial;
render_mode unshaded, cull_disabled;
uniform vec3 sky : source_color = vec3(0.02, 0.035, 0.07);
uniform vec3 lamp : source_color = vec3(1.0, 0.72, 0.38);
uniform vec2 lamp_pos = vec2(0.72, 0.62);
float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
void fragment() {
	vec2 uv = UV;
	vec3 c = sky * (0.6 + 0.8 * uv.y);
	float d = length((uv - lamp_pos) * vec2(1.6, 1.0));
	c += lamp * (0.9 * exp(-d * 30.0) + 0.25 * exp(-d * 6.0));
	// opposite tenement: dark mass with a few warm windows
	float bld = step(uv.y, 0.85) * step(0.05, uv.x) * step(uv.x, 0.55);
	c = mix(c, vec3(0.01, 0.012, 0.02), bld * 0.85);
	// one lit window across the street, soft through the wet glass
	c += vec3(0.45, 0.28, 0.1) * 0.5 * exp(-length((uv - vec2(0.3, 0.55)) * vec2(9.0, 7.0)) * 3.0);
	// wet cobbles: a smear of the lamp reflected low in the frame
	c += lamp * 0.12 * exp(-abs(uv.x - lamp_pos.x) * 18.0) * smoothstep(0.25, 0.0, uv.y);
	ALBEDO = c;
}

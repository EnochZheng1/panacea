// A printed paper label wrapped around part of a bottle: border rules, a heavy title line and
// rows of small "type" that read as print at a distance. Discards outside the label's arc.
shader_type spatial;
uniform vec3 paper : source_color = vec3(0.86, 0.8, 0.64);
uniform vec3 inkc : source_color = vec3(0.12, 0.08, 0.06);
uniform vec3 accent : source_color = vec3(0.55, 0.1, 0.06);
uniform float arc = 0.42;
uniform float seed = 0.0;
float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7)) + seed * 13.1) * 43758.5453); }
void fragment() {
	float u = (UV.x - 0.5) / arc + 0.5;
	if (u < 0.0 || u > 1.0) discard;
	float v = UV.y;
	vec3 c = paper * (0.92 + 0.08 * hash(floor(UV * 80.0)));
	float border = step(u, 0.04) + step(0.96, u) + step(v, 0.06) + step(0.94, v);
	float rule = step(abs(v - 0.3), 0.01) + step(abs(v - 0.72), 0.008);
	float title = step(abs(v - 0.5), 0.1) * step(0.14, u) * step(u, 0.86) * step(0.25, hash(vec2(floor(u * 9.0), 1.0)));
	vec2 cell = vec2(floor(u * 26.0), floor(v * 22.0));
	float type = step(0.35, hash(cell)) * step(0.1, u) * step(u, 0.9) * (step(v, 0.26) + step(0.76, v) * step(v, 0.9)) * step(0.3, fract(v * 22.0));
	c = mix(c, inkc, clamp(border * 0.85 + rule + type * 0.6, 0.0, 1.0));
	c = mix(c, accent, title * 0.9);
	ALBEDO = c;
	ROUGHNESS = 0.85;
}

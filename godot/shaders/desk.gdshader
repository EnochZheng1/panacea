// The desk is black ink with the faint marks of the carving tool: gouged diagonal lines that
// break up unevenly, never quite flat. Kept very dark so the white slips carry the screen.
shader_type canvas_item;
uniform vec3 ink : source_color = vec3(0.051, 0.047, 0.043);
uniform vec3 cut : source_color = vec3(0.13, 0.12, 0.11);
float hash(vec2 p) { vec3 p3 = fract(vec3(p.xyx) * 0.1031); p3 += dot(p3, p3.yzx + 33.33); return fract((p3.x + p3.y) * p3.z); }
float noise(vec2 x) {
	vec2 i = floor(x); vec2 f = fract(x); f = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash(i), hash(i + vec2(1, 0)), f.x), mix(hash(i + vec2(0, 1)), hash(i + vec2(1, 1)), f.x), f.y);
}
void fragment() {
	vec2 p = FRAGCOORD.xy;
	float d = (p.x + p.y * 1.7) / 9.0;
	float line = smoothstep(0.42, 0.5, abs(fract(d) - 0.5));
	float breaks = smoothstep(0.35, 0.6, noise(vec2(floor(d), p.y * 0.012)));
	COLOR = vec4(mix(ink, cut, line * breaks * 0.55), 1.0);
}

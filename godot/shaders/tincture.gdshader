// A drop of tincture blooming in a glass of water. Several nested shells of this give a fake volume.
shader_type spatial;
render_mode blend_mix, depth_draw_never, cull_disabled, specular_disabled;

uniform vec3 ink : source_color = vec3(0.42, 0.02, 0.06);
uniform vec3 glow : source_color = vec3(1.0, 0.35, 0.1);
uniform float seed = 0.0;
uniform float density = 1.0;
uniform float height = 0.11;

varying vec3 p;

float hash(vec3 q) { q = fract(q * 0.3183099 + 0.1); q *= 17.0; return fract(q.x * q.y * q.z * (q.x + q.y + q.z)); }
float noise(vec3 x) {
	vec3 i = floor(x); vec3 f = fract(x); f = f * f * (3.0 - 2.0 * f);
	return mix(mix(mix(hash(i), hash(i + vec3(1,0,0)), f.x), mix(hash(i + vec3(0,1,0)), hash(i + vec3(1,1,0)), f.x), f.y),
	           mix(mix(hash(i + vec3(0,0,1)), hash(i + vec3(1,0,1)), f.x), mix(hash(i + vec3(0,1,1)), hash(i + vec3(1,1,1)), f.x), f.y), f.z);
}
float fbm(vec3 x) { float v = 0.0; float a = 0.5; for (int i = 0; i < 5; i++) { v += a * noise(x); x *= 2.03; a *= 0.5; } return v; }

void vertex() { p = VERTEX; }

void fragment() {
	float t = TIME * 0.35 + seed * 7.0;
	float y = p.y / height + 0.5;                      // 0 bottom .. 1 top
	// shells share one field (seed only nudges it), so they stack into a volume instead of streaks
	float ang = atan(p.z, p.x) + t * 0.5 + y * 1.4;    // a slow turn as it sinks
	float r = length(p.xz) / 0.04;
	vec3 q = vec3(cos(ang) * r, y * 1.3 + t * 0.3, sin(ang) * r) * 1.4 + seed * 0.15;
	float n = fbm(q + vec3(fbm(q * 0.6 - t * 0.1), fbm(q * 0.6 + 3.1), 0.0) * 1.6);
	// the drop entered at the top: dense there, thinning into soft tendrils as it sinks
	float fall = mix(0.62, 0.38, smoothstep(0.1, 0.95, y));
	float plume = smoothstep(fall, fall + 0.28, n) * smoothstep(0.02, 0.3, y) * smoothstep(1.0, 0.85, y);
	float a = clamp(plume * density, 0.0, 0.9);
	ALBEDO = mix(ink * 0.35, ink, plume);
	EMISSION = glow * plume * plume * 0.12;
	ROUGHNESS = 0.4;
	ALPHA = a;
}

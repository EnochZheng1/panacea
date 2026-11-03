// Letterpress pass over the whole screen: type and cuts bite unevenly into cheap newsprint.
// A little edge wobble, ink that misses in specks, paper grain and darkened edges.
shader_type canvas_item;
uniform sampler2D screen_tex : hint_screen_texture, filter_linear;
uniform vec3 paper : source_color = vec3(0.941, 0.769, 0.741);
uniform float wobble = 0.55;
uniform float speck = 0.16;
uniform float grain = 0.05;
uniform float burn = 0.2;

float hash(vec2 p) { vec3 p3 = fract(vec3(p.xyx) * 0.1031); p3 += dot(p3, p3.yzx + 33.33); return fract((p3.x + p3.y) * p3.z); }
float noise(vec2 x) {
	vec2 i = floor(x); vec2 f = fract(x); f = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash(i), hash(i + vec2(1, 0)), f.x), mix(hash(i + vec2(0, 1)), hash(i + vec2(1, 1)), f.x), f.y);
}
void fragment() {
	vec2 px = SCREEN_PIXEL_SIZE;
	vec2 p = SCREEN_UV / px;
	vec2 off = (vec2(noise(p * 0.08), noise(p * 0.08 + 17.0)) - 0.5) * wobble * px * 2.0;
	vec3 c = texture(screen_tex, SCREEN_UV + off).rgb;
	// ink coverage: how far this pixel is from bare paper
	float ink = clamp(1.0 - dot(c, vec3(0.333)) / dot(paper, vec3(0.333)), 0.0, 1.0);
	float miss = step(1.0 - speck * ink * (1.0 - 0.6 * smoothstep(0.85, 1.0, ink)), hash(floor(p * 0.7)));
	c = mix(c, paper, miss * 0.75);
	// paper grain and slow blotches
	c *= 1.0 - grain * (hash(floor(p)) - 0.5);
	c *= 1.0 - 0.06 * (noise(p * 0.004) - 0.5);
	vec2 d = SCREEN_UV - 0.5;
	c *= 1.0 - burn * smoothstep(0.35, 0.75, length(d * vec2(1.0, 1.25)));
	COLOR = vec4(c, 1.0);
}

// The impression: an inked roller passes down the sheet. Above it the edition is printed;
// below it the paper is still blank. Drawn over the page and removed when the roller is done.
shader_type canvas_item;
uniform float progress = 0.0;
uniform float band = 0.03;
uniform vec3 paper : source_color = vec3(0.937, 0.925, 0.894);
uniform vec3 ink : source_color = vec3(0.078, 0.071, 0.071);
void fragment() {
	float y = UV.y;
	if (y < progress - band) {
		// freshly printed: a faint wet sheen just behind the roller
		float wet = smoothstep(progress - band - 0.05, progress - band, y);
		COLOR = vec4(ink, 0.10 * wet);
	} else if (y < progress) {
		float t = (progress - y) / band;
		vec3 c = mix(ink, vec3(0.32), 0.35 * sin(t * 3.14159));
		COLOR = vec4(c, 1.0);
	} else {
		COLOR = vec4(paper, 1.0);
	}
}

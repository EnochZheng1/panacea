// The ruled tint printed behind an engraved cut.
shader_type canvas_item;
uniform vec4 ink : source_color = vec4(0.086, 0.067, 0.071, 0.26);
uniform float spacing = 7.0;
void fragment() {
	float y = FRAGCOORD.y;
	float line = step(spacing - 1.4, mod(y, spacing));
	COLOR = vec4(ink.rgb, ink.a * line);
}

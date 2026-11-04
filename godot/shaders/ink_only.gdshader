// Prints a cut in black ink only: the spot colour (Scheele's green) plate is left off.
shader_type canvas_item;
void fragment() {
	vec4 c = texture(TEXTURE, UV);
	float l = dot(c.rgb, vec3(0.299, 0.587, 0.114));
	COLOR = vec4(vec3(smoothstep(0.25, 0.8, l)) * vec3(0.937, 0.925, 0.894) + vec3(0.03) * (1.0 - smoothstep(0.25, 0.8, l)), c.a);
}

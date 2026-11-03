/**
 * The "engraving" composite: turns the rendered scene into cross-hatched ink on paper, then lays
 * the liquids back over it in full, glowing colour. The medicine is the only colour in the world.
 *
 * Inputs: the colour pass (with depth), a view-space normal pass (for ink outlines) and a liquid
 * mask pass (liquids in colour, everything else black).
 */
export const inkVertexShader = /* glsl */ `
varying vec2 vUv;
void main() {
  vUv = uv;
  gl_Position = vec4(position.xy, 0.0, 1.0);
}
`;

export const inkFragmentShader = /* glsl */ `
precision highp float;

uniform sampler2D tColor;
uniform sampler2D tDepth;
uniform sampler2D tNormal;
uniform sampler2D tMask;
uniform vec2 resolution;
uniform float pixelRatio;
uniform float cameraNear;
uniform float cameraFar;
uniform float time;
uniform float inkEnabled;
uniform vec3 paperColor;
uniform vec3 inkColor;

varying vec2 vUv;

float hash(vec2 p) {
  p = fract(p * vec2(123.34, 456.21));
  p += dot(p, p + 45.32);
  return fract(p.x * p.y);
}

float noise(vec2 p) {
  vec2 i = floor(p);
  vec2 f = fract(p);
  float a = hash(i);
  float b = hash(i + vec2(1.0, 0.0));
  float c = hash(i + vec2(0.0, 1.0));
  float d = hash(i + vec2(1.0, 1.0));
  vec2 u = f * f * (3.0 - 2.0 * f);
  return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

float linearDepth(float z) {
  float ndc = z * 2.0 - 1.0;
  return (2.0 * cameraNear * cameraFar) / (cameraFar + cameraNear - ndc * (cameraFar - cameraNear));
}

/* An engraved line set: line width grows with darkness, like a burin cutting deeper. */
float hatch(vec2 p, float angle, float spacing, float darkness, float wobble) {
  vec2 dir = vec2(cos(angle), sin(angle));
  float d = dot(p, dir) + wobble;
  float f = abs(fract(d / spacing) - 0.5) * 2.0;
  float width = clamp(darkness, 0.0, 1.0);
  float aa = 1.5 / spacing;
  return 1.0 - smoothstep(width - aa, width + aa, f);
}

/* Offscreen passes are linear HDR; tone-map here (ACES approximation). */
vec3 aces(vec3 x) {
  return clamp((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14), 0.0, 1.0);
}

float lumAt(vec2 uv) {
  return pow(dot(aces(texture2D(tColor, uv).rgb * 1.1), vec3(0.2126, 0.7152, 0.0722)), 1.0 / 2.2);
}

vec3 sampleNormal(vec2 uv) {
  return texture2D(tNormal, uv).xyz * 2.0 - 1.0;
}

void main() {
  vec2 texel = 1.0 / resolution;
  vec4 color = vec4(aces(texture2D(tColor, vUv).rgb * 1.1), 1.0);
  vec3 liquid = texture2D(tMask, vUv).rgb;
  float liquidAmount = max(liquid.r, max(liquid.g, liquid.b));

  if (inkEnabled < 0.5) {
    vec3 plain = mix(color.rgb, liquid * 1.15, step(0.004, liquidAmount) * 0.85);
    gl_FragColor = vec4(plain, 1.0);
    #include <colorspace_fragment>
    return;
  }

  // --- Tone ---------------------------------------------------------------------
  // Perceptual luminance (the colour pass is linear).
  float lum = pow(dot(color.rgb, vec3(0.2126, 0.7152, 0.0722)), 1.0 / 2.2);
  lum = clamp((lum - 0.03) * 1.45, 0.0, 1.0);
  float dark = 1.0 - lum;

  // --- Ink outlines from depth and normal discontinuities --------------------------
  float d0 = linearDepth(texture2D(tDepth, vUv).x);
  float edge = 0.0;
  float rawLum = lumAt(vUv);
  float detail = 0.0;
  vec3 n0 = sampleNormal(vUv);
  for (int i = 0; i < 4; i++) {
    vec2 o = i == 0 ? vec2(1.0, 0.0) : i == 1 ? vec2(-1.0, 0.0) : i == 2 ? vec2(0.0, 1.0) : vec2(0.0, -1.0);
    vec2 uv = vUv + o * texel * pixelRatio;
    float di = linearDepth(texture2D(tDepth, uv).x);
    edge = max(edge, smoothstep(0.02, 0.06, abs(di - d0) / max(d0, 0.001)));
    edge = max(edge, smoothstep(0.25, 0.6, 1.0 - dot(n0, sampleNormal(uv))));
    // Fine detail the hatching would miss: lettering, dial ticks, borders.
    detail = max(detail, lumAt(uv) - rawLum);
  }

  // --- Cross-hatching in screen space ----------------------------------------------
  vec2 p = gl_FragCoord.xy / pixelRatio;
  float wob = (noise(p * 0.035) - 0.5) * 2.2;
  float spacing = 6.5;
  float ink = 0.0;
  ink = max(ink, hatch(p, 0.785, spacing, smoothstep(0.32, 1.0, dark) * 0.5, wob));
  ink = max(ink, hatch(p, -0.785, spacing, smoothstep(0.52, 1.0, dark) * 0.5, wob * 0.7));
  ink = max(ink, hatch(p, 0.0, spacing * 1.3, smoothstep(0.72, 1.0, dark) * 0.45, wob * 0.5));
  ink = max(ink, smoothstep(0.93, 1.0, dark) * 0.8);
  ink = max(ink, edge * 0.95);
  ink = max(ink, smoothstep(0.1, 0.22, detail) * 0.95);

  // --- Paper --------------------------------------------------------------------
  float grain = noise(p * 0.9) * 0.5 + noise(p * 0.12) * 0.5;
  vec3 paper = paperColor * (0.9 + 0.1 * grain);
  // Keep a whisper of the scene's own hue so brass reads warmer than wood.
  vec3 tint = mix(vec3(lum), color.rgb, 0.35);
  paper *= mix(vec3(1.0), tint / max(lum, 0.05) * 0.9 + 0.1, 0.18);
  vec3 outColor = mix(paper, inkColor, ink * (0.82 + 0.18 * grain));

  // --- The liquids: saturated, glowing, untouched by ink ---------------------------
  vec3 glow = vec3(0.0);
  float total = 0.0;
  for (int i = 0; i < 12; i++) {
    float a = float(i) * 0.5235988;
    for (int r = 1; r <= 2; r++) {
      vec2 o = vec2(cos(a), sin(a)) * float(r) * 7.0 * texel * pixelRatio;
      float w = 1.0 / float(r);
      glow += texture2D(tMask, vUv + o).rgb * w;
      total += w;
    }
  }
  glow /= total;
  float shimmer = 0.92 + 0.08 * sin(time * 1.7 + p.y * 0.05);
  outColor = mix(outColor, liquid * 1.25 * shimmer, smoothstep(0.004, 0.05, liquidAmount));
  outColor += glow * 0.9;

  // --- Vignette, like an old plate --------------------------------------------------
  vec2 c = vUv - 0.5;
  outColor *= 1.0 - dot(c, c) * 0.55;

  gl_FragColor = vec4(outColor, 1.0);
  #include <colorspace_fragment>
}
`;

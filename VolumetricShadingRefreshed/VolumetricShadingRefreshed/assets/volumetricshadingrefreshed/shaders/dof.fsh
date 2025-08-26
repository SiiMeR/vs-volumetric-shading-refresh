// dof.fsh — GLSL 1.20 style
// #version 120   // optional; safe to omit if the engine injects it

uniform sampler2D uScene;
uniform sampler2D uDepth;
uniform float uFocusDepth;// 0..1 linearized depth range to keep sharp
uniform float uBlurRange;// 0..1 half-width around focus that blends to blur

varying vec2 vUv;// passed from the vertex shader

void main() {
    float depth = texture2D(uDepth, vUv).r;

    // how far from the focus plane (0 = sharp, 1 = fully blurred)
    float factor = clamp(abs(depth - uFocusDepth) / max(uBlurRange, 1e-6), 0.0, 1.0);

    vec4 original = texture2D(uScene, vUv);

    // small box blur kernel (9 taps). You can tweak the step (0.002) or add more taps.
    vec4 accum = vec4(0.0);
    float w = 1.0;
    float total = 0.0;
    vec2 step = vec2(0.002, 0.002);// approx. 2px at 1k res; scale with resolution if you like

    for (int x = -1; x <= 1; x++) {
        for (int y = -1; y <= 1; y++) {
            accum += texture2D(uScene, vUv + vec2(x, y) * step) * w;
            total += w;
        }
    }

    vec4 blurred = accum / total;

    // blend sharp/blurred based on factor
    gl_FragColor = mix(original, blurred, factor);
}

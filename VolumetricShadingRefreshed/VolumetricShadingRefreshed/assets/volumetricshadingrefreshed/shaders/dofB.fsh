#version 330 core

uniform sampler2D blurPass;
uniform float dofStrength;

in vec2 texcoord;
out vec4 outColor;

#if VSMOD_DOF_ENABLED == 1
const int TAPS = 17;
#endif

void main(void) {
#if VSMOD_DOF_ENABLED == 1
    vec2 texelSize = vec2(0.0, 1.0 / textureSize(blurPass, 0).y);
    float coc = texture(blurPass, texcoord).a;
    float blurRadius = coc * dofStrength;

    vec4 col = vec4(0.0);
    float w = 0.0;

    for (int i = 0; i < TAPS; i++) {
        float t = (float(i) / float(TAPS - 1)) * 2.0 - 1.0;
        vec4 s = texture(blurPass, texcoord + texelSize * blurRadius * t);
        float luma = dot(s.rgb, vec3(0.2126, 0.7152, 0.0722));
        float wt = 1.0 + luma * luma;
        col += s * wt;
        w += wt;
    }

    outColor = vec4((col / w).rgb, 1.0);
#else
    outColor = vec4(texture(blurPass, texcoord).rgb, 1.0);
#endif
}

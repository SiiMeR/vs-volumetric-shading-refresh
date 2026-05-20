#version 330 core

uniform sampler2D primaryScene;
uniform sampler2D depthTexture;
uniform mat4 invProjectionMatrix;
uniform vec2 dofFocusUV;
uniform float dofStrength;
uniform float dofFocusRange;
uniform float dofAdaptiveRange;
#if VSMOD_DOF_AUTOFOCUS == 1
uniform float dofSmoothDepth;
#else
uniform float dofFocusDistance;
#endif

in vec2 texcoord;
out vec4 outColor;

#if VSMOD_DOF_ENABLED == 1

const int NUM_SAMPLES = 25;

vec3 viewPos(vec2 uv, float d) {
    vec4 clip = vec4(uv * 2.0 - 1.0, d * 2.0 - 1.0, 1.0);
    vec4 view = invProjectionMatrix * clip;
    return view.xyz / view.w;
}

vec2 vogelDisk(int i) {
    float r = sqrt((float(i) + 0.5) / float(NUM_SAMPLES));
    float theta = float(i) * 2.39996;
    return vec2(r * cos(theta), r * sin(theta));
}

#endif

void main(void) {
#if VSMOD_DOF_ENABLED == 1
    vec2 texelSize = 1.0 / vec2(textureSize(primaryScene, 0));

    float rawDepth = texture(depthTexture, texcoord).r;

    if (rawDepth >= 1.0) {
        outColor = texture(primaryScene, texcoord);
        return;
    }

    #if VSMOD_DOF_AUTOFOCUS == 1
        vec3 focusViewPos = viewPos(dofFocusUV, dofSmoothDepth);
        float focusDist = max(length(focusViewPos), 0.001);
        focusViewPos = focusViewPos * (min(focusDist, 200.0) / focusDist);
        focusDist = min(focusDist, 200.0);
        float effectiveFocusRange = dofFocusRange * max(1.0, focusDist * dofAdaptiveRange);
        float dist = length(viewPos(texcoord, rawDepth) - focusViewPos);
    #else
        float pixelDepth = -viewPos(texcoord, rawDepth).z;
        float effectiveFocusRange = dofFocusRange * max(1.0, dofFocusDistance * dofAdaptiveRange);
        float dist = abs(pixelDepth - dofFocusDistance);
    #endif

    float coc = clamp(dist / effectiveFocusRange, 0.0, 1.0);
    float blurRadius = coc * dofStrength;

    vec4 col = vec4(0.0);
    float w = 0.0;

    for (int i = 0; i < NUM_SAMPLES; i++) {
        vec4 s = texture(primaryScene, texcoord + vogelDisk(i) * texelSize * blurRadius);
        float luma = dot(s.rgb, vec3(0.2126, 0.7152, 0.0722));
        float wt = 1.0 + luma * luma;
        col += s * wt;
        w += wt;
    }

    outColor = col / w;
#else
    outColor = texture(primaryScene, texcoord);
#endif
}

#version 330 core

uniform sampler2D inputTexture;
uniform sampler2D glowParts;
uniform sampler2D depthTexture;
uniform float vsmod_volumetricIntensity;
uniform vec3 sunPos3dIn;
uniform mat4 invProjectionMatrix;
uniform mat4 invModelViewMatrix;

in vec2 texCoord;
in vec3 sunPosScreen;
in float iGlobalTime;
in float direction;
in vec3 frontColor;
in vec3 backColor;

out vec4 outColor;

#include printvalues.fsh

vec4 applyVolumetricLighting(in vec3 color, in vec2 uv, float intensity) {
    vec2 texelSize = 1.0 / textureSize(glowParts, 0);
    float centerDepth = texture(depthTexture, uv).r;
    float centerVGR = texture(glowParts, uv).g;

    vec4 blurredVGR = vec4(0.0);
    float totalWeight = 0.0;

    for (float i = -2.0; i <= 2.0; i++) {
        for (float j = -2.0; j <= 2.0; j++) {
            vec2 sampleUV = uv + vec2(i, j) * texelSize;
            vec4 s = texture(glowParts, sampleUV);
            float sampleDepth = texture(depthTexture, sampleUV).r;
            float wt = exp(-abs(sampleDepth - centerDepth) * 50.0);
            blurredVGR += s * wt;
            totalWeight += wt;
        }
    }

    blurredVGR /= max(totalWeight, 0.0001);
    blurredVGR.g = max(blurredVGR.g, centerVGR);

    vec3 vgrC = color * 1.05 * vsmod_volumetricIntensity * blurredVGR.g;
    return vec4(vgrC, 1.0);
}

void main(void) {
    vec4 proCoord = invProjectionMatrix * vec4(texCoord * 2.0 - 1.0, -1.0, 1);
    proCoord.xyz /= proCoord.w;
    proCoord.w = 0;
    proCoord = invModelViewMatrix * proCoord;

    float dp = dot(normalize(sunPos3dIn), normalize(proCoord.xyz));
    vec3 useColor = mix(backColor, frontColor, dp * 0.5 + 0.5);
    outColor = applyVolumetricLighting(useColor, texCoord, vsmod_volumetricIntensity);
}

#version 330 core

uniform sampler2D inputTexture;
uniform sampler2D glowParts;
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
    float vgr = texture(glowParts, uv).g;
    
    // Apply Gaussian blur to smooth out the volumetric lighting
    vec2 texelSize = 1.0 / textureSize(glowParts, 0);
    vec4 blurredVGR = vec4(0.0);
    float blurRadius = 2.0; // Adjust the radius as needed

    for (float i = -blurRadius; i <= blurRadius; i++) {
        for (float j = -blurRadius; j <= blurRadius; j++) {
            vec2 offset = vec2(i, j) * texelSize;
            blurredVGR += texture(glowParts, uv + offset);
        }
    }

    blurredVGR /= pow((2.0 * blurRadius + 1.0), 2.0);

    vec3 vgrC = color * 1.05 * VOLUMETRIC_INTENSITY * blurredVGR.g;
    return vec4(vgrC, 1.0);
}

void main(void) {
    vec4 proCoord = invProjectionMatrix * vec4(texCoord * 2.0 - 1.0, -1.0, 1);
    proCoord.xyz /= proCoord.w;
    proCoord.w = 0;
    proCoord = invModelViewMatrix * proCoord;

    float dp = dot(normalize(sunPos3dIn), normalize(proCoord.xyz));
    vec3 useColor = mix(backColor, frontColor, dp * 0.5 + 0.5);
    outColor = applyVolumetricLighting(useColor, texCoord, VOLUMETRIC_INTENSITY);
}

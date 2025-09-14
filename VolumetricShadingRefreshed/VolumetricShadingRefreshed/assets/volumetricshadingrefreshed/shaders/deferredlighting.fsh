#version 330 core

// AMD Compatibility: Add explicit precision qualifiers
precision highp float;
precision highp int;
precision highp sampler2D;

uniform sampler2D gDepth;
uniform sampler2D gNormal;
uniform sampler2D inColor;
uniform sampler2D inGlow;

uniform highp mat4 invProjectionMatrix;
uniform highp mat4 invModelViewMatrix;

uniform mediump float dayLight;
uniform highp vec3 sunPosition;

in mediump vec2 texcoord;
out mediump vec4 outColor;
out mediump vec4 outGlow;

uniform mediump float fogDensityIn;
uniform mediump float fogMinIn;
uniform mediump vec4 rgbaFog;

#include fogandlight.fsh
#include deferredfogandlight.fsh

void main(void)
{
    highp float projectedZ = texture(gDepth, texcoord).r;
    highp vec4 normal = texture(gNormal, texcoord);
    mediump vec4 color = texture(inColor, texcoord);
    mediump vec4 glowVec = texture(inGlow, texcoord);

    #if SHADOWQUALITY > 0
    mediump float intensity = 0.34 + (1.0 - shadowIntensity) / 8.0; // this was 0.45, which makes shadow acne visible on blocks
    #else
    mediump float intensity = 0.45;
    #endif

    if (projectedZ < 1.0) {
        highp vec4 screenPosition = vec4(vec3(texcoord, projectedZ) * 2.0 - 1.0, 1.0);
        screenPosition = invProjectionMatrix * screenPosition;
        screenPosition.xyz /= screenPosition.w;
        screenPosition.w = 1.0;
        highp vec4 worldPosition = invModelViewMatrix * screenPosition;
        highp vec4 cameraWorldPos = invModelViewMatrix * vec4(0.0, 0.0, 0.0, 1.0);
        highp vec4 worldNormal = invModelViewMatrix * vec4(normal.xyz, 0.0);

        mediump float fog = getFogLevelDeferred(length(screenPosition), fogMinIn, fogDensityIn, worldPosition.y);
        color = applyOverexposedFogAndShadowDeferred(worldPosition, color, fog, worldNormal.xyz,
        1.0, intensity, fogDensityIn, glowVec.b, glowVec.r);

        glowVec.y = calculateVolumetricScatterDeferred(worldPosition, cameraWorldPos);
    }

    glowVec.z = 0.0;
    outColor = color;
    outGlow = glowVec;
}
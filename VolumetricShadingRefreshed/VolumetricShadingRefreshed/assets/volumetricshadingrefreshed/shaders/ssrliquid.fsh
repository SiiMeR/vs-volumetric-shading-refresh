#version 330 core

// AMD Compatibility: Add explicit precision qualifiers
precision highp float;
precision highp int;
precision highp sampler2D;

uniform sampler2D terrainTex;
uniform highp vec3 playerpos;
uniform highp mat4 modelViewMatrix;
uniform mediump float dropletIntensity = 0.0;
uniform mediump float playerUnderwater;
uniform highp vec4 cameraWorldPosition;
const mediump vec4 rgbaFog = vec4(0.0);

in highp vec4 worldPos;
in highp vec4 fragPosition;
in highp vec3 fragWorldPos;
in highp vec4 gnormal;
in highp vec3 worldNormal;
in mediump vec2 flowVectorf;
in mediump vec2 uv;
flat in int waterFlags;
flat in mediump float alpha;
flat in int skyExposed;

layout(location = 0) out vec4 outGPosition;
layout(location = 1) out vec4 outGNormal;
layout(location = 2) out vec4 outTint;
#if VSMOD_REFRACT > 0
layout(location = 3) out vec4 outRefraction;
#endif

#include colormap.fsh
#include fogandlight.fsh
#include noise3d.ash
#include wavenoise.ash
#generated dropletnoise

void generateNoiseBump(inout highp vec3 normalMap, highp vec3 position, mediump float div) {
    const highp vec3 offset = vec3(0.05, 0.0, 0.0);
    highp vec3 posCenter = position.xyz;
    highp vec3 posNorth = posCenter - offset.zyx;
    highp vec3 posEast = posCenter + offset.xzy;

    mediump float val0 = generateWaveNoise(posCenter, div);
    mediump float val1 = generateWaveNoise(posNorth, div);
    mediump float val2 = generateWaveNoise(posEast, div);

    mediump float zDelta = (val0 - val1);
    mediump float xDelta = (val2 - val0);

    normalMap += vec3(xDelta * 0.5, zDelta * 0.5, 0.0);
}

void generateNoiseParallax(inout highp vec3 normalMap, highp vec3 viewVector, mediump float div, out highp vec3 parallaxPos) {
    highp vec3 targetPos = fragWorldPos.xyz;

    mediump float currentNoise = generateWaveNoise(fragWorldPos.xyz, div);
    targetPos.xz += (currentNoise * viewVector.xy) * 0.4;

    generateNoiseBump(normalMap, targetPos, div);
    parallaxPos = targetPos;
}

mediump float generateSplash(highp vec3 pos)
{
    highp vec3 localPos = fract(pos.xyz / 512.0) * 512.0;
    mediump vec2 uvSplash = 5.0 * pos.xz;

    return dropletnoise(uvSplash, waterWaveCounter);
}

void generateSplashBump(inout highp vec3 normalMap, highp vec3 pos)
{
    const highp vec3 deltaPos = vec3(0.01, 0.0, 0.0);
    highp vec3 startPos = pos - deltaPos.xyx * 0.5;

    mediump float val0 = generateSplash(startPos);
    mediump float val1 = generateSplash(startPos + deltaPos.xyz);
    mediump float val2 = generateSplash(startPos + deltaPos.zyx);

    mediump float xDelta = (val1 - val0);
    mediump float zDelta = (val2 - val0);

    normalMap += vec3(xDelta, zDelta, 0.0) * 0.75;
}

// https://gamedev.stackexchange.com/questions/86530/is-it-possible-to-calculate-the-tbn-matrix-in-the-fragment-shader
highp mat3 cotangentFrame(highp vec3 N, highp vec3 p, mediump vec2 uvCoord) {
    highp vec3 dp1 = dFdx(p);
    highp vec3 dp2 = dFdy(p);
    mediump vec2 duv1 = dFdx(uvCoord);
    mediump vec2 duv2 = dFdy(uvCoord);

    highp vec3 dp2perp = cross(dp2, N);
    highp vec3 dp1perp = cross(N, dp1);
    highp vec3 T = dp2perp * duv1.x + dp1perp * duv2.x;
    highp vec3 B = dp2perp * duv1.y + dp1perp * duv2.y;

    highp float invmax = inversesqrt(max(dot(T, T), dot(B, B)));
    return mat3(T * invmax, B * invmax, N);
}

void main()
{
    // AMD Compatibility: Fix implicit type conversions
    mediump float isWater = ((waterFlags & (1<<25)) > 0) ? 0.0 : 1.0;
    mediump float myAlpha = alpha * isWater;
    if (myAlpha < 0.5) discard;

    // apply waves
    mediump float caustics = length(flowVectorf) > 0.001 ? 0.0 : 1.0;
    mediump float div = float(((waterFlags & (1<<27)) > 0) ? 90 : 10);

    highp mat3 tbn = cotangentFrame(worldNormal, worldPos.xyz, uv);
    highp mat3 invTbn = transpose(tbn);

    highp vec3 normalMap = vec3(0.0);
    highp vec3 parallaxPos;
    highp vec3 viewTangent = normalize(invTbn * (worldPos.xyz - cameraWorldPosition.xyz));
    generateNoiseParallax(normalMap, viewTangent, div, parallaxPos);

    if (dropletIntensity > 0.001) {
        generateSplashBump(normalMap, parallaxPos);
    }

    highp vec3 worldNormalMap = tbn * normalMap;
    highp vec3 camNormalMap = (modelViewMatrix * vec4(worldNormalMap, 0.0)).xyz;
    highp vec3 myGNormal = gnormal.xyz;

    if (dot(gnormal.xyz, fragPosition.xyz) > 0.0) {
        // flip the normal if viewed from behind
        myGNormal = -gnormal.xyz;
    }

    outGPosition = vec4(fragPosition.xyz, 0.0);
    outGNormal = vec4(normalize(camNormalMap * 2.0 + myGNormal), 1.0 - playerUnderwater * caustics);
    outTint = vec4(getColorMapped(terrainTex, vec4(1.0)).rgb, 0.0);
    #if VSMOD_REFRACT > 0
    outRefraction = vec4((-camNormalMap.xy * 1.2) / fragPosition.z, 0.0, 0.0);
    #endif
}
#version 330 core

uniform sampler2D primaryScene;

uniform sampler2D gPosition;
uniform sampler2D gNormal;
uniform sampler2D gTint;
uniform sampler2D gDepth;

uniform mat4 projectionMatrix;
uniform float vsmod_ssrReflectionDimming;
uniform float vsmod_ssrTintInfluence;
uniform float vsmod_ssrSkyMixin;
uniform mat4 invProjectionMatrix;
uniform mat4 invModelViewMatrix;

uniform vec3 sunPosition;
uniform float dayLight;
uniform float horizonFog;
uniform float fogDensityIn;
uniform float fogMinIn;
uniform vec4 rgbaFog;
uniform float vsmod_ssrDistortion;
uniform float vsmod_ssrStrength;
uniform float waterWaveCounter;
uniform vec3 playerPos;

in vec2 texcoord;
out vec4 outColor;

#include dither.fsh
#include fogandlight.fsh
#include skycolor.fsh
#include deferredfogandlight.fsh

float comp = 1.0-zNear/zFar/zFar;

const float ref = 0.11;
const float inc = 3.0;

float ssrNoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = fract(sin(dot(i,              vec2(127.1, 311.7))) * 43758.5453);
    float b = fract(sin(dot(i + vec2(1, 0), vec2(127.1, 311.7))) * 43758.5453);
    float c = fract(sin(dot(i + vec2(0, 1), vec2(127.1, 311.7))) * 43758.5453);
    float d = fract(sin(dot(i + vec2(1, 1), vec2(127.1, 311.7))) * 43758.5453);
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y) * 2.0 - 1.0;
}

vec3 nvec3(vec4 pos) {
    return pos.xyz/pos.w;
}
vec4 nvec4(vec3 pos) {
    return vec4(pos.xyz, 1.0);
}
float cdist(vec2 coord) {
    return max(abs(coord.s-0.5), abs(coord.t-0.5))*2.0;
}

vec4 raytrace(vec3 fragpos, vec3 rvector, float jitter) {
    vec4 color = vec4(0.0);
    vec3 start = fragpos;
    rvector *= 1.2;
    fragpos += rvector * (1.0 + jitter);
    vec3 tvector = rvector * (1.0 + jitter);
    int sr = 0;

    bool hit = false;
    vec3 hitFragpos0 = vec3(0);
    vec3 hitPos = vec3(0);

    for (int i = 0; i < 10; ++i) {
        vec3 pos = nvec3(projectionMatrix * nvec4(fragpos)) * 0.5 + 0.5;
        if (pos.x < 0 || pos.x > 1 || pos.y < 0 || pos.y > 1 || pos.z < 0 || pos.z > 1.0) break;
        vec3 fragpos0 = vec3(pos.st, texture(gDepth, pos.st).r);
        fragpos0 = nvec3(invProjectionMatrix * nvec4(fragpos0 * 2.0 - 1.0));
        float err = distance(fragpos, fragpos0);
        bool isFurther = fragpos0.z < start.z;
        if (err < pow(length(rvector), 1.175) && isFurther) {
            hit = true;
            hitFragpos0 = fragpos0;
            hitPos = pos;
            sr++;

            if (sr >= 1){
                break;
            }

            tvector -= rvector;
            rvector *= ref;
        }
        rvector *= inc;
        tvector += rvector;
        fragpos = start + tvector;
    }

    if (hit) {
        color = pow(texture(primaryScene, hitPos.st), vec4(vsmod_ssrReflectionDimming));
        color.a = clamp(1.0 - pow(cdist(hitPos.st), 20.0), 0.0, 1.0);
    }

    return color;
}

void main(void) {
    vec4 positionFrom = texture(gPosition, texcoord);
    vec3 unitPositionFrom = normalize(positionFrom.xyz);
    vec3 normal = normalize(texture(gNormal, texcoord).xyz);
    vec3 pivot = normalize(reflect(unitPositionFrom, normal));

    outColor = vec4(0);

    if (positionFrom.w < 1.0) {
        vec3 positionFromUV = nvec3(projectionMatrix * positionFrom) * 0.5 + 0.5;
        vec3 positionFromDepth = vec3(positionFromUV.xy, texture(gDepth, positionFromUV.xy).r);
        positionFromDepth = nvec3(invProjectionMatrix * nvec4(positionFromDepth * 2.0 - 1.0));

        if (positionFromDepth.z > positionFrom.z + 0.01) {
            // this point in the reflection is occluded by something, maybe an item the player is holding
            return;
        }

        vec4 worldPos = invModelViewMatrix * positionFrom;
        float jitter = fract(sin(dot(worldPos.xz, vec2(12.9898, 78.233))) * 43758.5453);

        float distAmt = vsmod_ssrDistortion * 0.002;
        float t = waterWaveCounter * 0.3;
        vec2 wp = worldPos.xz + playerPos.xz;
        float dnx = ssrNoise(vec2(wp.x * 4.0 - t, wp.y * 4.0))
                  + ssrNoise(vec2(wp.x * 1.7 + 3.1, wp.y * 1.7 - t * 0.7)) * 0.5;
        float dnz = ssrNoise(vec2(wp.y * 4.0 + 8.3, wp.x * 4.0 + t))
                  + ssrNoise(vec2(wp.y * 1.7 - 2.4, wp.x * 1.7 + t * 0.7)) * 0.5;
        vec3 distortedNormal = normalize(normal + vec3(dnx, 0.0, dnz) * distAmt);
        pivot = normalize(reflect(unitPositionFrom, distortedNormal));

        vec4 reflection = raytrace(positionFrom.xyz, pivot, jitter);
        vec4 skyColor = vec4(0);
        vec4 outGlow = vec4(0);

        vec4 worldNormal = invModelViewMatrix * vec4(normal, 0.0);
        float upness = clamp(dot(worldNormal.xyz, vec3(0, 1, 0)), 0, 1);

        pivot = (invModelViewMatrix * vec4(pivot, 0.0)).xyz;
        getSkyColorAt(pivot, sunPosition, 0.0, clamp(dayLight, 0, 1), horizonFog, skyColor, outGlow);
        skyColor.rgb = pow(skyColor.rgb, vec3(vsmod_ssrReflectionDimming));
        reflection.rgb = mix(reflection.rgb, skyColor.rgb, vsmod_ssrSkyMixin * upness);
        reflection.rgb = mix(skyColor.rgb * upness, reflection.rgb, reflection.a);

        float normalDotEye = dot(normal, unitPositionFrom);
        float fresnel = pow(clamp(1.0 + normalDotEye, 0.0, 1.0), 4.0);
        fresnel = mix(0.09, 1.0, fresnel);

        outColor = reflection;
        outColor.a = 1.0f;

        outColor.rgb *= pow(texture(gTint, texcoord).rgb, vec3(vsmod_ssrTintInfluence));

        vec4 positionFromWorldSpace = invModelViewMatrix * vec4(positionFrom.xyz, 1.0);
        float fogLevel = getFogLevelDeferred(length(positionFrom), fogMinIn, fogDensityIn, positionFromWorldSpace.y);
        outColor = applyFog(outColor, fogLevel);

        outColor.a *= (1.0f - positionFrom.w) * fresnel * vsmod_ssrStrength;
    }

    //outColor.rgb = normal;
    //outColor.a = 1;
}
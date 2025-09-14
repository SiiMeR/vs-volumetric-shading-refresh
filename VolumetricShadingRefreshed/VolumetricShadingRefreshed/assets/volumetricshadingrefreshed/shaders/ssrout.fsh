#version 330 core

// AMD Compatibility: Add explicit precision qualifiers
precision highp float;
precision highp int;
precision highp sampler2D;

uniform sampler2D primaryScene;

uniform sampler2D gPosition;
uniform sampler2D gNormal;
uniform sampler2D gTint;
uniform sampler2D gDepth;

uniform highp mat4 projectionMatrix;
uniform highp mat4 invProjectionMatrix;
uniform highp mat4 invModelViewMatrix;

uniform highp vec3 sunPosition;
uniform mediump float dayLight;
uniform mediump float horizonFog;
uniform mediump float fogDensityIn;
uniform mediump float fogMinIn;
uniform mediump vec4 rgbaFog;

in mediump vec2 texcoord;
out mediump vec4 outColor;

#include dither.fsh
#include fogandlight.fsh
#include skycolor.fsh
#include deferredfogandlight.fsh

mediump float comp = 1.0 - zNear/zFar/zFar;

const int maxf = 7; // number of refinements
const mediump float ref = 0.11; // refinement multiplier
const mediump float inc = 3.0; // increasement factor at each step

highp vec3 nvec3(highp vec4 pos) {
    return pos.xyz / pos.w;
}
highp vec4 nvec4(highp vec3 pos) {
    return vec4(pos.xyz, 1.0);
}
mediump float cdist(mediump vec2 coord) {
    return max(abs(coord.s - 0.5), abs(coord.t - 0.5)) * 2.0;
}

mediump vec4 raytrace(highp vec3 fragpos, highp vec3 rvector) {
    mediump vec4 color = vec4(0.0);
    highp vec3 start = fragpos;
    rvector *= 1.2;
    fragpos += rvector;
    highp vec3 tvector = rvector;
    int sr = 0;

    bool hit = false;
    highp vec3 hitFragpos0 = vec3(0.0);
    mediump vec3 hitPos = vec3(0.0);

    // AMD Compatibility: Use constant loop bounds
    for (int i = 0; i < 25; ++i) {
        highp vec3 pos = nvec3(projectionMatrix * nvec4(fragpos)) * 0.5 + 0.5;
        if (pos.x < 0.0 || pos.x > 1.0 || pos.y < 0.0 || pos.y > 1.0 || pos.z < 0.0 || pos.z > 1.0) break;
        
        highp vec3 fragpos0 = vec3(pos.st, texture(gDepth, pos.st).r);
        fragpos0 = nvec3(invProjectionMatrix * nvec4(fragpos0 * 2.0 - 1.0));
        mediump float err = distance(fragpos, fragpos0);
        bool isFurther = fragpos0.z < start.z;
        
        if (err < pow(length(rvector), 1.175) && isFurther) {
            hit = true;
            hitFragpos0 = fragpos0;
            hitPos = pos;
            sr++;

            if (sr >= maxf) {
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
        color = pow(texture(primaryScene, hitPos.st), vec4(VSMOD_SSR_REFLECTION_DIMMING));
        color.a = clamp(1.0 - pow(cdist(hitPos.st), 20.0), 0.0, 1.0);
    }

    return color;
}

void main(void) {
    highp vec4 positionFrom = texture(gPosition, texcoord);
    highp vec3 unitPositionFrom = normalize(positionFrom.xyz);
    highp vec3 normal = normalize(texture(gNormal, texcoord).xyz);
    highp vec3 pivot = normalize(reflect(unitPositionFrom, normal));

    outColor = vec4(0.0);

    if (positionFrom.w < 1.0) {
        highp vec3 positionFromUV = nvec3(projectionMatrix * positionFrom) * 0.5 + 0.5;
        highp vec3 positionFromDepth = vec3(positionFromUV.xy, texture(gDepth, positionFromUV.xy).r);
        positionFromDepth = nvec3(invProjectionMatrix * nvec4(positionFromDepth * 2.0 - 1.0));

        if (positionFromDepth.z > positionFrom.z + 0.01) {
            // this point in the reflection is occluded by something, maybe an item the player is holding
            return;
        }

        mediump vec4 reflection = raytrace(positionFrom.xyz, pivot);
        mediump vec4 skyColor = vec4(0.0);
        mediump vec4 outGlow = vec4(0.0);

        highp vec4 worldNormal = invModelViewMatrix * vec4(normal, 0.0);
        mediump float upness = clamp(dot(worldNormal.xyz, vec3(0.0, 1.0, 0.0)), 0.0, 1.0);

        pivot = (invModelViewMatrix * vec4(pivot, 0.0)).xyz;
        getSkyColorAt(pivot, sunPosition, 0.0, clamp(dayLight, 0.0, 1.0), horizonFog, skyColor, outGlow);
        skyColor.rgb = pow(skyColor.rgb, vec3(VSMOD_SSR_REFLECTION_DIMMING));
        reflection.rgb = mix(reflection.rgb, skyColor.rgb, VSMOD_SSR_SKY_MIXIN * upness);
        reflection.rgb = mix(skyColor.rgb * upness, reflection.rgb, reflection.a);

        mediump float normalDotEye = dot(normal, unitPositionFrom);
        mediump float fresnel = pow(clamp(1.0 + normalDotEye, 0.0, 1.0), 4.0);
        fresnel = mix(0.09, 1.0, fresnel);

        outColor = reflection;
        outColor.a = 1.0;

        outColor.rgb *= pow(texture(gTint, texcoord).rgb, vec3(VSMOD_SSR_TINT_INFLUENCE));

        highp vec4 positionFromWorldSpace = invModelViewMatrix * vec4(positionFrom.xyz, 1.0);
        mediump float fogLevel = getFogLevelDeferred(length(positionFrom), fogMinIn, fogDensityIn, positionFromWorldSpace.y);
        outColor = applyFog(outColor, fogLevel);

        outColor.a *= (1.0 - positionFrom.w) * fresnel;
    }
}
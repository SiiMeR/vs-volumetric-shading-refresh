#version 330 core
// Deferred lighting pass fragment shader
// Performs lighting calculations using G-buffer data

// Input from vertex shader
in highp vec2 texCoord;

// Output
layout(location = 0) out highp vec4 outColor;
layout(location = 1) out highp vec4 outGlow;

// G-buffer textures
uniform sampler2D albedoTex;    // RGB: albedo, A: metallic
uniform sampler2D normalTex;    // RGB: normal, A: roughness
uniform sampler2D positionTex;  // RGB: world position, A: ambient occlusion
uniform sampler2D emissiveTex;  // RGB: emissive color, A: emissive intensity
uniform highp sampler2D depthTex;     // Scene depth

// Shadow textures
uniform sampler2DShadow shadowMapNear;
uniform sampler2DShadow shadowMapFar;

// Light properties
uniform vec3 sunPosition;
uniform vec3 sunColor;
uniform float sunBrightness;
uniform vec3 ambientLight;

// Camera properties
uniform vec3 cameraPosition;
uniform mat4 invProjectionMatrix;
uniform mat4 invModelViewMatrix;

// Shadow properties
uniform mat4 shadowMapViewMatrixNear;
uniform mat4 shadowMapViewMatrixFar;
uniform mat4 shadowMapProjectionMatrixNear;
uniform mat4 shadowMapProjectionMatrixFar;
uniform float shadowIntensity = 0.8;
uniform float softShadowsEnabled = 0.0;
uniform float shadowWidth = 1.5;

// Volumetric lighting properties
uniform float volumetricIntensity = 0.0;
uniform float volumetricFlatness = 1.0;

// Physically-based shading variables
const float PI = 3.14159265359;

// Helper functions
highp vec3 reconstructWorldPosition(highp vec2 uv, highp float depth)
{
    highp vec4 clipPos = vec4(uv * 2.0 - 1.0, depth * 2.0 - 1.0, 1.0);
    highp vec4 viewPos = invProjectionMatrix * clipPos;
    viewPos /= viewPos.w;
    highp vec4 worldPos = invModelViewMatrix * viewPos;
    return worldPos.xyz;
}

// Shadow mapping functions
float getShadowMapValue(vec3 worldPos, int cascade)
{
    vec4 shadowPos;
    
    if (cascade == 0)
    {
        shadowPos = shadowMapProjectionMatrixNear * shadowMapViewMatrixNear * vec4(worldPos, 1.0);
    }
    else
    {
        shadowPos = shadowMapProjectionMatrixFar * shadowMapViewMatrixFar * vec4(worldPos, 1.0);
    }
    
    vec3 shadowCoord = shadowPos.xyz / shadowPos.w;
    shadowCoord = shadowCoord * 0.5 + 0.5; // Convert to 0-1 range
    
    // Check if outside shadow map
    if (shadowCoord.x < 0.0 || shadowCoord.x > 1.0 ||
        shadowCoord.y < 0.0 || shadowCoord.y > 1.0 ||
        shadowCoord.z < 0.0 || shadowCoord.z > 1.0)
    {
        return 1.0;
    }
    
    // Apply shadow bias to prevent shadow acne
    float bias = 0.001;
    shadowCoord.z -= bias;
    
    // Get shadow value
    float shadow;
    if (softShadowsEnabled > 0.5)
    {
        // PCF soft shadows
        float texelSize = 1.0 / textureSize(cascade == 0 ? shadowMapNear : shadowMapFar, 0).x;
        float width = shadowWidth * texelSize;
        
        shadow = 0.0;
        int samples = 4; // Keep sample count reasonable for performance
        float weight = 1.0 / (float(samples * samples) * 4.0);
        
        for (int x = -samples; x <= samples; x++)
        {
            for (int y = -samples; y <= samples; y++)
            {
                vec3 offset = vec3(x * width, y * width, 0.0);
                if (cascade == 0)
                {
                    shadow += texture(shadowMapNear, shadowCoord + offset) * weight;
                }
                else
                {
                    shadow += texture(shadowMapFar, shadowCoord + offset) * weight;
                }
            }
        }
    }
    else
    {
        // Hard shadows - can't use ternary with sampler2DShadow type
        if (cascade == 0) {
            shadow = texture(shadowMapNear, shadowCoord);
        } else {
            shadow = texture(shadowMapFar, shadowCoord);
        }
    }
    
    return shadow;
}

// PBR lighting calculations
float distributionGGX(float NdotH, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH2 = NdotH * NdotH;
    
    float denom = NdotH2 * (a2 - 1.0) + 1.0;
    denom = PI * denom * denom;
    
    return a2 / max(denom, 0.0001);
}

float geometrySmith(float NdotV, float NdotL, float roughness)
{
    float a = roughness + 1.0;
    float k = (a * a) / 8.0;
    
    float ggx1 = NdotV / (NdotV * (1.0 - k) + k);
    float ggx2 = NdotL / (NdotL * (1.0 - k) + k);
    
    return ggx1 * ggx2;
}

vec3 fresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}

// Volumetric scattering approximate calculation
float calculateVolumetricScatter(vec3 worldPos)
{
    if (volumetricIntensity <= 0.0) return 0.0;
    
    // Calculate direction to sun
    vec3 lightDir = normalize(sunPosition);
    vec3 viewDir = normalize(cameraPosition - worldPos);
    
    // Calculate scattering angle
    float cosAngle = dot(viewDir, lightDir);
    
    // Henyey-Greenstein phase function approximation
    float g = 0.2; // Forward scattering bias
    float phase = (1.0 - g*g) / pow(1.0 + g*g - 2.0*g*cosAngle, 1.5);
    
    // Get shadow for volumetric occlusion
    float shadow = getShadowMapValue(worldPos, 0);
    
    // Calculate final scatter amount
    float scatter = phase * shadow * volumetricIntensity;
    scatter = pow(scatter, volumetricFlatness);
    
    return scatter;
}

void main()
{
    // Sample G-buffer textures
    highp vec4 albedoData = texture(albedoTex, texCoord);
    highp vec4 normalData = texture(normalTex, texCoord);
    highp vec4 positionData = texture(positionTex, texCoord);
    highp vec4 emissiveData = texture(emissiveTex, texCoord);
    highp float depth = texture(depthTex, texCoord).r;
    
    // Extract G-buffer properties
    highp vec3 albedo = albedoData.rgb;
    highp float metallic = albedoData.a;
    highp vec3 normal = normalize(normalData.rgb * 2.0 - 1.0); // Decode from 0-1 to -1 to 1
    highp float roughness = max(0.05, normalData.a); // Prevent zero roughness
    highp vec3 worldPos = positionData.rgb;
    highp float ao = positionData.a;
    highp vec3 emissive = emissiveData.rgb;
    
    // If depth is 1.0, we're rendering the skybox
    if (depth >= 0.99999)
    {
        outColor = vec4(albedo, 1.0);
        outGlow = vec4(emissive, 1.0);
        return;
    }
    
    // Reconstruct world position from depth if not available in G-buffer
    if (worldPos == vec3(0.0))
    {
        worldPos = reconstructWorldPosition(texCoord, depth);
    }
    
    // Calculate view direction
    highp vec3 V = normalize(cameraPosition - worldPos);
    
    // Directional light (sun)
    highp vec3 L = normalize(sunPosition);
    highp vec3 H = normalize(V + L);
    
    // Calculate basic light intensities
    highp float NdotL = max(dot(normal, L), 0.0);
    highp float NdotV = max(dot(normal, V), 0.0);
    highp float NdotH = max(dot(normal, H), 0.0);
    
    // Get shadow value (blend near and far cascades)
    highp float shadowNear = getShadowMapValue(worldPos, 0);
    highp float shadowFar = getShadowMapValue(worldPos, 1);
    
    // Blend between cascades based on depth
    highp float shadowBlend = min(1.0, depth * 10.0); // Arbitrary blend factor
    highp float shadow = mix(shadowNear, shadowFar, shadowBlend);
    
    // Apply shadow intensity
    shadow = mix(1.0, shadow, shadowIntensity);
    
    // Calculate PBR lighting
    highp vec3 F0 = mix(vec3(0.04), albedo, metallic); // Base reflectivity
    
    // Cook-Torrance BRDF
    highp float D = distributionGGX(NdotH, roughness);
    highp float G = geometrySmith(NdotV, NdotL, roughness);
    highp vec3 F = fresnelSchlick(max(dot(H, V), 0.0), F0);
    
    highp vec3 kD = vec3(1.0) - F;
    kD *= 1.0 - metallic;
    
    highp vec3 numerator = D * G * F;
    highp float denominator = max(4.0 * NdotV * NdotL, 0.001);
    highp vec3 specular = numerator / denominator;
    
    // Direct lighting
    highp vec3 directLight = (kD * albedo / PI + specular) * sunColor * sunBrightness * NdotL * shadow;
    
    // Ambient lighting (with ambient occlusion)
    highp vec3 ambient = ambientLight * albedo * ao;
    
    // Final color
    highp vec3 finalColor = ambient + directLight + emissive;
    
    // Calculate volumetric scattering for glow buffer
    highp float volumetricScatter = calculateVolumetricScatter(worldPos);
    
    // Output final color
    outColor = vec4(finalColor, 1.0);
    
    // Output to glow buffer
    outGlow = vec4(emissiveData.rgb, volumetricScatter);
}
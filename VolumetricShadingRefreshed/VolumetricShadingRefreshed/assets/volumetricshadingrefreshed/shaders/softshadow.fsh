#version 330 core
// Soft shadow fragment shader
// Implements PCSS (Percentage Closer Soft Shadows)

// Input from vertex shader
in highp vec2 texCoord;

// Output
layout(location = 0) out highp float outShadow;

// Textures
uniform highp sampler2D shadowMap;      // Raw shadow depth
uniform highp sampler2D depthTexture;   // Scene depth
uniform sampler2D normalTexture;        // Scene normals

// Matrices
uniform mat4 invProjectionMatrix;
uniform mat4 invModelViewMatrix;
uniform mat4 shadowMapViewMatrix;
uniform mat4 shadowMapProjectionMatrix;

// Shadow parameters
uniform float shadowWidth = 1.5;        // Base shadow width (softness)
uniform int shadowSamples = 16;         // Number of PCF samples
uniform float nearShadowBias = 0.02;    // Shadow bias for near cascade
uniform float farShadowBias = 0.05;     // Shadow bias for far cascade
uniform bool isFarCascade = false;      // Whether this is the far cascade pass

// AMD compatibility
uniform float qualityLevel = 1.0;       // Dynamic quality adjustment

// Helper functions
highp vec3 reconstructWorldPosition(highp vec2 uv, highp float depth)
{
    highp vec4 clipPos = vec4(uv * 2.0 - 1.0, depth * 2.0 - 1.0, 1.0);
    highp vec4 viewPos = invProjectionMatrix * clipPos;
    viewPos /= viewPos.w;
    highp vec4 worldPos = invModelViewMatrix * viewPos;
    return worldPos.xyz;
}

highp float random(highp vec2 uv)
{
    return fract(sin(dot(uv, vec2(12.9898, 78.233))) * 43758.5453);
}

// PCSS implementation
highp float calculateSoftShadow(highp vec3 worldPos, highp vec3 normal)
{
    // Transform position to shadow space
    highp vec4 shadowPos = shadowMapProjectionMatrix * shadowMapViewMatrix * vec4(worldPos, 1.0);
    highp vec3 shadowCoord = shadowPos.xyz / shadowPos.w;
    shadowCoord = shadowCoord * 0.5 + 0.5; // Convert to 0-1 range
    
    // Early exit if outside shadow map
    if (shadowCoord.x < 0.0 || shadowCoord.x > 1.0 ||
        shadowCoord.y < 0.0 || shadowCoord.y > 1.0 ||
        shadowCoord.z < 0.0 || shadowCoord.z > 1.0)
    {
        return 1.0;
    }
    
    // Apply shadow bias based on normal and light direction
    highp vec3 lightDir = vec3(0.0, 1.0, 0.0); // Simplified light direction
    highp float bias = isFarCascade ? farShadowBias : nearShadowBias;
    bias *= (1.0 - abs(dot(normal, lightDir))) * 0.5 + 0.5; // Adjust bias based on surface angle
    
    // Add bias to shadow coordinate
    shadowCoord.z -= bias;
    
    // Get shadow map texel size
    highp float texelSize = 1.0 / textureSize(shadowMap, 0).x;
    
    // PCSS Step 1: Blocker search (find average depth of occluders)
    highp float blockerSum = 0.0;
    highp float blockerCount = 0.0;
    
    // Dynamic sample count based on quality setting
    int blockerSamples = max(4, int(float(shadowSamples) * 0.5 * qualityLevel));
    float searchWidth = shadowWidth * 0.5 * texelSize;
    
    // Blocker search radius
    for (int i = -blockerSamples; i <= blockerSamples; i += 2)
    {
        for (int j = -blockerSamples; j <= blockerSamples; j += 2)
        {
            highp vec2 offset = vec2(i, j) * searchWidth;
            highp float shadowMapDepth = texture(shadowMap, shadowCoord.xy + offset).r;
            
            if (shadowMapDepth < shadowCoord.z)
            {
                blockerSum += shadowMapDepth;
                blockerCount += 1.0;
            }
        }
    }
    
    // Early exit if no blockers found
    if (blockerCount < 1.0)
    {
        return 1.0;
    }
    
    // Average blocker depth
    highp float avgBlockerDepth = blockerSum / blockerCount;
    
    // PCSS Step 2: Penumbra size calculation
    highp float penumbraWidth = (shadowCoord.z - avgBlockerDepth) / avgBlockerDepth;
    penumbraWidth = min(penumbraWidth * shadowWidth, shadowWidth * 3.0) * texelSize;
    
    // PCSS Step 3: PCF filtering
    highp float shadowSum = 0.0;
    
    // Dynamic sample count based on quality setting
    int pcfSamples = max(4, int(float(shadowSamples) * qualityLevel));
    
    // Add random rotation to reduce banding
    highp float randomAngle = random(texCoord) * 6.283185;
    highp vec2 randomRotation = vec2(cos(randomAngle), sin(randomAngle));
    
    // PCF filtering with variable penumbra width
    for (int i = -pcfSamples; i <= pcfSamples; i++)
    {
        for (int j = -pcfSamples; j <= pcfSamples; j++)
        {
            highp vec2 offset = vec2(i, j) * penumbraWidth;
            
            // Rotate sample pattern to reduce banding
            offset = vec2(
                offset.x * randomRotation.x - offset.y * randomRotation.y,
                offset.x * randomRotation.y + offset.y * randomRotation.x
            );
            
            highp float shadowMapDepth = texture(shadowMap, shadowCoord.xy + offset).r;
            shadowSum += (shadowMapDepth > shadowCoord.z) ? 1.0 : 0.0;
        }
    }
    
    // Calculate final shadow value
    highp float totalSamples = float((pcfSamples * 2 + 1) * (pcfSamples * 2 + 1));
    highp float shadow = shadowSum / totalSamples;
    
    return shadow;
}

void main()
{
    // Sample depth and reconstruct position
    highp float depth = texture(depthTexture, texCoord).r;
    
    // Skip skybox
    if (depth >= 0.99999)
    {
        outShadow = 1.0;
        return;
    }
    
    // Reconstruct world position
    highp vec3 worldPos = reconstructWorldPosition(texCoord, depth);
    
    // Get normal
    highp vec3 normal = normalize(texture(normalTexture, texCoord).rgb * 2.0 - 1.0);
    
    // Calculate soft shadow
    highp float shadow = calculateSoftShadow(worldPos, normal);
    
    // Output shadow value
    outShadow = shadow;
}
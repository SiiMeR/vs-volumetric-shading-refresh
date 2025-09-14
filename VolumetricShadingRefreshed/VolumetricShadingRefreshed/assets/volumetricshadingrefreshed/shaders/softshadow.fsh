#version 330 core

// Soft Shadow Fragment Shader
// High-quality PCSS implementation replacing YAML-based patches
// Provides variable penumbra soft shadows based on blocker distance

in vec2 texCoord;
in vec3 rayDirection;

out float shadowFactor;

// Input textures
uniform sampler2D shadowMapNearTex;
uniform sampler2D shadowMapFarTex;  
uniform sampler2D depthTexture;
uniform sampler2D normalTexture;

// Shadow parameters
uniform int softShadowSamples;
uniform float nearShadowWidth;
uniform float farShadowWidth;
uniform float shadowRadius;
uniform float nearShadowOffset;
uniform float farShadowOffset;
uniform float frameTime;

// Shadow matrices
uniform mat4 toShadowMapSpaceMatrixNear;
uniform mat4 toShadowMapSpaceMatrixFar;
uniform float shadowRangeNear;
uniform float shadowRangeFar;

// Projection matrices
uniform mat4 invProjectionMatrix;
uniform mat4 invModelViewMatrix;

// Constants for PCSS
const float LIGHT_SIZE = 0.5; // Virtual light size
const int MAX_BLOCKER_SAMPLES = 16;
const int MAX_PCF_SAMPLES = 32;
const float MIN_PENUMBRA = 1.0;
const float MAX_PENUMBRA = 10.0;

// Reconstruct world position from depth
vec3 reconstructWorldPosition(float depth, vec2 screenCoord, vec3 rayDir)
{
    float linearDepth = depth * shadowRangeFar;
    return rayDir * linearDepth;
}

// Generate pseudo-random rotation for dithering
float random(vec2 co)
{
    return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453);
}

// Generate Poisson disk samples for soft shadows
vec2 poissonDisk[16] = vec2[](
    vec2(-0.613392, 0.617481),
    vec2(0.170019, -0.040254),
    vec2(-0.299417, 0.791925),
    vec2(0.645680, 0.493210),
    vec2(-0.651784, 0.717887),
    vec2(0.421003, 0.027070),
    vec2(-0.817194, -0.271096),
    vec2(-0.705374, -0.668203),
    vec2(0.977050, -0.108615),
    vec2(0.063326, 0.142369),
    vec2(0.203528, 0.214331),
    vec2(-0.667531, 0.326090),
    vec2(-0.098422, -0.295755),
    vec2(-0.885922, 0.215369),
    vec2(0.566637, 0.605213),
    vec2(0.039766, -0.396100)
);

// PCSS Step 1: Blocker search
float findBlockerDistance(sampler2D shadowMap, vec3 shadowCoord, float searchRadius)
{
    float blockerDistance = 0.0;
    int numBlockers = 0;
    
    // Add temporal dithering to reduce banding
    float rotAngle = random(texCoord + frameTime) * 6.28318;
    float cosAngle = cos(rotAngle);
    float sinAngle = sin(rotAngle);
    
    for (int i = 0; i < MAX_BLOCKER_SAMPLES && i < softShadowSamples; i++)
    {
        vec2 offset = poissonDisk[i] * searchRadius;
        
        // Rotate the offset for temporal anti-aliasing
        vec2 rotatedOffset = vec2(
            offset.x * cosAngle - offset.y * sinAngle,
            offset.x * sinAngle + offset.y * cosAngle
        );
        
        vec2 sampleCoord = shadowCoord.xy + rotatedOffset;
        
        // Check if sample is within shadow map bounds
        if (sampleCoord.x >= 0.0 && sampleCoord.x <= 1.0 && 
            sampleCoord.y >= 0.0 && sampleCoord.y <= 1.0)
        {
            float shadowMapDepth = texture(shadowMap, sampleCoord).r;
            
            if (shadowMapDepth < shadowCoord.z)
            {
                blockerDistance += shadowMapDepth;
                numBlockers++;
            }
        }
    }
    
    return numBlockers > 0 ? blockerDistance / float(numBlockers) : -1.0;
}

// PCSS Step 2: Penumbra estimation
float calculatePenumbra(float blockerDistance, float receiverDistance)
{
    float penumbra = (receiverDistance - blockerDistance) / blockerDistance;
    return clamp(penumbra * LIGHT_SIZE, MIN_PENUMBRA, MAX_PENUMBRA);
}

// PCSS Step 3: PCF filtering
float pcfFilter(sampler2D shadowMap, vec3 shadowCoord, float filterRadius, float bias)
{
    float shadow = 0.0;
    int samples = min(softShadowSamples, MAX_PCF_SAMPLES);
    
    // Add temporal dithering
    float rotAngle = random(texCoord + frameTime * 0.5) * 6.28318;
    float cosAngle = cos(rotAngle);
    float sinAngle = sin(rotAngle);
    
    for (int i = 0; i < samples; i++)
    {
        vec2 offset = poissonDisk[i % 16] * filterRadius;
        
        // Rotate the offset
        vec2 rotatedOffset = vec2(
            offset.x * cosAngle - offset.y * sinAngle,
            offset.x * sinAngle + offset.y * cosAngle
        );
        
        vec2 sampleCoord = shadowCoord.xy + rotatedOffset;
        
        // Check bounds
        if (sampleCoord.x >= 0.0 && sampleCoord.x <= 1.0 && 
            sampleCoord.y >= 0.0 && sampleCoord.y <= 1.0)
        {
            float shadowMapDepth = texture(shadowMap, sampleCoord).r;
            shadow += (shadowMapDepth < shadowCoord.z - bias) ? 0.0 : 1.0;
        }
        else
        {
            shadow += 1.0; // Assume lit for out-of-bounds samples
        }
    }
    
    return shadow / float(samples);
}

// Calculate soft shadow using PCSS
float calculateSoftShadow(sampler2D shadowMap, vec4 shadowCoords, float shadowWidth, float shadowOffset)
{
    // Convert to NDC space
    vec3 shadowCoord = shadowCoords.xyz / shadowCoords.w;
    shadowCoord = shadowCoord * 0.5 + 0.5; // Convert from [-1,1] to [0,1]
    
    // Apply bias to prevent shadow acne
    float bias = shadowOffset * 0.0001;
    shadowCoord.z -= bias;
    
    // Check if we're within shadow map bounds
    if (shadowCoord.x < 0.0 || shadowCoord.x > 1.0 || 
        shadowCoord.y < 0.0 || shadowCoord.y > 1.0 || 
        shadowCoord.z >= 1.0)
    {
        return 1.0; // Outside shadow map = fully lit
    }
    
    // PCSS Step 1: Blocker search
    float searchRadius = shadowWidth * 0.01;
    float blockerDistance = findBlockerDistance(shadowMap, shadowCoord, searchRadius);
    
    if (blockerDistance < 0.0)
    {
        return 1.0; // No blockers found = fully lit
    }
    
    // PCSS Step 2: Penumbra estimation
    float penumbra = calculatePenumbra(blockerDistance, shadowCoord.z);
    float filterRadius = penumbra * shadowWidth * 0.001;
    
    // PCSS Step 3: PCF filtering
    return pcfFilter(shadowMap, shadowCoord, filterRadius, bias);
}

void main()
{
    // Sample depth and normal
    float depth = texture(depthTexture, texCoord).r;
    vec3 normal = normalize(texture(normalTexture, texCoord).rgb * 2.0 - 1.0);
    
    // Skip processing for sky pixels
    if (depth >= 0.9999)
    {
        shadowFactor = 1.0;
        return;
    }
    
    // Reconstruct world position
    vec3 worldPos = reconstructWorldPosition(depth, texCoord, rayDirection);
    
    // Transform to shadow map space
    vec4 shadowCoordsNear = toShadowMapSpaceMatrixNear * vec4(worldPos, 1.0);
    vec4 shadowCoordsFar = toShadowMapSpaceMatrixFar * vec4(worldPos, 1.0);
    
    float shadowNear = 1.0;
    float shadowFar = 1.0;
    
    // Calculate soft shadows for near shadow map
    if (shadowCoordsNear.w > 0.0)
    {
        shadowNear = calculateSoftShadow(shadowMapNearTex, shadowCoordsNear, nearShadowWidth, nearShadowOffset);
    }
    
    // Calculate soft shadows for far shadow map
    if (shadowCoordsFar.w > 0.0)
    {
        shadowFar = calculateSoftShadow(shadowMapFarTex, shadowCoordsFar, farShadowWidth, farShadowOffset);
    }
    
    // Combine near and far shadows
    shadowFactor = min(shadowNear, shadowFar);
    
    // Apply normal-based bias to reduce peter panning
    float normalBias = max(0.05, dot(normal, vec3(0.0, 1.0, 0.0)));
    shadowFactor = mix(shadowFactor, 1.0, 1.0 - normalBias);
}

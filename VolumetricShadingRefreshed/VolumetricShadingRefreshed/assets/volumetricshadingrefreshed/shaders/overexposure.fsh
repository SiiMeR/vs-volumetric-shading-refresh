#version 330 core

// Overexposure Effect Fragment Shader
// Modern replacement for complex YAML-based shader patching
// This consolidates all overexposure logic into a single, maintainable shader

in vec2 texCoord;
in vec3 rayDirection;

out vec4 outColor;
out vec4 outGlow;

// Input textures
uniform sampler2D inputTexture;    // Main color buffer
uniform sampler2D depthTexture;    // Depth buffer for world position reconstruction  
uniform sampler2D normalTexture;   // G-buffer normals
uniform sampler2D glowTexture;     // Glow buffer

// Overexposure parameters
uniform float overexposureIntensity;  // 0.0-1.0 intensity
uniform float sunBloomIntensity;      // Additional bloom around sun
uniform float dayLight;               // Current daylight level
uniform float timeOfDay;              // 0.0-1.0 time of day

// Environmental parameters
uniform float fogDensityIn;
uniform float viewDistance;
uniform vec3 sunPosition;

// Matrices for world position reconstruction
uniform mat4 invProjectionMatrix;
uniform mat4 invModelViewMatrix;

// Constants
const float OVEREXPOSURE_FALLOFF = 2.0;
const float SUN_ANGLE_THRESHOLD = 0.98;
const float DISTANCE_FADE_START = 0.7;

// Reconstruct world position from depth
vec3 reconstructWorldPosition(float depth, vec2 screenCoord, vec3 rayDir)
{
    // Convert screen-space depth to linear depth
    float linearDepth = depth * viewDistance;
    
    // Scale ray direction by depth
    return rayDir * linearDepth;
}

// Calculate overexposure factor based on various conditions
float calculateOverexposureFactor(vec3 worldPos, vec3 normal, float originalGlow)
{
    if (overexposureIntensity <= 0.0)
        return 0.0;
    
    float factor = overexposureIntensity;
    
    // Enhance overexposure based on surface normal relative to sun
    vec3 toSun = normalize(sunPosition);
    float sunAlignment = max(0.0, dot(normal, toSun));
    factor *= (0.5 + 0.5 * sunAlignment);
    
    // Time-based modulation - stronger during day
    float dayStrength = smoothstep(0.2, 0.8, dayLight);
    factor *= dayStrength;
    
    // Distance-based falloff to prevent overexposure at extreme distances
    float distance = length(worldPos);
    float distanceFade = 1.0 - smoothstep(viewDistance * DISTANCE_FADE_START, viewDistance, distance);
    factor *= distanceFade;
    
    // Fog density influence - less overexposure in dense fog
    float fogInfluence = 1.0 - clamp(fogDensityIn * distance * 0.001, 0.0, 0.8);
    factor *= fogInfluence;
    
    // Existing glow enhancement
    factor *= (1.0 + originalGlow * 0.5);
    
    return factor;
}

// Calculate sun bloom effect
float calculateSunBloom(vec3 worldPos, vec3 normal)
{
    if (sunBloomIntensity <= 0.0)
        return 0.0;
        
    vec3 toSun = normalize(sunPosition);
    vec3 viewDir = normalize(worldPos);
    
    // Check if surface is facing towards sun and viewer is looking near sun direction
    float sunAlignment = max(0.0, dot(normal, toSun));
    float viewSunAlignment = dot(-viewDir, toSun);
    
    if (viewSunAlignment > SUN_ANGLE_THRESHOLD && sunAlignment > 0.1)
    {
        float bloomStrength = (viewSunAlignment - SUN_ANGLE_THRESHOLD) / (1.0 - SUN_ANGLE_THRESHOLD);
        return sunBloomIntensity * bloomStrength * sunAlignment * dayLight;
    }
    
    return 0.0;
}

// Enhanced fog application with overexposure
vec4 applyOverexposedFog(vec4 color, vec3 worldPos, vec3 normal, float fogAmount, float overexposureFactor)
{
    // Base fog application
    vec4 foggedColor = mix(color, vec4(0.7, 0.8, 1.0, color.a), fogAmount);
    
    // Apply overexposure enhancement
    if (overexposureFactor > 0.0)
    {
        // Enhance brightness with overexposure
        float brightness = dot(foggedColor.rgb, vec3(0.299, 0.587, 0.114));
        vec3 overexposed = foggedColor.rgb * (1.0 + overexposureFactor * OVEREXPOSURE_FALLOFF);
        
        // Blend based on original brightness to avoid over-brightening dark areas
        float blendFactor = smoothstep(0.1, 0.6, brightness) * overexposureFactor;
        foggedColor.rgb = mix(foggedColor.rgb, overexposed, blendFactor);
        
        // Subtle color temperature shift towards warmer tones during high overexposure
        if (overexposureFactor > 0.5)
        {
            float warmth = (overexposureFactor - 0.5) * 0.4;
            foggedColor.r += warmth * 0.1;
            foggedColor.g += warmth * 0.05;
        }
    }
    
    return foggedColor;
}

void main()
{
    // Sample input textures
    vec4 originalColor = texture(inputTexture, texCoord);
    float depth = texture(depthTexture, texCoord).r;
    vec3 normal = normalize(texture(normalTexture, texCoord).rgb * 2.0 - 1.0);
    float originalGlow = texture(glowTexture, texCoord).r;
    
    // Skip processing for sky pixels (depth = 1.0)
    if (depth >= 0.9999)
    {
        outColor = originalColor;
        outGlow = texture(glowTexture, texCoord);
        return;
    }
    
    // Reconstruct world position
    vec3 worldPos = reconstructWorldPosition(depth, texCoord, rayDirection);
    
    // Calculate fog amount (simplified - in real implementation this would come from uniforms)
    float distance = length(worldPos);
    float fogAmount = 1.0 - exp(-fogDensityIn * distance * 0.0001);
    fogAmount = clamp(fogAmount, 0.0, 1.0);
    
    // Calculate overexposure factor
    float overexposureFactor = calculateOverexposureFactor(worldPos, normal, originalGlow);
    
    // Apply overexposed fog and lighting
    outColor = applyOverexposedFog(originalColor, worldPos, normal, fogAmount, overexposureFactor);
    
    // Calculate enhanced glow with sun bloom
    float sunBloom = calculateSunBloom(worldPos, normal);
    float enhancedGlow = originalGlow + sunBloom * 0.01;
    
    // Modify glow based on overexposure - reduce to prevent over-brightening  
    if (overexposureFactor > 0.3)
    {
        float factor = 0.95 - clamp(overexposureFactor - 0.35 * overexposureIntensity, 0.0, 1.0);
        enhancedGlow *= factor;
    }
    
    outGlow = vec4(enhancedGlow, enhancedGlow, enhancedGlow, 1.0);
}

#version 330 core
// Overexposure fragment shader
// Optimized standalone implementation replacing YAML-based patches
// AMD compatibility: All precision qualifiers explicitly specified

// Input from vertex shader
in highp vec2 texCoord;

// Output
layout(location = 0) out highp vec4 outColor;

// Textures
uniform sampler2D inputTexture; // Main scene color
uniform highp sampler2D depthTexture; // Scene depth
uniform sampler2D normalTexture; // Normal G-buffer
uniform sampler2D glowTexture;  // Glow buffer

// Overexposure parameters
uniform float overexposureIntensity = 0.3;
uniform float sunBloomIntensity = 0.5;

// Transformation matrices
uniform mat4 invProjectionMatrix;
uniform mat4 invModelViewMatrix;
uniform highp vec3 sunPosition;

// Screen dimensions
uniform vec2 viewResolution;

// Helper function to reconstruct world position from depth
highp vec3 reconstructWorldPosition(highp vec2 uv, highp float depth)
{
    highp vec4 clipPos = vec4(uv * 2.0 - 1.0, depth * 2.0 - 1.0, 1.0);
    highp vec4 viewPos = invProjectionMatrix * clipPos;
    viewPos /= viewPos.w;
    highp vec4 worldPos = invModelViewMatrix * viewPos;
    return worldPos.xyz;
}

void main()
{
    // Sample input textures
    highp vec4 sceneColor = texture(inputTexture, texCoord);
    highp float depth = texture(depthTexture, texCoord).r;
    mediump vec3 normal = normalize(texture(normalTexture, texCoord).rgb * 2.0 - 1.0);
    mediump vec4 glow = texture(glowTexture, texCoord);
    
    // Reconstruct world position
    highp vec3 worldPos = reconstructWorldPosition(texCoord, depth);
    
    // Calculate sun direction
    highp vec3 viewDir = normalize(-worldPos);
    highp vec3 sunDir = normalize(sunPosition);
    
    // Sun overexposure effect (bloom around sun)
    mediump float sunDot = max(0.0, dot(viewDir, sunDir));
    mediump float sunEffect = pow(sunDot, 8.0) * sunBloomIntensity;
    
    // Overexposure based on brightness
    mediump float luminance = dot(sceneColor.rgb, vec3(0.299, 0.587, 0.114));
    mediump float overexposed = max(0.0, luminance - 0.7) * overexposureIntensity;
    
    // Mix in volumetric lighting effects from glow buffer
    mediump float volumetricContribution = glow.g * 0.5; // Use green channel for volumetrics
    
    // Combine effects
    mediump vec3 bloomColor = mix(vec3(1.0), sceneColor.rgb, 0.7);
    mediump vec3 finalColor = sceneColor.rgb;
    
    // Apply sun bloom
    finalColor += bloomColor * sunEffect;
    
    // Apply general overexposure
    finalColor += bloomColor * overexposed;
    
    // Add volumetric contribution
    finalColor += bloomColor * volumetricContribution * overexposureIntensity;
    
    // Output final color
    outColor = vec4(finalColor, sceneColor.a);
}
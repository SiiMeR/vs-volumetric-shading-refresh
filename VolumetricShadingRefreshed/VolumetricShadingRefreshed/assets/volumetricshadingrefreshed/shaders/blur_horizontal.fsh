#version 330 core
// Horizontal Gaussian blur shader
// Separable blur implementation for better performance

// Input from vertex shader
in highp vec2 texCoord;

// Output
layout(location = 0) out highp vec4 outColor;

// Textures
uniform sampler2D inputTexture;

// Blur parameters
uniform float blurRadius = 4.0;
uniform int blurSamples = 9; // Should be odd number
uniform vec2 viewResolution;

// Performance settings
uniform float qualityLevel = 1.0;

void main()
{
    // Calculate pixel size for correct blur radius
    highp vec2 texSize = 1.0 / viewResolution;
    highp vec2 pixelSize = vec2(texSize.x, 0.0); // Horizontal only
    
    // Dynamic quality adjustment
    int actualSamples = int(max(3.0, float(blurSamples) * qualityLevel));
    if ((actualSamples & 1) == 0) actualSamples--; // Ensure odd number
    
    // Calculate Gaussian weights
    highp float sigma = blurRadius * 0.5;
    highp float weightSum = 0.0;
    highp vec4 result = vec4(0.0);
    
    // Center sample
    highp vec4 centerSample = texture(inputTexture, texCoord);
    highp float centerWeight = exp(-(0.0 * 0.0) / (2.0 * sigma * sigma));
    result += centerSample * centerWeight;
    weightSum += centerWeight;
    
    // Side samples with Gaussian weights
    int halfSamples = actualSamples / 2;
    
    for (int i = 1; i <= halfSamples; i++)
    {
        // Weight calculation using Gaussian function
        highp float weight = exp(-(float(i) * float(i)) / (2.0 * sigma * sigma));
        
        // Sample in positive direction
        highp vec2 offset = pixelSize * float(i);
        highp vec4 sample1 = texture(inputTexture, texCoord + offset);
        result += sample1 * weight;
        
        // Sample in negative direction
        highp vec4 sample2 = texture(inputTexture, texCoord - offset);
        result += sample2 * weight;
        
        // Add weights to sum for normalization
        weightSum += weight * 2.0;
    }
    
    // Normalize by weight sum to preserve brightness
    outColor = result / weightSum;
}
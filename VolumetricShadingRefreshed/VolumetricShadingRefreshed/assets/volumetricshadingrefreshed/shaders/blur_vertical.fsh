#version 330 core

// Vertical Blur Fragment Shader
// High-quality Gaussian blur vertical pass  
// Modern replacement for simple YAML-based blur patches

in vec2 texCoord;
in vec2 blurTexCoords[9];

out vec4 outColor;

uniform sampler2D inputTexture;
uniform int blurSamples;

// Gaussian weights for different sample counts
// Pre-calculated for performance (same as horizontal pass)
const float gaussianWeights5[5] = float[](
    0.2270270270, 0.3162162162, 0.0702702703, 0.3162162162, 0.2270270270
);

const float gaussianWeights7[7] = float[](
    0.0702702703, 0.3162162162, 0.2270270270, 0.3243243243, 0.2270270270, 0.3162162162, 0.0702702703
);

const float gaussianWeights9[9] = float[](
    0.0205, 0.0855, 0.2320, 0.3423, 0.3843, 0.3423, 0.2320, 0.0855, 0.0205
);

vec4 applyGaussianBlur()
{
    vec4 result = vec4(0.0);
    
    if (blurSamples == 5)
    {
        for (int i = 0; i < 5; i++)
        {
            result += texture(inputTexture, blurTexCoords[i]) * gaussianWeights5[i];
        }
    }
    else if (blurSamples == 7)
    {
        for (int i = 0; i < 7; i++)
        {
            result += texture(inputTexture, blurTexCoords[i]) * gaussianWeights7[i];
        }
    }
    else if (blurSamples == 9)
    {
        for (int i = 0; i < 9; i++)
        {
            result += texture(inputTexture, blurTexCoords[i]) * gaussianWeights9[i];
        }
    }
    else
    {
        // Fallback for other sample counts - dynamic weight calculation
        float totalWeight = 0.0;
        int halfSamples = blurSamples / 2;
        float sigma = float(halfSamples) / 3.0; // Standard deviation
        
        for (int i = 0; i < blurSamples && i < 9; i++)
        {
            float x = float(i - halfSamples);
            float weight = exp(-(x * x) / (2.0 * sigma * sigma));
            result += texture(inputTexture, blurTexCoords[i]) * weight;
            totalWeight += weight;
        }
        
        result /= totalWeight; // Normalize
    }
    
    return result;
}

void main()
{
    if (blurSamples <= 1)
    {
        // No blur - pass through
        outColor = texture(inputTexture, texCoord);
        return;
    }
    
    outColor = applyGaussianBlur();
}

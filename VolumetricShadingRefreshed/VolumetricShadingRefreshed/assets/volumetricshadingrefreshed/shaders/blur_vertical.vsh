#version 330 core

// Vertical Blur Vertex Shader  
// Optimized separable Gaussian blur - vertical pass
// Replaces YAML-based blur.vsh patches with proper separable implementation

in vec3 vertex;
in vec2 uv;

out vec2 texCoord;
out vec2 blurTexCoords[9]; // Pre-calculated texture coordinates for blur samples

uniform vec2 texelSize;    // (0, 1/height) for vertical blur
uniform float blurRadius;  // Blur radius in pixels
uniform int blurSamples;   // Number of blur samples (should be odd)

void main()
{
    gl_Position = vec4(vertex, 1.0);
    texCoord = uv;
    
    // Pre-calculate texture coordinates for blur samples
    // This optimization moves coordinate calculation from fragment to vertex shader
    float step = blurRadius / float((blurSamples - 1) / 2);
    int halfSamples = blurSamples / 2;
    
    for (int i = 0; i < 9; i++)
    {
        if (i < blurSamples)
        {
            float offset = float(i - halfSamples) * step;
            blurTexCoords[i] = uv + vec2(0.0, texelSize.y * offset);
        }
        else
        {
            blurTexCoords[i] = uv; // Unused coordinates default to center
        }
    }
}

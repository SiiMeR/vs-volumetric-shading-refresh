#version 330 core

// Overexposure Effect Vertex Shader
// Replaces YAML-based patches with dedicated post-processing approach

in vec3 vertex;
in vec2 uv;

out vec2 texCoord;
out vec3 rayDirection;

uniform mat4 invProjectionMatrix;
uniform mat4 invModelViewMatrix;

void main()
{
    // Standard screen-space quad rendering
    gl_Position = vec4(vertex, 1.0);
    texCoord = uv;
    
    // Calculate ray direction for world-space position reconstruction
    // This enables proper distance-based overexposure calculations
    vec4 clipSpace = vec4(vertex.xy, -1.0, 1.0);
    vec4 viewSpace = invProjectionMatrix * clipSpace;
    viewSpace /= viewSpace.w;
    
    vec4 worldSpace = invModelViewMatrix * viewSpace;
    rayDirection = worldSpace.xyz;
}

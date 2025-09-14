#version 330 core

// Soft Shadow Vertex Shader
// PCSS (Percentage Closer Soft Shadows) implementation
// Replaces YAML-based soft shadow patches

in vec3 vertex;
in vec2 uv;

out vec2 texCoord;
out vec3 rayDirection;

uniform mat4 invProjectionMatrix;
uniform mat4 invModelViewMatrix;

void main()
{
    gl_Position = vec4(vertex, 1.0);
    texCoord = uv;
    
    // Calculate ray direction for world-space position reconstruction
    vec4 clipSpace = vec4(vertex.xy, -1.0, 1.0);
    vec4 viewSpace = invProjectionMatrix * clipSpace;
    viewSpace /= viewSpace.w;
    
    vec4 worldSpace = invModelViewMatrix * viewSpace;
    rayDirection = worldSpace.xyz;
}

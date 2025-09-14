#version 330 core
// Deferred lighting pass vertex shader
// Simple fullscreen quad implementation

// Vertex attributes
in vec3 position;
in vec2 uv;

// Output to fragment shader
out vec2 texCoord;

void main()
{
    // Pass texture coordinates to fragment shader
    texCoord = uv;
    
    // Set position (fullscreen quad)
    gl_Position = vec4(position, 1.0);
}
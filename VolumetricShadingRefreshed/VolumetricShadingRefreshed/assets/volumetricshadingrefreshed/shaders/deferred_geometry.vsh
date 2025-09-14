#version 330 core
// Deferred geometry pass vertex shader
// Creates G-buffer for deferred lighting

// Vertex attributes
in vec3 position;
in vec2 uv;
in vec3 normal;
in vec4 color;
in vec3 tangent;
in vec2 uv2;

// Outputs to fragment shader
out vec2 texCoord;
out vec3 worldPos;
out vec3 worldNormal;
out vec4 vertexColor;
out mat3 tbnMatrix;
out vec2 texCoord2;

// Transformation matrices
uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 modelViewMatrix;
uniform mat4 projectionMatrix;
uniform mat3 normalMatrix;

void main()
{
    // Pass vertex color and texture coordinates
    vertexColor = color;
    texCoord = uv;
    texCoord2 = uv2;
    
    // Transform position to world space
    vec4 worldPosition = modelMatrix * vec4(position, 1.0);
    worldPos = worldPosition.xyz;
    
    // Transform normal to world space
    worldNormal = normalize(normalMatrix * normal);
    
    // Calculate tangent space
    vec3 T = normalize(normalMatrix * tangent);
    vec3 N = worldNormal;
    vec3 B = normalize(cross(N, T));
    tbnMatrix = mat3(T, B, N);
    
    // Output position
    gl_Position = projectionMatrix * viewMatrix * worldPosition;
}
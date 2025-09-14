#version 330 core
// Deferred geometry fragment shader
// Creates G-buffer for deferred lighting

// Input from vertex shader
in highp vec2 texCoord;
in highp vec3 worldPos;
in mediump vec3 worldNormal;
in mediump vec4 vertexColor;
in mediump mat3 tbnMatrix;
in mediump vec2 texCoord2;

// Output G-buffer
layout(location = 0) out highp vec4 outAlbedo;    // RGB: albedo, A: metallic
layout(location = 1) out highp vec4 outNormal;    // RGB: normal, A: roughness
layout(location = 2) out highp vec4 outPosition;  // RGB: world position, A: ambient occlusion
layout(location = 3) out highp vec4 outEmissive;  // RGB: emissive color, A: emissive intensity

// Textures
uniform sampler2D albedoTex;
uniform sampler2D normalTex;
uniform sampler2D materialTex; // R: roughness, G: metallic, B: ambient occlusion

// Material parameters
uniform float roughnessMultiplier = 1.0;
uniform float metallicMultiplier = 1.0;
uniform float emissiveIntensity = 0.0;

// Settings
uniform bool useNormalMap = true;
uniform bool useVertexColors = true;

// Functions
mediump vec3 decodeNormal(mediump vec3 texNormal)
{
    mediump vec3 n = texNormal * 2.0 - 1.0;
    return normalize(tbnMatrix * n);
}

void main()
{
    // Sample textures
    mediump vec4 albedoSample = texture(albedoTex, texCoord);
    mediump vec4 normalSample = texture(normalTex, texCoord);
    mediump vec4 materialSample = texture(materialTex, texCoord);
    
    // Calculate albedo with vertex colors
    mediump vec3 albedo = albedoSample.rgb;
    if (useVertexColors)
    {
        albedo *= vertexColor.rgb;
    }
    
    // Calculate normal
    mediump vec3 normal = worldNormal;
    if (useNormalMap)
    {
        normal = decodeNormal(normalSample.rgb);
    }
    
    // Extract material properties
    mediump float roughness = materialSample.r * roughnessMultiplier;
    mediump float metallic = materialSample.g * metallicMultiplier;
    mediump float ao = materialSample.b;
    
    // Emissive calculation (based on brightness or dedicated map)
    mediump vec3 emissive = vec3(0.0);
    mediump float emissiveValue = 0.0;
    
    if (emissiveIntensity > 0.0)
    {
        // Simple emissive based on albedo brightness
        mediump float brightness = max(max(albedo.r, albedo.g), albedo.b);
        emissiveValue = brightness * emissiveIntensity;
        emissive = albedo * emissiveValue;
    }
    
    // Output to G-buffer
    outAlbedo = vec4(albedo, metallic);
    outNormal = vec4(normal * 0.5 + 0.5, roughness); // Encode normal to 0-1 range
    outPosition = vec4(worldPos, ao);
    outEmissive = vec4(emissive, emissiveValue);
}
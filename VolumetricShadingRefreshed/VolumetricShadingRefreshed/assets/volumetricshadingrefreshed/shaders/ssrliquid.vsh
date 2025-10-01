#version 330 core
#extension GL_ARB_explicit_attrib_location: enable

layout(location = 0) in vec3 vertexPositionIn;
layout(location = 1) in vec2 uvIn;
// rgb = block light, a=sun light level
layout(location = 2) in vec4 rgbaLightIn;
// Check out chunkvertexflags.ash for understanding the contents of this data
layout(location = 3) in int renderFlags;

layout(location = 4) in vec2 flowVector;

// Bits 0..7 = season map index
// Bits 8..11 = climate map index
// Bits 12 = Frostable bit
// Bits 13, 14, 15 = free \o/
// Bits 16-23 = temperature
// Bits 24-31 = rainfall
layout(location = 5) in int colormapData;

// Old format:
// Bit 0: Should animate yes/no
// Bit 1: Should texture fade yes/no
// Bits 8-15: x-Distance to upper left corner, where 255 = size of the block texture
// Bits 16-24: y-Distance to upper left corner, where 255 = size of the block texture
// Bit 25: Lava yes/no
// Bit 26: Weak foamy yes/no
// Bit 27: Weak Wavy yes/no

// New format: (from 1.21.0+)
// Bit 0: Should animate yes/no
// Bit 1: Should texture fade yes/no
// Bit 2-9: Oceanity
// Bits 10-17: x-Distance to upper left corner, where 255 = size of the block texture
// Bits 18-26: y-Distance to upper left corner, where 255 = size of the block texture
// Bit 27: Lava yes/no - use LiquidIsLavaBitPosition

// Bit 28: Weak foamy yes/no - use LiquidWeakFoamBitPosition
// Bit 29: Weak Wavy yes/no - use LiquidWeakWaveBitPosition
// Bit 30: Don't tweak alpha channel - use LiquidFullAlphaBitPosition
// Bit 31: LiquidExposedToSky - use LiquidSkyExposedBitPosition
layout(location = 6) in int waterFlagsIn;


uniform vec3 origin;
uniform mat4 projectionMatrix;
uniform mat4 modelViewMatrix;
uniform vec4 rgbaFogIn;

out vec2 flowVectorf;
out vec4 worldPos;
out vec4 fragPosition;
out vec4 gnormal;
out vec3 worldNormal;
out vec3 fragWorldPos;
out vec2 uv;
flat out int waterFlags;
out float alpha;
flat out int skyExposed;


#include vertexflagbits.ash
#include vertexwarp.vsh
#include fogandlight.vsh
#include colormap.vsh

void main(void)
{
    worldPos = vec4(vertexPositionIn + origin, 1.0); // Dont declare as vec4. it breaks \(O_O)/

    //float div = ((waterFlagsIn & (1<<27)) > 0) ? 90 : 5;
    //float yBefore = worldPos.y;
    if ((waterFlagsIn & 1) == 1)
    {
        float div = ((waterFlagsIn & LiquidWeakWaveBitMask) > 0) ? 90 : 5;
	
        float oceanity = ((waterFlagsIn >> 2) & 0xff) * OneOver255;
        div *= max(0.2, 1 - oceanity);
		
        worldPos = applyLiquidWarping((waterFlagsIn & LiquidIsLavaBitMask) == 0, worldPos, div);
    }
    else if ((waterFlagsIn & LiquidWeakWaveBitMask) > 0)
    {
        worldPos = applyLiquidWarping((waterFlagsIn & LiquidIsLavaBitMask) == 0, worldPos, 90);
    }
    //worldPos = applyLiquidWarping((waterFlagsIn & 0x2000000) == 0, worldPos, div);

    vec4 cameraPos = modelViewMatrix * worldPos;

    gl_Position = projectionMatrix * cameraPos;

    vec3 fragNormal = unpackNormal(renderFlags);

    fragWorldPos = worldPos.xyz + playerpos;
    fragPosition = cameraPos;
    gnormal = modelViewMatrix * vec4(fragNormal.xyz, 0);
    worldNormal = fragNormal;
    waterFlags = waterFlagsIn;
    skyExposed = renderFlags & LiquidExposedToSkyBitMask;

    flowVectorf = flowVector;
    uv = uvIn;

    alpha = rgbaLightIn.a < 0.2f ? 0.0f : 1.0f;
    calcColorMapUvs(colormapData, vec4(vertexPositionIn + origin, 1.0) + vec4(playerpos, 1), rgbaLightIn.a, false);
}
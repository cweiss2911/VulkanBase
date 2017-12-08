#version 450 core

layout(location = 0) in vec2 vertexUV;

layout( location = 0 )out vec4 out_frag_color;

layout (set = 0, binding = 0) uniform sampler2D diffuseTexture;


void main(void)
{    
    out_frag_color = vec4( texture(diffuseTexture, vertexUV).rgba);  
}


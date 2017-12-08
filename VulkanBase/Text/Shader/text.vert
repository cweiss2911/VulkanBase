#version 450 core

layout(location = 0) in vec3 pos;
layout(location = 1) in vec2 vertexUV;

layout(location = 0) out vec2 uv;

layout (push_constant) uniform push_constantType 
{
    mat4 modelMatrix;        
} push_constants;

void main() 
{
    uv = vertexUV;
    gl_Position =  push_constants.modelMatrix * vec4(pos, 1);
}


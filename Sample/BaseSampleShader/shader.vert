#version 450 core

layout(location = 0) in vec3 pos;

layout (push_constant) uniform push_constantType 
{
    mat4 modelMatrix;    
    vec3 color;
} push_constants;

layout (set = 0, binding = 0) uniform offsetT
{
    mat4 projectionMatrix;
    mat4 viewMatrix;    
} offset;

layout(location = 0) out vec3 color;

void main() 
{    
    color = push_constants.color;
    gl_Position = offset.projectionMatrix * offset.viewMatrix * push_constants.modelMatrix * vec4(pos, 1);
}


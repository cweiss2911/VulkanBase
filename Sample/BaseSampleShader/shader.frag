#version 450 core

layout(location = 0) in vec3 color;

layout( location = 0 )out vec4 out_frag_color;

void main(void)
{    
    out_frag_color = vec4(color, 1);  
}


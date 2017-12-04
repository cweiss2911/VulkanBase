#version 450 core

layout( location = 0 )out vec4 out_frag_color;


layout (set = 1, binding = 0) uniform sampler2D diffuseTexture;
layout (set = 1, binding = 1) uniform sampler2D normalTexture;


layout(location = 0) in vec3 normal;
layout(location = 1) in vec3 light;
layout(location = 2) in vec2 uvText;
layout(location = 3) in mat3 tbn;
layout(location = 6) flat in int normalMapping;
layout(location = 7) in vec3 testpass;

vec3 MultiplyOneByOne(vec3 vector1, vec3 vector2)
{
    return vec3(vector1.x * vector2.x, vector1.y * vector2.y, vector1.z * vector2.z);
}


vec3 DetermineNormal()
{
    vec3 normalToReturn = normal;
    if(normalMapping == 1)
    {
        normalToReturn = tbn * normalize((2 * texture( normalTexture, uvText.xy ).rgb) - vec3(1));
    }
    return normalToReturn;
}


void main(void)
{    
    vec3 ambientLightColor= vec3(0.3);

    vec3 normalForCalculation = DetermineNormal();;
    vec3 diffuseLightColor = vec3(0.25) * clamp(dot (normalize(-light), normalForCalculation), 0, 1);        
    
    vec3 diffuseColorWithAmbient = ambientLightColor + diffuseLightColor;

    vec3 lightedDiffuseTextureColor = MultiplyOneByOne(diffuseColorWithAmbient, texture(diffuseTexture, vec2(uvText.x, uvText.y)).rgb );         
    
    vec3 finalLightedTextureColor =  lightedDiffuseTextureColor ;
        
    out_frag_color = vec4(finalLightedTextureColor, 1);  
    //out_frag_color = vec4(texture( diffuseTexture, uvText ).rgb, 1);
   // out_frag_color = vec4(uvText.x, uvText.y, 0, 1);  
    //out_frag_color = vec4(testpass, 1);
  //  out_frag_color = vec4(normalForCalculation, 1);
    //out_frag_color = vec4(push_constants.useNormalMapping);
    //out_frag_color = vec4(texture( normalTexture, UV ).rgb, 1);
    //out_frag_color = vec4(uvText.x, uvText.y,0,1 );
 //   out_frag_color = vec4(uvText.y, uvText.y,0,1 );
}


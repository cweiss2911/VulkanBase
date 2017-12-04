#version 450 core

layout(location = 0) in vec3 pos;
layout(location = 1) in vec3 vertex_normal;
layout(location = 2) in vec2 vertexUV;
layout(location = 3) in vec3 vertex_tangent;          
layout(location = 4) in vec3 vertex_bitangent;
layout(location = 5) in vec4 joint_indices;
layout(location = 6) in vec4 weights;
layout(location = 7) in vec4 joint_indices2;
layout(location = 8) in vec4 weights2;
layout(location = 9) in mat4 instance_modelmatrix;

layout (push_constant) uniform push_constantType 
{
    mat4 modelMatrix;    
    int useNormalMapping;
    int time;
} push_constants;


layout (constant_id = 0) const float SSAO_KERNEL_SIZE= 64;

layout (set = 0, binding = 0) uniform offsetT
{
    mat4 projectionMatrix;
    mat4 viewMatrix;    
} offset;


layout (set = 2, binding = 0) uniform samplerBuffer quaternionSampler;
layout (set = 2, binding = 1) uniform samplerBuffer originSampler;
layout (set = 2, binding = 2) uniform samplerBuffer bindposeOriginSampler;

layout(location = 0) out vec3 normal;
layout(location = 1) out vec3 light;
layout(location = 2) out vec2 uvText;
layout(location = 3) out mat3 tbn;
layout(location = 6) out int normalMapping;
layout(location = 7) out vec3 testpass;


vec3 skinned_vertex_position;
vec3 skinned_vertex_normal;
vec3 skinned_vertex_tangent;
vec3 skinned_vertex_bitangent;

mat4 modelMatrixtoUse;

int skinningFieldsInitialized = 0;

int frames = 139;


vec4 InvertQuat(vec4 quat)
{
    float i = 1.0f / dot(quat, quat);    
    return vec4(quat.xyz * -i, quat.w * i);
}

vec4 MultiplyQuat(vec4 left, vec4 right)
{
    return vec4(right.w * left.xyz + left.w * right.xyz + cross(left.xyz, right.xyz), left.w * right.w - dot(left.xyz, right.xyz));
}

vec4 Transform(vec4 v, vec4 quat)
{
    vec4 inverseQuat = InvertQuat(quat);    
    vec4 t = MultiplyQuat(quat, v);
    return MultiplyQuat(t, inverseQuat);    
}

void InitializeSkinnedFieldsIfNecessary()
{
    if(skinningFieldsInitialized == 0)
    {
        skinned_vertex_position = vec3(0);
        skinned_vertex_normal = vec3(0);
        skinned_vertex_tangent = vec3(0);
        skinned_vertex_bitangent = vec3(0);
        
        skinningFieldsInitialized = 1;
    }
}

void Skin(vec4 weight_, vec4 joint_indices_)
{
    if(weight_[0] > 0.0f)
    {
        InitializeSkinnedFieldsIfNecessary();
        
        for(int i = 0; i < 4; i++)
        {         
            float weight = weight_[i];
           
            int joint_index = int(joint_indices_[i])*4;
            
            int textureIndex = frames *  joint_index + push_constants.time * 4;
            vec4 quaternion = vec4(texelFetch(quaternionSampler, textureIndex).r, texelFetch(quaternionSampler, textureIndex+1).r,texelFetch(quaternionSampler, textureIndex+2).r,texelFetch(quaternionSampler, textureIndex+3).r);
            vec3 direction = pos - vec3(texelFetch(bindposeOriginSampler, joint_index).r, texelFetch(bindposeOriginSampler, joint_index+1).r, texelFetch(bindposeOriginSampler, joint_index+2).r);
            vec4 origin = vec4(texelFetch(originSampler, textureIndex).r, texelFetch(originSampler, textureIndex+1).r,texelFetch(originSampler, textureIndex+2).r,texelFetch(originSampler, textureIndex+3).r);
            

            skinned_vertex_position += ((origin + Transform(vec4(direction, 0), quaternion)) * weight).xyz;            
            skinned_vertex_normal += (Transform(vec4(vertex_normal, 0), quaternion) * weight).xyz;
            skinned_vertex_tangent += (Transform(vec4(vertex_tangent, 0), quaternion) * weight).xyz;
            skinned_vertex_bitangent += (Transform(vec4(vertex_bitangent, 0), quaternion) * weight).xyz;
        }
    }  
}


mat3 CalculateTbnAndNormal()
{    
    vec3 internalNormal =  normalize(modelMatrixtoUse *  vec4( skinned_vertex_normal, 0.0 ) ).xyz;
    normal = internalNormal;

    mat3 tbnMatrix = mat3(1);
    if(push_constants.useNormalMapping == 1)
    {
        vec3 tangent = normalize(modelMatrixtoUse *  vec4( skinned_vertex_tangent, 0.0 ) ).xyz;
        vec3 bitangent = normalize(modelMatrixtoUse * vec4( skinned_vertex_bitangent, 0.0 ) ).xyz;
        tbnMatrix = mat3(tangent, bitangent, internalNormal);
    }
    return tbnMatrix; 
}



void main() 
{
    modelMatrixtoUse = instance_modelmatrix;
    //modelMatrixtoUse = push_constants.modelMatrix;
    skinned_vertex_position = pos;
    skinned_vertex_normal = vertex_normal;
    skinned_vertex_tangent = vertex_tangent;
    skinned_vertex_bitangent = vertex_bitangent;
    
    Skin(weights,joint_indices);
    Skin(weights2, joint_indices2);

    tbn = CalculateTbnAndNormal();

    
    testpass = vec3(instance_modelmatrix[0][0], instance_modelmatrix[1][1], instance_modelmatrix[2][2]);
    light = (offset.viewMatrix * vec4((modelMatrixtoUse * vec4(pos, 1)).xyz - vec3(-1, 4, 5), 0)).xyz;    
    
    uvText = vertexUV;
    normalMapping = push_constants.useNormalMapping;
    gl_Position = offset.projectionMatrix * offset.viewMatrix * modelMatrixtoUse * vec4(skinned_vertex_position, 1);
}


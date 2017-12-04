using System;
using System.Collections.Generic;
using Vulkan;
using VulkanBase.ShaderParsing;

namespace VulkanBase
{
    public unsafe static class Constants
    {
        public const float INSTANCES_PER_SECOND = 100f;


        public const double rho = 0.7d;
        public static readonly float Lambda = (float)Math.Log(rho, 0.5d);

        public static class Grid
        {
            public const int Width = 32;
            public const int Height = 18;

            private const float basicCellScale = 8f / Width;
            public static readonly Vector3 CellScale = new Vector3(basicCellScale, 1f, basicCellScale);
        }


        public static readonly Dictionary<string, Type> GlslToNetType = new Dictionary<string, Type>()
        {
            { "int", typeof(int) },
            { "uint", typeof(uint) },
            { "float", typeof(float) },
            { "vec2", typeof(Vector2) },
            { "vec3", typeof(Vector3) },
            { "vec4", typeof(Vector4) },
            { "mat3", typeof(Matrix3) },
            { "mat4", typeof(Matrix4) },
        };


        public static readonly Dictionary<Type, uint> TypeToSize = new Dictionary<Type, uint>()
        {
            { typeof(int), sizeof(int) },
            { typeof(uint), sizeof(uint) },
            { typeof(float), sizeof(float) },
            { typeof(Vector2), (uint)Vector2.SizeInBytes },
            { typeof(Vector3), (uint)Vector3.SizeInBytes },
            { typeof(Vector4), (uint)Vector4.SizeInBytes },
            { typeof(Matrix3), (uint)sizeof(Matrix3) },
            { typeof(Matrix4), (uint)sizeof(Matrix4) },
        };

        public static readonly Dictionary<Type, Format> TypeToVertexInputFormat = new Dictionary<Type, Format>()
        {
            { typeof(int), Format.R32Sint },
            { typeof(uint), Format.R32Uint },
            { typeof(float), Format.R32Sfloat },
            { typeof(Vector2), Format.R32G32Sfloat },
            { typeof(Vector3), Format.R32G32B32Sfloat },
            { typeof(Vector4), Format.R32G32B32A32Sfloat },
            { typeof(Matrix3), Format.R32G32B32Sfloat },
            { typeof(Matrix4), Format.R32G32B32A32Sfloat },
        };

        public static readonly List<Type> TypesWithMultipleLocations = new List<Type>()
        {
            typeof(Matrix3), typeof(Matrix4)
        };

        public static readonly Dictionary<Type, int> TypeToOccupiedVertexInputLocations = new Dictionary<Type, int>()
        {
            { typeof(Matrix3), 3},
            { typeof(Matrix4), 4},
        };

        public static readonly Dictionary<Type, uint> TypeToOffsetStrideSize = new Dictionary<Type, uint>()
        {
            { typeof(Matrix3), (uint)Vector3.SizeInBytes },
            { typeof(Matrix4), (uint)Vector4.SizeInBytes },
        };


        public static readonly Dictionary<string, DescriptorType> GlslToDescriptorType = new Dictionary<string, DescriptorType>()
        {
            { "samplerBuffer", DescriptorType.UniformTexelBuffer},
            { "sampler2D", DescriptorType.CombinedImageSampler},
            { "image2D", DescriptorType.StorageImage},
        };

        
        public static readonly TypeConverter<string, DescriptorType> GlslToCustomDescriptorTypeConverter = new TypeConverter<string, DescriptorType>(
            new Dictionary<string, DescriptorType>()
            {
                { "uniform", DescriptorType.UniformBuffer},
                { "buffer", DescriptorType.StorageBuffer},
            }
        );

        public static readonly TypeConverter<string, ShaderStageFlags> FileExtensionToShaderStageConverter = new TypeConverter<string, ShaderStageFlags>(
            new Dictionary<string, ShaderStageFlags>()
            {
                { ".comp", ShaderStageFlags.Compute },
                { ".frag", ShaderStageFlags.Fragment},
                { ".vert", ShaderStageFlags.Vertex},
                { ".tc", ShaderStageFlags.TessellationControl},
                { ".te", ShaderStageFlags.TessellationEvaluation},
                { ".geo", ShaderStageFlags.Geometry},
            }
        );
    }
}
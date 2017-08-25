using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace VulkanBase.ShaderParsing
{
    public class ShaderUniformSet
    {
        public int Set { get; private set; }
        public List<ShaderUniform> ShaderUniforms { get; private set; }

        public ShaderUniformSet(int set)
        {
            Set = set;
            ShaderUniforms = new List<ShaderUniform>();
        }

        public uint GetSize()
        {
            if (ShaderUniforms.Any(su => su.DescriptorType == DescriptorType.CombinedImageSampler || su.DescriptorType == DescriptorType.StorageImage))
            {
                throw new Exception("Set does not have a fixed size");    
            }
            return (uint)ShaderUniforms.Sum(s => s.Size);
        }

    }
}

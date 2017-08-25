using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace VulkanBase.ShaderParsing
{
    public class ShaderUniform
    {
        public int Binding { get; internal set; }
        public DescriptorType DescriptorType { get; internal set; }
        public ShaderStageFlags StageFlags { get; internal set; }
        public string Name { get; internal set; }
        public uint Size { get; internal set; }

        public override string ToString()
        {
            return $"{Binding} {DescriptorType} {StageFlags} {Name}";
        }
    }
}

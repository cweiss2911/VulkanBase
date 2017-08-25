using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace VulkanBase.ShaderParsing
{
    public class ShaderVariable
    {
        public string Name { get; set; }
        public Type VariableType { get; set; }
        public uint Size { get; set; }
        public uint Offset { get; set; }
        public ShaderStageFlags ShaderStage { get; internal set; }

        public ShaderVariable(string name, Type type)
        {
            Name = name;
            VariableType = type;
        }

        public override string ToString()
        {
            return $"{Name} - {VariableType.Name}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace VulkanBase.ShaderParsing
{
    public class VertexInput
    {
        public string Name { get; internal set; }
        public string GlslType { get; internal set; }
        public uint Binding { get; internal set; }
        public VertexInputRate InputRate { get; internal set; }
        public uint Stride { get; internal set; }
        public Format Format { get; internal set; }
        public uint Location { get; internal set; }
        public uint Offset { get; internal set; }

        public override string ToString()
        {
            return $"layout(location = {Location}) in {GlslType} {Name}";
        }
    }
}

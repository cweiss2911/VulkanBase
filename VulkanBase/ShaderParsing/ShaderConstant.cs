using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VulkanBase.ShaderParsing
{
    public class ShaderConstant
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public Type VariableType { get; set; }
        public uint Size { get; set; }
        public uint Offset { get; set; }

        public ShaderConstant(uint id, string name, Type type, uint size, uint offset)
        {
            Id = id;
            Name = name;
            VariableType = type;
            Size = size;
            Offset = offset;
        }

        public override string ToString()
        {
            return $"{Id} {Name} - {VariableType.Name}";
        }
    }
}


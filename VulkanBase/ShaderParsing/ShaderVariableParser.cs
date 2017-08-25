using System;
using System.Collections.Generic;
using Vulkan;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace VulkanBase.ShaderParsing
{
    internal class ShaderVariableParser
    {
        internal static List<ShaderVariable> Parse(IEnumerable<string> fields)
        {
            List<ShaderVariable> variables = new List<ShaderVariable>();

            uint offset = 0;
            for (int i = 0; i < fields.Count(); i++)
            {
                string variableLine = Util.KillDuplicateWhiteSpaces(fields.ElementAt(i)).Replace("[ ]", "[]");
                if (string.IsNullOrEmpty(variableLine))
                {
                    continue;
                }

                string[] segments = variableLine.Split(' ');

                string name = segments[1];


                string glslType = segments[0];

                uint size = 0;                
                Type type;
                if (Constants.GlslToNetType.ContainsKey(glslType))
                {
                    type = Constants.GlslToNetType[glslType];
                    size = Constants.TypeToSize[type];                    
                }
                else
                {
                    type = System.Reflection.Assembly.GetEntryAssembly().DefinedTypes.Where(n => n.Name == glslType).First();
                    size = (uint)Marshal.SizeOf(type);                    
                }
                

                ShaderVariable shaderVariable = new ShaderVariable(name, type)
                {
                    Size = size,
                    Offset = offset,
                };

                variables.Add(shaderVariable);

                offset += size;
            }

            return variables;
        }
    }
}
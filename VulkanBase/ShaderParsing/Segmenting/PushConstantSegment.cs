using System.Collections.Generic;
using System.Linq;

namespace VulkanBase.ShaderParsing.Segmenting
{
    internal class PushConstantSegment : Segment
    {
        public IEnumerable<string> Fields { get; }

        public PushConstantSegment(string code) : base(code, "push_constant")
        {
            int openingIndex = code.IndexOf("{");
            int closingIndex = code.IndexOf("}");

            string pushconstantSegment = code.Substring(openingIndex + 1, closingIndex - openingIndex - 1);
            Fields = pushconstantSegment.Split(';').Select(s => s.Trim());
        }

    }
}
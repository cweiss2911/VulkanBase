using System;

namespace VulkanBase.ShaderParsing.Segmenting
{
    internal class SpecializationConstantSegment : Segment
    {
        public uint ConstantId { get; }
        public string GlslType { get; }
        public string Name { get; }

        public SpecializationConstantSegment(string code, string layout) : base(code, layout)
        {
            ConstantId = uint.Parse(layout.Replace("constant_id", string.Empty).Replace("=", string.Empty).Trim());

            string segmentAfterConst = code.Substring(code.IndexOf("const ", StringComparison.InvariantCultureIgnoreCase) + "const ".Length);
            string[] typeAndName = Util.KillDuplicateWhiteSpaces(segmentAfterConst).Split(' ');

            GlslType = typeAndName[0];
            Name = typeAndName[1];            
        }

 
    }
}
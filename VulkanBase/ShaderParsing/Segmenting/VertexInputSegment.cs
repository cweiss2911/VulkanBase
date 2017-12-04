using System;

namespace VulkanBase.ShaderParsing.Segmenting
{
    internal class VertexInputSegment : Segment
    {
        public uint Binding { get; }
        public string GlslType { get; }
        public string Name { get; }

        public VertexInputSegment(string code, string layout) : base(code, layout)
        {
            Binding = uint.Parse(layout.Replace("location", string.Empty).Replace("=", string.Empty).Trim());

            string segmentAfterIn = code.Substring(code.IndexOf("in ", StringComparison.InvariantCultureIgnoreCase) + "in ".Length);

            string[] typeAndName = Util.KillDuplicateWhiteSpaces(segmentAfterIn).Split(' ');

            GlslType = typeAndName[0];
            Name = typeAndName[1];
        }

    }
}
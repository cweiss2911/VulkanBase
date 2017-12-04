using System;

namespace VulkanBase.ShaderParsing.Segmenting
{
    internal class UniformSegmentFactory
    {
        public UniformSegmentFactory()
        {
        }

        internal static Segment CreateSegmentFromCode(string code, string layoutInfo, string bufferOrUniform = "")
        {            
            if (layoutInfo.Equals("push_constant", StringComparison.CurrentCultureIgnoreCase))
            {
                return new PushConstantSegment(code);
            }
            else
            {
                return new DescriptorSegment(code, layoutInfo, bufferOrUniform);
            }            
        }
    }
}
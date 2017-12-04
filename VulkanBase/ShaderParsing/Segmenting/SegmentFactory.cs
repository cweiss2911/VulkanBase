using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VulkanBase.ShaderParsing.Segmenting
{
    class SegmentFactory
    {
        UniformSegmentFactory _uniformSegmentFactory = new UniformSegmentFactory();
        internal Segment CreateSegmentFromCode(string code)
        {
            string layoutInfo = SegmentParser.GetBracketContent(code);

            int closingLayoutBracketIndex = code.IndexOf(')');
            string codeAfterLayoutBracktes = code.Substring(closingLayoutBracketIndex + 1);

            string[] words = codeAfterLayoutBracktes.Split(' ').Select(w => w.ToLower()).ToArray();

            if (words.Contains("in"))
            {
                return new VertexInputSegment(code, layoutInfo);
            }
            else if (words.Contains("out"))
            {
                return new OutSegment(code, layoutInfo);
            }
            else if (words.Contains("buffer"))
            {
                return UniformSegmentFactory.CreateSegmentFromCode(code, layoutInfo, "buffer");
            }
            else if (words.Contains("uniform"))
            {
                return UniformSegmentFactory.CreateSegmentFromCode(code, layoutInfo, "uniform");
            }
            else
            {
                if (layoutInfo.StartsWith("constant_id"))
                {
                    return new SpecializationConstantSegment(code, layoutInfo);
                }
                else
                {
                    throw new Exception($"Unknown segment {code}");
                }
            }


        }
    }
}

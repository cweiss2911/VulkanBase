using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VulkanBase.ShaderParsing.Segmenting
{
    class SegmentCollection
    {
        private string _code;
        List<Segment> _segments = new List<Segment>();
        SegmentFactory _segmentFactory = new SegmentFactory();

        public SegmentCollection(string code)
        {
            _code = code;
            int currentIndex = 0;
            while (currentIndex < code.Length)
            {
                char currentChar = code[currentIndex];
                if (currentChar == '#')
                {
                    int endOfLine = SegmentParser.GetEndOfLine(_code, currentIndex);

                    int segmentLength = endOfLine - currentIndex;

                    currentIndex += segmentLength;
                }
                else if (_code.Substring(currentIndex).StartsWith("layout", StringComparison.InvariantCultureIgnoreCase))
                {
                    string layoutSegmentCode = SegmentParser.GetWholeCodeSegment(_code, currentIndex);

                    string lastWord = layoutSegmentCode.Split(' ').Last();
                    //this would be the layout of the compute shader
                    if (lastWord != "in")
                    {
                        Segment segment = _segmentFactory.CreateSegmentFromCode(layoutSegmentCode);
                        _segments.Add(segment);
                    }

                    currentIndex += layoutSegmentCode.Length;
                }
                else
                {
                    currentIndex++;
                }
            }
        }

        internal IEnumerable<string> GetPushConstantFields()
        {
            return _segments.Where(s => s is PushConstantSegment).Select(s => ((PushConstantSegment)s).Fields).FirstOrDefault();
        }

        internal IEnumerable<DescriptorSegment> GetDescriptors()
        {
            return _segments.Where(s => s is DescriptorSegment).Select(s => (DescriptorSegment)s);
        }


        internal IEnumerable<T> GetSegments<T>() where T : Segment
        {
            return _segments.Where(s => s is T).Select(s => (T)s);
        }


    }
}

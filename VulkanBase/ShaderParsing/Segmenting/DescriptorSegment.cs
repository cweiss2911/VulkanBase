using System;
using System.Collections.Generic;
using System.Linq;
using Vulkan;

namespace VulkanBase.ShaderParsing.Segmenting
{
    internal class DescriptorSegment : Segment
    {
        public string Name { get; }
        public int Set { get; }
        public int Binding { get; }
        public IEnumerable<string> Fields { get; }
        public DescriptorType DescriptorType { get; }

        public DescriptorSegment(string code, string layout, string bufferOrUniform) : base(code, layout)
        {

            string[] layoutFields = layout.Split(',');
            Set = int.Parse(layoutFields.Where(lf => lf.Contains("set")).First().Replace("set", string.Empty).Replace("=", string.Empty).Trim());
            Binding = int.Parse(layoutFields.Where(lf => lf.Contains("binding")).First().Replace("binding", string.Empty).Replace("=", string.Empty).Trim());

            string segmentAfterUniform = code.Substring(code.IndexOf($"{bufferOrUniform} ", StringComparison.InvariantCultureIgnoreCase) + $"{bufferOrUniform} ".Length);
            segmentAfterUniform = Util.KillDuplicateWhiteSpaces(segmentAfterUniform).Trim();

            string[] segments = segmentAfterUniform.Split(' ');
            Name = segments.Last();

            if (segmentAfterUniform.Contains('{'))
            {
                DescriptorType = Constants.GlslToCustomDescriptorTypeConverter.Convert(bufferOrUniform.Trim());

                int openingIndex = code.IndexOf("{");
                int closingIndex = code.IndexOf("}");

                string pushconstantSegment = code.Substring(openingIndex + 1, closingIndex - openingIndex - 1);
                Fields = pushconstantSegment.Split(';').Select(s => s.Trim());
            }
            else
            {
                string uniformGlsl = segments[segments.Length - 2];
                DescriptorType =  Constants.GlslToDescriptorType[uniformGlsl];
            }

        }


    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VulkanBase.ShaderParsing.Segmenting
{
    static class SegmentParser
    {
        internal static string GetWholeCodeSegment(string code, int index)
        {
            int curlyBalance = 0;

            for (int i = index; i < code.Length; i++)
            {
                if (code[i] == '{')
                {
                    curlyBalance++;
                }
                else if (code[i] == '}')
                {
                    curlyBalance--;
                }
                else if (code[i] == ';' || i == code.Length - 1)
                {
                    if (curlyBalance == 0)
                    {
                        return code.Substring(index, i - index);
                    }
                }
            }

            throw new Exception("GLSL malformed");
        }

        internal static string GetBracketContent(string code)
        {
            int openBracketIndex = code.IndexOf('(');
            int closedBracketIndex = code.IndexOf(')');

            int bracketContentLength = closedBracketIndex - openBracketIndex - 1;

            string bracketContent = code.Substring(openBracketIndex + 1, bracketContentLength).Trim();

            return bracketContent;
        }

        internal static int GetEndOfLine(string code, int startIndex)
        {
            int index = code.IndexOf(Environment.NewLine, startIndex);

            return index;
        }

    }
}

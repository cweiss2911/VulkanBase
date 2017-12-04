using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VulkanBase.ShaderParsing
{
    enum FindMode
    {
        None,
        SingleLine,
        MultiLine
    }

    static class CommentRemover
    {
        internal static string GetCodeWithoutComments(string codeWithComments)
        {
            List<Tuple<int, int>> commentPlaces = new List<Tuple<int, int>>();

            FindMode currentMode = FindMode.None;

            int beginIndex = -1;
            for (int i = 0; i < codeWithComments.Length - 1; i++)
            {
                if (currentMode == FindMode.None)
                {
                    if (codeWithComments[i] == '/' && codeWithComments[i + 1] == '*')
                    {
                        beginIndex = i;
                        currentMode = FindMode.MultiLine;
                    }
                    else if (codeWithComments[i] == '/' && codeWithComments[i + 1] == '/')
                    {
                        beginIndex = i;
                        currentMode = FindMode.SingleLine;
                    }
                }
                else if (currentMode == FindMode.MultiLine)
                {
                    if (codeWithComments[i] == '*' && codeWithComments[i + 1] == '/' && i - beginIndex > 1)
                    {
                        commentPlaces.Add(new Tuple<int, int>(beginIndex, i));
                        currentMode = FindMode.None;
                    }
                }
                else if (currentMode == FindMode.SingleLine)
                {
                    if (codeWithComments[i] == '\r' && codeWithComments[i + 1] == '\n')
                    {
                        commentPlaces.Add(new Tuple<int, int>(beginIndex, i));
                        currentMode = FindMode.None;
                    }
                }
            }

            if (currentMode == FindMode.SingleLine)
            {
                commentPlaces.Add(new Tuple<int, int>(beginIndex, codeWithComments.Length - 2));
                currentMode = FindMode.None;
            }

            commentPlaces.Reverse();


            string codeToBeWorkedOn = codeWithComments;
            for (int i = 0; i < commentPlaces.Count; i++)
            {
                Tuple<int, int> commentPlace = commentPlaces[i];
                codeToBeWorkedOn = codeToBeWorkedOn.Substring(0, commentPlace.Item1) + codeToBeWorkedOn.Substring(commentPlace.Item2 + 2);
            }

            return codeToBeWorkedOn;
        }
    }
}

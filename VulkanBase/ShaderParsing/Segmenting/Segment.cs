using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VulkanBase.ShaderParsing.Segmenting
{
    class Segment
    {
        protected string _code;
        protected string _layout;

        public Segment(string code, string layout)
        {
            _code = code;
            _layout = layout;
        }

        public override string ToString()
        {
            return _code;
        }
    }
}

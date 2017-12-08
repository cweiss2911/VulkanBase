using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VulkanBase.ShaderParsing
{
    public class EmbeddedShaderObject : ShaderObject
    {
        private Assembly assembly;

        public EmbeddedShaderObject(string path) : base(path)
        {
        }

        public EmbeddedShaderObject(string path, Dictionary<string, int> specializationConstantValues) : base(path, specializationConstantValues)
        {            
        }


        protected override string GetContentFromPath(string path)
        {
            string result = string.Empty;
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(path))
            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }

        protected override byte[] GetBytesFromPath(string path)
        {
            byte[] buffer = null;
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(path))
            {
                buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
            }

            return buffer;
        }

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vulkan;
using VulkanBase.ShaderParsing.Segmenting;

namespace VulkanBase.ShaderParsing
{
    public class EmbeddedShaderObject : ShaderObject
    {
        private Assembly _assembly;

        public EmbeddedShaderObject(string path) : base(path)
        {
            _assembly = Assembly.GetExecutingAssembly();
            InitEmbedded();
        }

        public EmbeddedShaderObject(string path, Dictionary<string, int> specializationConstantValues) : base(path, specializationConstantValues)
        {
            _assembly = Assembly.GetExecutingAssembly();
            InitEmbedded();
        }

        public EmbeddedShaderObject(Assembly assembly, string path) : base(path)
        {
            _assembly = assembly;
            InitEmbedded();
        }

        public EmbeddedShaderObject(Assembly assembly, string path, Dictionary<string, int> specializationConstantValues) : base(path, specializationConstantValues)
        {
            _assembly = assembly;
            InitEmbedded();
        }

        private void InitEmbedded()
        {
            string codeWithComments = GetContentFromPath(FilePath);
            code = CommentRemover.GetCodeWithoutComments(codeWithComments);
            _segmentCollection = new SegmentCollection(code);
            string extension = Path.GetExtension(FilePath);
            ShaderStageFlags shaderStage = Constants.FileExtensionToShaderStageConverter.Convert(extension);
            ShaderStage = shaderStage;
        }

        protected override void Init()
        {
            
        }

        protected override string GetContentFromPath(string path)
        {
            string result = string.Empty;
            using (Stream stream = _assembly.GetManifestResourceStream(path))
            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }

        protected override byte[] GetBytesFromPath(string path)
        {
            byte[] buffer = null;            
            using (var stream = _assembly.GetManifestResourceStream(path))
            {
                buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
            }

            return buffer;
        }

    }
}

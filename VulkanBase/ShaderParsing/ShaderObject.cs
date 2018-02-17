using Vulkan;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using VulkanBase.ShaderParsing.Segmenting;

namespace VulkanBase.ShaderParsing
{
    public unsafe class ShaderObject
    {
        public string FilePath { get; protected set; }

        protected Dictionary<string, int> _specializationConstantValues;

        public ShaderStageFlags ShaderStage { get; protected set; }

        protected ShaderModule module;
        protected string code;
        protected SegmentCollection _segmentCollection;
        protected Dictionary<string, int> specializationConstantValues;

        public virtual ShaderModule Module
        {
            get
            {
                if (module == null)
                {
                    module = VContext.Instance.device.CreateShaderModule
                    (
                       new ShaderModuleCreateInfo()
                       {
                           CodeBytes = GetBytesFromPath($"{FilePath}.spv")
                       }
                    );
                }
                return module;
            }
        }
        
        public ShaderObject(string path) : this(path, null)
        {
        }

        public ShaderObject(string path, Dictionary<string, int> specializationConstantValues)
        {
            FilePath = path;
            _specializationConstantValues = specializationConstantValues;

            Init();
            
        }

        protected virtual void Init()
        {
            string codeWithComments = GetContentFromPath(FilePath);
            code = CommentRemover.GetCodeWithoutComments(codeWithComments);
            _segmentCollection = new SegmentCollection(code);
            string extension = Path.GetExtension(FilePath);
            ShaderStageFlags shaderStage = Constants.FileExtensionToShaderStageConverter.Convert(extension);
            ShaderStage = shaderStage;
        }

        protected virtual string GetContentFromPath(string path)
        {
            return File.ReadAllText(path);
        }

        protected virtual byte[] GetBytesFromPath(string path)
        {
            return File.ReadAllBytes(path);
        }

        public ShaderVariable[] GetPushConstants()
        {
            ShaderVariable[] pushConstants = new ShaderVariable[0];

            IEnumerable<string> fields = _segmentCollection.GetPushConstantFields();

            if (fields != null)
            {
                pushConstants = ShaderVariableParser.Parse(fields).ToArray();

                for (int i = 0; i < pushConstants.Length; i++)
                {
                    pushConstants[i].ShaderStage = ShaderStage;
                }
            }

            return pushConstants;
        }



        public SpecializationInfo GetSpecializationInfo()
        {
            SpecializationInfo specializationInfo = null;
            List<ShaderConstant> specializationConstants = GetSpecializationConstants();

            if (specializationConstants.Any())
            {
                int[] values = specializationConstants.Select(sc => specializationConstantValues[sc.Name]).ToArray();

                fixed (void* p = &values[0])
                {
                    specializationInfo = new SpecializationInfo()
                    {
                        MapEntries = specializationConstants
                           .Select(sc => new SpecializationMapEntry()
                           {
                               ConstantId = sc.Id,
                               Offset = sc.Offset,
                               Size = new UIntPtr(sc.Size),
                           }).ToArray(),
                        Data = new IntPtr(p),
                        DataSize = new UIntPtr((uint)(values.Length * sizeof(int)))
                    };
                }
            }

            return specializationInfo;
        }

        public List<ShaderConstant> GetSpecializationConstants()
        {
            List<ShaderConstant> specializationConstants = new List<ShaderConstant>();

            IEnumerable<SpecializationConstantSegment> specializationConstantSegments = _segmentCollection.GetSegments<SpecializationConstantSegment>();

            uint offset = 0;
            for (int i = 0; i < specializationConstantSegments.Count(); i++)
            {
                SpecializationConstantSegment specializationConstantSegment = specializationConstantSegments.ElementAt(i);

                Type type = Constants.GlslToNetType[specializationConstantSegment.GlslType];
                uint size = Constants.TypeToSize[type];
                ShaderConstant shaderConstant = new ShaderConstant(specializationConstantSegment.ConstantId, specializationConstantSegment.Name, type, size, offset);
                specializationConstants.Add(shaderConstant);

                offset += size;
            }

            return specializationConstants;
        }

        public Dictionary<int, ShaderUniformSet> GetDescriptorSetLayouts()
        {
            Dictionary<int, ShaderUniformSet> sets = new Dictionary<int, ShaderUniformSet>();

            IEnumerable<DescriptorSegment> descriptorSegments = _segmentCollection.GetSegments<DescriptorSegment>();

            for (int i = 0; i < descriptorSegments.Count(); i++)
            {

            }
            /*
            if (!sets.ContainsKey(set))
            {
                sets.Add(set, new ShaderUniformSet(set));
            }
            sets[set].ShaderUniforms.Add(shaderUniform);*/

            MatchCollection matches = Regex.Matches(code, @"layout *\( *(( *std140 *| *set *= *\d*) *, *){0,1} *binding *= *\d*\) *(readonly){0,1} *(buffer|uniform) ");

            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];

                int set = 0;
                string theMatch = match.ToString();
                if (theMatch.Contains("set"))
                {
                    set = int.Parse(theMatch.Substring(theMatch.IndexOf("set"), theMatch.IndexOf(",") - theMatch.IndexOf("set")).Replace("set", string.Empty).Replace("=", string.Empty).Trim());
                }
                int binding = int.Parse(theMatch.Substring(theMatch.IndexOf("binding"), theMatch.IndexOf(")") - theMatch.IndexOf("binding")).Replace("binding", string.Empty).Replace("=", string.Empty).Trim());

                string bufferOrUniformSegment = Regex.Match(theMatch, @"(buffer |uniform )").ToString();

                string codeSegment = SegmentParser.GetWholeCodeSegment(code, match.Index);

                DescriptorType descriptorType;
                string name;

                uint size = 0;
                string segmentAfterBufferOrUniform = codeSegment.Substring(codeSegment.IndexOf(bufferOrUniformSegment, StringComparison.InvariantCultureIgnoreCase) + bufferOrUniformSegment.Length);
                if (codeSegment.Contains("{"))
                {
                    descriptorType = Constants.GlslToCustomDescriptorTypeConverter.Convert(bufferOrUniformSegment.Trim());

                    int openingIndex = segmentAfterBufferOrUniform.IndexOf("{");
                    int closingIndex = segmentAfterBufferOrUniform.IndexOf("}");

                    name = Util.KillDuplicateWhiteSpaces(segmentAfterBufferOrUniform.Substring(0, openingIndex));

                    string pushconstantSegment = segmentAfterBufferOrUniform.Substring(openingIndex + 1, closingIndex - openingIndex - 1);
                    IEnumerable<string> fields = pushconstantSegment.Split(';').Select(s => s.Trim());

                    List<ShaderVariable> variables = ShaderVariableParser.Parse(fields);

                    size = (uint)variables.Sum(v => v.Size);
                }
                else
                {
                    string[] typeAndName = Util.KillDuplicateWhiteSpaces(segmentAfterBufferOrUniform).Split(' ');

                    string glslType = typeAndName[0];
                    descriptorType = Constants.GlslToDescriptorType[glslType];

                    name = typeAndName[1];
                }


                ShaderUniform shaderUniform = new ShaderUniform()
                {
                    Name = name,
                    Binding = binding,
                    DescriptorType = descriptorType,
                    StageFlags = ShaderStage,
                    Size = size,
                };


                if (!sets.ContainsKey(set))
                {
                    sets.Add(set, new ShaderUniformSet(set));
                }
                sets[set].ShaderUniforms.Add(shaderUniform);
            }

            matches = Regex.Matches(code, @"layout *\(.*\) *uniform *image2D *");
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];

                int set = 0;
                string theMatch = match.ToString();
                if (theMatch.Contains("set"))
                {
                    set = int.Parse(theMatch.Substring(theMatch.IndexOf("set"), theMatch.IndexOf(",") - theMatch.IndexOf("set")).Replace("set", string.Empty).Replace("=", string.Empty).Trim());
                }
                int binding = int.Parse(theMatch.Substring(theMatch.IndexOf("binding"), Math.Min(theMatch.LastIndexOf(","), theMatch.IndexOf(")")) - theMatch.IndexOf("binding")).Replace("binding", string.Empty).Replace("=", string.Empty).Trim());

                string bufferOrUniformSegment = Regex.Match(theMatch, @"(buffer |uniform )").ToString();

                string codeSegment = SegmentParser.GetWholeCodeSegment(code, match.Index);

                DescriptorType descriptorType;
                string name;

                string segmentAfterBufferOrUniform = codeSegment.Substring(codeSegment.IndexOf(bufferOrUniformSegment, StringComparison.InvariantCultureIgnoreCase) + bufferOrUniformSegment.Length);
                if (codeSegment.Contains("{"))
                {
                    descriptorType = Constants.GlslToCustomDescriptorTypeConverter.Convert(bufferOrUniformSegment.Trim());

                    int openingIndex = segmentAfterBufferOrUniform.IndexOf("{");
                    int closingIndex = segmentAfterBufferOrUniform.IndexOf("}");

                    name = Util.KillDuplicateWhiteSpaces(segmentAfterBufferOrUniform.Substring(0, openingIndex));

                    string pushconstantSegment = segmentAfterBufferOrUniform.Substring(openingIndex + 1, closingIndex - openingIndex - 1);
                    IEnumerable<string> fields = pushconstantSegment.Split(';').Select(s => s.Trim());

                    List<ShaderVariable> variables = ShaderVariableParser.Parse(fields);
                }
                else
                {
                    string[] typeAndName = Util.KillDuplicateWhiteSpaces(segmentAfterBufferOrUniform).Split(' ');

                    string glslType = typeAndName[0];
                    descriptorType = Constants.GlslToDescriptorType[glslType];

                    name = typeAndName[1];
                }


                ShaderUniform shaderUniform = new ShaderUniform()
                {
                    Name = name,
                    Binding = binding,
                    DescriptorType = descriptorType,
                    StageFlags = ShaderStage,
                };


                if (!sets.ContainsKey(set))
                {
                    sets.Add(set, new ShaderUniformSet(set));
                }
                sets[set].ShaderUniforms.Add(shaderUniform);
            }

            return sets;
        }



        public VertexInput[] GetVertexInput(IEnumerable<uint> bindingsWithInstanceRate = null)
        {
            List<VertexInput> vertexInputs = new List<VertexInput>();

            IEnumerable<VertexInputSegment> vertexInputSegments = _segmentCollection.GetSegments<VertexInputSegment>();

            for (int i = 0; i < vertexInputSegments.Count(); i++)
            {
                VertexInputSegment vertexInputSegment = vertexInputSegments.ElementAt(i);

                Type type = Constants.GlslToNetType[vertexInputSegment.GlslType];

                VertexInputRate inputRate = VertexInputRate.Vertex;
                if (bindingsWithInstanceRate != null && bindingsWithInstanceRate.Contains(vertexInputSegment.Binding))
                {
                    inputRate = VertexInputRate.Instance;
                }

                uint stride = Constants.TypeToSize[type];
                Format format = Constants.TypeToVertexInputFormat[type];

                if (!Constants.TypesWithMultipleLocations.Contains(type))
                {
                    VertexInput vertexInput = new VertexInput()
                    {
                        Name = vertexInputSegment.Name,
                        GlslType = vertexInputSegment.GlslType,
                        Binding = vertexInputSegment.Binding,
                        InputRate = inputRate,
                        Stride = stride,
                        Format = format,
                        Location = vertexInputSegment.Binding,
                        Offset = 0,
                    };

                    vertexInputs.Add(vertexInput);
                }
                else
                {
                    int occupiedSpaces = Constants.TypeToOccupiedVertexInputLocations[type];

                    for (uint j = 0; j < occupiedSpaces; j++)
                    {
                        uint offset = (uint)j * Constants.TypeToOffsetStrideSize[type];

                        VertexInput vertexInput = new VertexInput()
                        {
                            Name = vertexInputSegment.Name,
                            GlslType = vertexInputSegment.GlslType,
                            Binding = vertexInputSegment.Binding,
                            InputRate = inputRate,
                            Stride = stride,
                            Format = format,
                            Location = vertexInputSegment.Binding + j,
                            Offset = offset,
                        };

                        vertexInputs.Add(vertexInput);
                    }
                }
            }

            return vertexInputs.ToArray();

        }
    }
}
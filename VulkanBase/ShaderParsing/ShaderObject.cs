using Vulkan;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System;

namespace VulkanBase.ShaderParsing
{
    public unsafe class ShaderObject
    {
        public string FilePath { get; private set; }
        public ShaderStageFlags ShaderStage { get; private set; }

        private ShaderModule module;
        private string code;
        private Dictionary<string, int> specializationConstantValues;

        public ShaderModule Module
        {
            get
            {
                if (module == null)
                {
                    module = VContext.Instance.device.CreateShaderModule
                    (
                       new ShaderModuleCreateInfo()
                       {
                           CodeBytes = File.ReadAllBytes($"{FilePath}.spv")
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
            this.specializationConstantValues = specializationConstantValues;

            string codeWithComments = File.ReadAllText(FilePath);
            code = GetCodeWithoutComments(codeWithComments);

            string extension = Path.GetExtension(FilePath);
            ShaderStageFlags shaderStage = Constants.FileExtensionToShaderStageConverter.Convert(extension);
            ShaderStage = shaderStage;
        }



        public ShaderObject(string path, ShaderStageFlags shaderStage)
        {
            FilePath = path;

            string codeWithComments = File.ReadAllText(FilePath);
            code = GetCodeWithoutComments(codeWithComments);

            ShaderStage = shaderStage;
        }

        enum FindMode
        {
            None,
            SingleLine,
            MultiLine
        }

        private string GetCodeWithoutComments(string codeWithComments)
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

        public ShaderVariable[] GetPushConstants()
        {
            List<ShaderVariable> pushConstants = new List<ShaderVariable>();

            var match = Regex.Match(code, @"layout *\( *push_constant\ *\)");
            if (match.Success)
            {
                int openingIndex = code.IndexOf("{", match.Index);
                int closingIndex = code.IndexOf("}", match.Index);

                string pushconstantSegment = code.Substring(openingIndex + 1, closingIndex - openingIndex - 1);
                IEnumerable<string> fields = pushconstantSegment.Split(';').Select(s => s.Trim());

                pushConstants = ShaderVariableParser.Parse(fields);

                for (int i = 0; i < pushConstants.Count; i++)
                {
                    pushConstants[i].ShaderStage = ShaderStage;
                }
            }

            return pushConstants.ToArray();
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

            var matches = Regex.Matches(code, @"layout *\( *constant_id.*\)");

            uint offset = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];

                uint constantId = uint.Parse(match.ToString().Substring(match.ToString().IndexOf("constant_id"), match.ToString().IndexOf(")") - match.ToString().IndexOf("constant_id")).Replace("constant_id", string.Empty).Replace("=", string.Empty).Trim());

                string codeSegment = GetWholeCodeSegment(match.Index);

                string segmentAfterConst = codeSegment.Substring(codeSegment.IndexOf("const ", StringComparison.InvariantCultureIgnoreCase) + "const ".Length);

                string[] typeAndName = Util.KillDuplicateWhiteSpaces(segmentAfterConst).Split(' ');

                string glslType = typeAndName[0];
                Type type = Constants.GlslToNetType[glslType];
                string name = typeAndName[1];
                uint size = Constants.TypeToSize[type];

                specializationConstants.Add(new ShaderConstant(constantId, name, type, size, offset));

                offset += size;
            }

            return specializationConstants;
        }

        public Dictionary<int, ShaderUniformSet> GetDescriptorSetLayouts()
        {
            Dictionary<int, ShaderUniformSet> sets = new Dictionary<int, ShaderUniformSet>();

            List<DescriptorSetLayout> descriptorSetLayouts = new List<DescriptorSetLayout>();

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

                string codeSegment = GetWholeCodeSegment(match.Index);

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

                string codeSegment = GetWholeCodeSegment(match.Index);

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

        private string GetWholeCodeSegment(int index)
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
                else if (code[i] == ';')
                {
                    if (curlyBalance == 0)
                    {
                        return code.Substring(index, i - index);
                    }
                }
            }

            throw new Exception("GLSL malformed");
        }

        public VertexInput[] GetVertexInput(IEnumerable<uint> bindingsWithInstanceRate = null)
        {
            List<VertexInput> vertexInputs = new List<VertexInput>();

            MatchCollection matches = Regex.Matches(code, @"layout *\( *location *= *\d* *\) *in *");

            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];

                uint binding = uint.Parse(match.ToString().Substring(match.ToString().IndexOf("location"), match.ToString().IndexOf(")") - match.ToString().IndexOf("location")).Replace("location", string.Empty).Replace("=", string.Empty).Trim());

                string codeSegment = GetWholeCodeSegment(match.Index);

                string segmentAfterIn = codeSegment.Substring(codeSegment.IndexOf("in ", StringComparison.InvariantCultureIgnoreCase) + "in ".Length);

                string[] typeAndName = Util.KillDuplicateWhiteSpaces(segmentAfterIn).Split(' ');

                string glslType = typeAndName[0];
                Type type = Constants.GlslToNetType[glslType];

                string name = typeAndName[1];

                VertexInputRate inputRate = VertexInputRate.Vertex;
                if (bindingsWithInstanceRate != null && bindingsWithInstanceRate.Contains(binding))
                {
                    inputRate = VertexInputRate.Instance;
                }

                uint stride = Constants.TypeToSize[type];
                Format format = Constants.TypeToVertexInputFormat[type];

                if (!Constants.TypesWithMultipleLocations.Contains(type))
                {
                    VertexInput vertexInput = new VertexInput()
                    {
                        Name = name,
                        GlslType = glslType,
                        Binding = binding,
                        InputRate = inputRate,
                        Stride = stride,
                        Format = format,
                        Location = binding,
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
                            Name = name,
                            GlslType = glslType,
                            Binding = binding,
                            InputRate = inputRate,
                            Stride = stride,
                            Format = format,
                            Location = binding + j,
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
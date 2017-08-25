using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace VulkanBase.ShaderParsing
{
    public unsafe class GenericComputePipeline : GenericPipeline
    {
        private PipelineShaderStageCreateInfo _shaderStage;
                
        public GenericComputePipeline(List<ShaderObject> shaderObjects) 
            : base(shaderObjects, null)
        {
            _shaderStage = GetShaderStages(shaderObjects);            
            Pipeline = CreateComputePipeline(_shaderStage,  PipelineLayout);
        }
        
        private PipelineShaderStageCreateInfo GetShaderStages(List<ShaderObject> shaderObjects)
        {
            return shaderObjects.Select(shaderObject => new PipelineShaderStageCreateInfo()
            {
                SpecializationInfo = shaderObject.GetSpecializationInfo(),
                Stage = shaderObject.ShaderStage,
                Module = shaderObject.Module,
                Name = "main",
            }).First();
        }

        private VertexInput[] GetVertexInput(List<ShaderObject> shaderObjects, IEnumerable<uint> bindingsWithInstanceRate)
        {
            return shaderObjects.Where(s => s.ShaderStage == ShaderStageFlags.Vertex).SelectMany(s => s.GetVertexInput(bindingsWithInstanceRate)).ToArray();
        }

        private VertexInputBindingDescription[] GetVertexInputBindingDescriptions(VertexInput[] vertexInput)
        {
            return vertexInput
                .Where(vi => vi.Location == vi.Binding)
                .Select(vi =>
                    new VertexInputBindingDescription()
                    {
                        Binding = vi.Binding,
                        InputRate = vi.InputRate,
                        Stride = vi.Stride,
                    }
                ).ToArray();
        }

        private VertexInputAttributeDescription[] GetVertexInputAttributeDescription(VertexInput[] vertexInput)
        {
            return vertexInput
                .Select(vi =>
                    new VertexInputAttributeDescription()
                    {
                        Binding = vi.Binding,
                        Format = vi.Format,
                        Location = vi.Location,
                        Offset = vi.Offset,
                    }
                ).ToArray();
        }


        private Pipeline CreateComputePipeline(
            PipelineShaderStageCreateInfo shaderStage,
            PipelineLayout pipelineLayout)
        {
            return VContext.Instance.device.CreateComputePipelines(
                null,
                new ComputePipelineCreateInfo[]
                {
                    new ComputePipelineCreateInfo()
                    {
                        Layout = pipelineLayout,
                        Stage = shaderStage,
                    }
                }
            ).First();
        }

        
    }
}

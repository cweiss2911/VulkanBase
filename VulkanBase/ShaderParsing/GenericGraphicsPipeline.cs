using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace VulkanBase.ShaderParsing
{
    public unsafe class GenericGraphicsPipeline : GenericPipeline
    {
        private PipelineShaderStageCreateInfo[] _shaderStages;
        private VertexInput[] _vertexInput;
        
        public GenericGraphicsPipeline(List<ShaderObject> shaderObjects) : this(shaderObjects, null, null)
        {
        }

        public GenericGraphicsPipeline(List<ShaderObject> shaderObjects, IEnumerable<uint> bindingsWithInstanceRate) : this(shaderObjects, bindingsWithInstanceRate, null)
        {
        }

        public GenericGraphicsPipeline(List<ShaderObject> shaderObjects, GraphicsPipelineCreateInfo graphicsPipelineCreateInfo) : this(shaderObjects, null, graphicsPipelineCreateInfo)
        {
        }

        public GenericGraphicsPipeline(List<ShaderObject> shaderObjects, IEnumerable<uint> bindingsWithInstanceRate, GraphicsPipelineCreateInfo graphicsPipelineCreateInfo) 
            : base(shaderObjects, bindingsWithInstanceRate)
        {
            _shaderStages = GetShaderStages(shaderObjects);
            _vertexInput = GetVertexInput(shaderObjects, bindingsWithInstanceRate);

            VertexInputBindingDescription[] vertexInputBindingDescription = GetVertexInputBindingDescriptions(_vertexInput);
            VertexInputAttributeDescription[] vertexInputAttributeDescription = GetVertexInputAttributeDescription(_vertexInput);

            Pipeline = CreateGraphicsPipeline(_shaderStages, vertexInputBindingDescription, vertexInputAttributeDescription, PipelineLayout, graphicsPipelineCreateInfo);
        }

        
        private PipelineShaderStageCreateInfo[] GetShaderStages(List<ShaderObject> shaderObjects)
        {
            return shaderObjects.Select(shaderObject => new PipelineShaderStageCreateInfo()
            {
                Stage = shaderObject.ShaderStage,
                Module = shaderObject.Module,
                Name = "main",
            }).ToArray();
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

        private Pipeline CreateGraphicsPipeline(
            PipelineShaderStageCreateInfo[] shaderStages,
            VertexInputBindingDescription[] vertexInputBindingDescription,
            VertexInputAttributeDescription[] vertexInputAttributeDescription,
            PipelineLayout pipelineLayout,
            GraphicsPipelineCreateInfo customPipelineProperties = null)
        {
            return VContext.Instance.device.CreateGraphicsPipelines
            (
                null,
                new GraphicsPipelineCreateInfo[]
                {
                    new GraphicsPipelineCreateInfo()
                    {
                        Stages = shaderStages,
                        VertexInputState = new PipelineVertexInputStateCreateInfo()
                        {
                            VertexBindingDescriptions = vertexInputBindingDescription,
                            VertexAttributeDescriptions = vertexInputAttributeDescription,
                        },
                        InputAssemblyState = customPipelineProperties?.InputAssemblyState ?? VContext.Instance.DefaultGraphicsPipelineCreateInfo.InputAssemblyState,
                        ViewportState = customPipelineProperties?.ViewportState ?? VContext.Instance.DefaultGraphicsPipelineCreateInfo.ViewportState,
                        RasterizationState = customPipelineProperties?.RasterizationState ?? VContext.Instance.DefaultGraphicsPipelineCreateInfo.RasterizationState,
                        DepthStencilState = customPipelineProperties?.DepthStencilState ?? VContext.Instance.DefaultGraphicsPipelineCreateInfo.DepthStencilState,
                        ColorBlendState = customPipelineProperties?.ColorBlendState ?? VContext.Instance.DefaultGraphicsPipelineCreateInfo.ColorBlendState,
                        MultisampleState = customPipelineProperties?.MultisampleState ?? VContext.Instance.DefaultGraphicsPipelineCreateInfo.MultisampleState,
                        RenderPass = VContext.Instance.RenderPass,
                        Layout =  pipelineLayout
                    }
                }
            ).First();
        }
    }
}

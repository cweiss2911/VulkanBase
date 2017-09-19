using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;
using VulkanBase.BufferObjects;

namespace VulkanBase.ShaderParsing
{
    public abstract unsafe class GenericPipeline
    {
        public DescriptorSetLayout[] DescriptorSetLayouts { get; private set; }
        public PipelineLayout PipelineLayout { get; private set; }
        public Pipeline Pipeline { get; protected set; }
        public PushConstantManager PushConstantManager { get; private set; }

        protected List<ShaderObject> _shaderObjects;
        protected PushConstantRange[] _pushConstantRanges;

        public ShaderUniformSet[] ShaderUniformSets { get; private set; }
        
        public GenericPipeline(List<ShaderObject> shaderObjects) : this(shaderObjects, null)
        {
        }

        public GenericPipeline(List<ShaderObject> shaderObjects, IEnumerable<uint> bindingsWithInstanceRate)
        {
            _shaderObjects = shaderObjects;

            ShaderUniformSets = GetShaderUniformSets(shaderObjects);
            DescriptorSetLayouts = CreateDescriptorSetLayouts(ShaderUniformSets);

            PushConstantManager = new PushConstantManager(shaderObjects);
            _pushConstantRanges = PushConstantManager.GetPushConstantRanges();

            PipelineLayout = CreatePipelinePipelineLayout(_pushConstantRanges, DescriptorSetLayouts);

            PushConstantManager.PipelineLayout = PipelineLayout;
        }


        private ShaderUniformSet[] GetShaderUniformSets(List<ShaderObject> shaderObjects)
        {
            return shaderObjects.SelectMany(s => s.GetDescriptorSetLayouts().Values).OrderBy(s => s.Set).ToArray();
        }

        private DescriptorSetLayout[] CreateDescriptorSetLayouts(ShaderUniformSet[] shaderUniformSets)
        {
            return shaderUniformSets.Select(sh =>
                 VContext.Instance.device.CreateDescriptorSetLayout
                 (
                     new DescriptorSetLayoutCreateInfo()
                     {
                         Bindings = sh.ShaderUniforms.Select(shaderUniform =>
                             new DescriptorSetLayoutBinding()
                             {
                                 Binding = (uint)shaderUniform.Binding,
                                 DescriptorCount = 1,
                                 DescriptorType = shaderUniform.DescriptorType,
                                 StageFlags = shaderUniform.StageFlags
                             }
                         ).ToArray()
                     }
                 )
            ).ToArray();
        }



        public DescriptorSet CreateDescriptorSet(int setIndex, int binding, BufferWithMemory buffer)
        {
            ValidateDiscriporPool();

            DescriptorSetLayout descriptorSetLayout = DescriptorSetLayouts[setIndex];
            ShaderUniformSet shaderUniformSet = ShaderUniformSets[setIndex];

            DescriptorSet descriptorSet = VContext.Instance.device.AllocateDescriptorSets(
                    new DescriptorSetAllocateInfo()
                    {
                        DescriptorPool = VContext.Instance.descriptorPool,
                        SetLayouts = new DescriptorSetLayout[]
                        {
                            descriptorSetLayout
                        }
                    }
                ).First();

            ShaderUniform shaderUniformWithCorrectBinding = shaderUniformSet.ShaderUniforms.Where(su => su.Binding == binding).First();

            VContext.Instance.device.UpdateDescriptorSet(
                new WriteDescriptorSet()
                {
                    DescriptorType = shaderUniformWithCorrectBinding.DescriptorType,
                    DstBinding = (uint)shaderUniformWithCorrectBinding.Binding,
                    DstSet = descriptorSet,
                    BufferInfo = new DescriptorBufferInfo[]
                    {
                        new DescriptorBufferInfo()
                        {
                            Buffer = buffer.Buffer,
                            Offset = 0,
                            Range = buffer.Size,
                        }
                    }
                },
                null
            );

            return descriptorSet;
        }

        public DescriptorSet CreateDescriptorSet(int setIndex, int binding, ImageView imageView, ImageLayout imageLayout = ImageLayout.ColorAttachmentOptimal)
        {
            ValidateDiscriporPool();

            DescriptorSetLayout descriptorSetLayout = DescriptorSetLayouts[setIndex];
            ShaderUniformSet shaderUniformSet = ShaderUniformSets[setIndex];

            DescriptorSet descriptorSet = VContext.Instance.device.AllocateDescriptorSets
            (
               new DescriptorSetAllocateInfo()
               {
                   DescriptorPool = VContext.Instance.descriptorPool,
                   DescriptorSetCount = 1,
                   SetLayouts = new DescriptorSetLayout[]
                   {
                        descriptorSetLayout
                   }
               }
            ).First();


            ShaderUniform shaderUniformWithCorrectBinding = shaderUniformSet.ShaderUniforms.Where(su => su.Binding == binding).First();

            VContext.Instance.device.UpdateDescriptorSet(
                new WriteDescriptorSet()
                {
                    DescriptorType = shaderUniformWithCorrectBinding.DescriptorType,
                    DstBinding = (uint)shaderUniformWithCorrectBinding.Binding,
                    DstSet = descriptorSet,

                    ImageInfo = new DescriptorImageInfo[]
                    {
                        new DescriptorImageInfo()
                        {
                            ImageLayout = imageLayout,
                            ImageView = imageView,
                            Sampler = VContext.Instance.sampler
                        }
                    }
                },
                null
            );

            return descriptorSet;
        }

        private static void ValidateDiscriporPool()
        {
            if (VContext.Instance.descriptorPool == null)
            {
                throw new Exception("DescriptorPool not initialized");
            }
        }

        public DescriptorSet CreateDescriptorSet(int setIndex, int binding, BufferviewWithMemory bufferviewWithMemory)
        {
            ValidateDiscriporPool();

            DescriptorSetLayout descriptorSetLayout = DescriptorSetLayouts[setIndex];
            ShaderUniformSet shaderUniformSet = ShaderUniformSets[setIndex];

            DescriptorSet descriptorSet = VContext.Instance.device.AllocateDescriptorSets(
                    new DescriptorSetAllocateInfo()
                    {
                        DescriptorPool = VContext.Instance.descriptorPool,
                        SetLayouts = new DescriptorSetLayout[]
                        {
                            descriptorSetLayout
                        }
                    }
                ).First();

            ShaderUniform shaderUniformWithCorrectBinding = shaderUniformSet.ShaderUniforms.Where(su => su.Binding == binding).First();
            
            VContext.Instance.device.UpdateDescriptorSet
           (
               new WriteDescriptorSet()
               {
                   DescriptorType = shaderUniformWithCorrectBinding.DescriptorType,
                   DstBinding = (uint)shaderUniformWithCorrectBinding.Binding,
                   DstSet = descriptorSet,
                   BufferInfo = new DescriptorBufferInfo[]
                   {
                        new DescriptorBufferInfo()
                        {
                            Buffer = bufferviewWithMemory.Buffer,
                            Offset = 0,
                            Range =  bufferviewWithMemory.Size,
                        }
                   },
                   TexelBufferView = new BufferView[]
                   {
                        bufferviewWithMemory.Bufferview
                   },
               },
               null
           );

            return descriptorSet;
        }

        private PipelineLayout CreatePipelinePipelineLayout(PushConstantRange[] pushConstantRanges, DescriptorSetLayout[] descriptorSetLayouts)
        {
            return VContext.Instance.device.CreatePipelineLayout
            (
               new PipelineLayoutCreateInfo()
               {
                   PushConstantRanges = pushConstantRanges,
                   SetLayouts = descriptorSetLayouts
               }
            );
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

        private Pipeline CreatePipeline(
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

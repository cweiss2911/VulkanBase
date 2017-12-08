using System;
using System.Collections.Generic;
using System.Reflection;
using Vulkan;
using VulkanBase;
using VulkanBase.SafeComfort.VertexInputBinding;
using VulkanBase.ShaderParsing;
using VulkanBase.TextureLoading;

namespace VulkanBase.Text
{
    public class TextDisplayer
    {
        private List<Label> _labels = new List<Label>();

        private GenericGraphicsPipeline _textPipeline;

        private Font _font;

        private ImageWithMemory _imageWithMemory;
        private DescriptorSet _imageDescriptorSet;
        
        public float TotalWidth { get; private set; }


        public TextDisplayer()
        {
            _textPipeline = new GenericGraphicsPipeline(
                    new List<ShaderObject>()
                    {
                    new EmbeddedShaderObject("VulkanBase.Text.Shader.text.frag"),
                    new EmbeddedShaderObject("VulkanBase.Text.Shader.text.vert")
                    }
                );

            _font = new Font(Properties.Resources.Courier, Properties.Resources.CourierCharacterWidth);
            _imageWithMemory = TextureLoader.CreateImageWithMemory(_font.Texture);
            _imageDescriptorSet = _textPipeline.CreateDescriptorSet(0, 0, _imageWithMemory.ImageView);

        }

        public void AddText(string text, Vector2 position)
        {
            Label label = new Label(text, position, _font);

            _labels.Add(label);

        }

        public void AddLabel(Label label)
        {
            _labels.Add(label);
        }

        public void RemoveLabel(Label label)
        {
            _labels.Remove(label);
        }

        public void Render(CommandBuffer commandBuffer)
        {
            commandBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, _textPipeline.Pipeline);
            commandBuffer.CmdBindDescriptorSet(PipelineBindPoint.Graphics, _textPipeline.PipelineLayout, 0, _imageDescriptorSet, null);
            for (int i = 0; i < _labels.Count; i++)
            {
                _labels[i].Render(commandBuffer, _textPipeline);
            }


        }
    }
}
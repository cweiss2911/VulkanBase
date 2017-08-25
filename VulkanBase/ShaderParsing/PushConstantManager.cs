using System;
using System.Collections.Generic;
using System.Linq;
using Vulkan;

namespace VulkanBase.ShaderParsing
{
    public unsafe class PushConstantManager
    {
        private List<ShaderObject> _shaderObjects;
        private ShaderVariable[] _pushConstants;
        private Dictionary<string, ShaderVariable> _pushConstantDict;


        public PipelineLayout PipelineLayout { get; internal set; }

        public PushConstantManager(List<ShaderObject> shaderObjects)
        {
            _shaderObjects = shaderObjects;
            _pushConstants = GetPushConstants(_shaderObjects);
            _pushConstantDict = ConvertToPushConstantDict(_pushConstants);
        }

        private ShaderVariable[] GetPushConstants(List<ShaderObject> shaderObjects)
        {
            return shaderObjects.SelectMany(s => s.GetPushConstants()).ToArray();
        }

        private Dictionary<string, ShaderVariable> ConvertToPushConstantDict(ShaderVariable[] pushConstants)
        {
            Dictionary<string, ShaderVariable> pushConstantDict = new Dictionary<string, ShaderVariable>();
            for (int i = 0; i < pushConstants.Length; i++)
            {
                ShaderVariable pushConstant = pushConstants[i];
                pushConstantDict[pushConstant.Name] = pushConstant;
            }
            return pushConstantDict;
        }

        public PushConstantRange[] GetPushConstantRanges()
        {
            return _pushConstants.GroupBy(p => p.ShaderStage)
                .Select(
                grp =>
                new PushConstantRange()
                {
                    StageFlags = grp.Key,
                    Size = (uint)grp.Sum(g => g.Size),
                    Offset = grp.Min(g => g.Offset),
                }).ToArray();
        }


        public void SetPushConstant(CommandBuffer commandBuffer, string pushConstantName, Vector3 value)
        {
            ShaderVariable pushConstant = _pushConstantDict[pushConstantName];
            commandBuffer.CmdPushConstants(PipelineLayout, pushConstant.ShaderStage, pushConstant.Offset, pushConstant.Size, new IntPtr(&value));
        }

        public void SetPushConstant(CommandBuffer commandBuffer, string pushConstantName, Vector4 value)
        {
            ShaderVariable pushConstant = _pushConstantDict[pushConstantName];
            commandBuffer.CmdPushConstants(PipelineLayout, pushConstant.ShaderStage, pushConstant.Offset, pushConstant.Size, new IntPtr(&value));
        }

        public void SetPushConstant(CommandBuffer commandBuffer, string pushConstantName, float value)
        {
            ShaderVariable pushConstant = _pushConstantDict[pushConstantName];
            commandBuffer.CmdPushConstants(PipelineLayout, pushConstant.ShaderStage, pushConstant.Offset, pushConstant.Size, new IntPtr(&value));
        }

        public void SetPushConstant(CommandBuffer commandBuffer, string pushConstantName, int value)
        {
            ShaderVariable pushConstant = _pushConstantDict[pushConstantName];
            commandBuffer.CmdPushConstants(PipelineLayout, pushConstant.ShaderStage, pushConstant.Offset, pushConstant.Size, new IntPtr(&value));
        }

        public void SetPushConstant(CommandBuffer commandBuffer, string pushConstantName, uint value)
        {
            ShaderVariable pushConstant = _pushConstantDict[pushConstantName];
            commandBuffer.CmdPushConstants(PipelineLayout, pushConstant.ShaderStage, pushConstant.Offset, pushConstant.Size, new IntPtr(&value));
        }

        public void SetPushConstant(CommandBuffer commandBuffer, string pushConstantName, Matrix4 value)
        {
            ShaderVariable pushConstant = _pushConstantDict[pushConstantName];
            commandBuffer.CmdPushConstants(PipelineLayout, pushConstant.ShaderStage, pushConstant.Offset, pushConstant.Size, new IntPtr(&value));
        }
    }
}
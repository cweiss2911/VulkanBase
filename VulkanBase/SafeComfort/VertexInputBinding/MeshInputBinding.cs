using System;
using System.Collections.Generic;
using Vulkan;

namespace VulkanBase.SafeComfort.VertexInputBinding
{
    public class MeshInputBinding
    {
        private List<InputBinding> _inputBindings = new List<InputBinding>();
        
        public void AddInputBinding(InputBinding inputBinding)
        {
            _inputBindings.Add(inputBinding);
        }

        public void Bind(CommandBuffer commandBuffer)
        {
            for (int i = 0; i < _inputBindings.Count; i++)
            {
                _inputBindings[i].Bind(commandBuffer);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _inputBindings.Count; i++)
            {
                _inputBindings[i].BufferWithMemory.Destroy();
            }
            _inputBindings = new List<InputBinding>();
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Vulkan;

namespace VulkanBase.SafeComfort.VertexInputBinding
{
    public class InputBindingM4 : InputBinding
    {
        public InputBindingM4(BufferUsageFlags bufferUsageFlags, List<Matrix4> data, int index) : base(bufferUsageFlags, index)
        {
            BufferWithMemory = VContext.Instance.BufferManager.CreateBuffer(bufferUsageFlags, data.Count() * Matrix4.SizeInBytes, data.ToArray());
        }
    }
}


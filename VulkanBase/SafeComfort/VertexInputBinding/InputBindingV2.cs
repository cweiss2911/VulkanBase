using System.Collections.Generic;
using System.Linq;
using Vulkan;

namespace VulkanBase.SafeComfort.VertexInputBinding
{
    public class InputBindingV2 : InputBinding
    {
        public InputBindingV2(BufferUsageFlags bufferUsageFlags, List<Vector2> data, int index) : base(bufferUsageFlags, index)
        {
            BufferWithMemory = VContext.Instance.BufferManager.CreateBuffer(bufferUsageFlags, data.Count() * Vector2.SizeInBytes, data.ToArray());
        }
    }
}

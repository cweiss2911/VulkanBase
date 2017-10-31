using System.Collections.Generic;
using System.Linq;
using Vulkan;

namespace VulkanBase.SafeComfort.VertexInputBinding
{
    public class InputBindingV3 : InputBinding
    {
        public InputBindingV3(BufferUsageFlags bufferUsageFlags, List<Vector3> data, int index) : base(bufferUsageFlags, index)
        {
            BufferWithMemory = VContext.Instance.BufferManager.CreateBuffer(bufferUsageFlags, data.Count() * Vector3.SizeInBytes, data.ToArray());
        }
    }
}

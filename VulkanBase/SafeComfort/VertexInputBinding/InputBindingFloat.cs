using System.Collections.Generic;
using System.Linq;
using Vulkan;

namespace VulkanBase.SafeComfort.VertexInputBinding
{
    public class InputBindingFloat : InputBinding
    {
        public InputBindingFloat(BufferUsageFlags bufferUsageFlags, List<float> data, int index) : base(bufferUsageFlags, index)
        {
            BufferWithMemory = VContext.Instance.BufferManager.CreateBuffer(bufferUsageFlags, data.Count() * sizeof(float), data.ToArray());
        }
    }
}

}

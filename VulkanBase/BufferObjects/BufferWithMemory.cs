using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;
using VulkanBase.BufferObjects;

namespace VulkanBase
{
    public class BufferWithMemory
    {
        public Buffer Buffer { get; set; }
        public DeviceMemory Memory { get; set; }
        public uint Size { get; internal set; }

        public void Destroy()
        {
            VContext.Instance.device.DestroyBuffer(Buffer);            
            VContext.Instance.device.FreeMemory(Memory);
        }
    }
}

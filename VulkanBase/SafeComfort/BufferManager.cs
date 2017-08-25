using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace VulkanBase.SafeComfort
{
    public unsafe class BufferManager
    {
        public virtual BufferWithMemory CreateBuffer(BufferUsageFlags usageFlags, DeviceSize size, Matrix4[] data)
        {
            BufferWithMemory bufferWithMemory;
            fixed (void* p = &data[0])
            {
                bufferWithMemory = VContext.Instance.CreateBuffer(usageFlags, size, p);
            }
            return bufferWithMemory;
        }
        
        public virtual BufferWithMemory CreateBuffer(BufferUsageFlags usageFlags, DeviceSize size, Vector3[] data)
        {
            BufferWithMemory bufferWithMemory;
            fixed (void* p = &data[0])
            {
                bufferWithMemory = VContext.Instance.CreateBuffer(usageFlags, size, p);
            }
            return bufferWithMemory;
        }

        public virtual BufferWithMemory CreateBuffer(BufferUsageFlags usageFlags, DeviceSize size, Vector2[] data)
        {
            BufferWithMemory bufferWithMemory;
            fixed (void* p = &data[0])
            {
                bufferWithMemory = VContext.Instance.CreateBuffer(usageFlags, size, p);
            }
            return bufferWithMemory;
        }

        public virtual BufferWithMemory CreateBuffer(BufferUsageFlags usageFlags, DeviceSize size, float[] data)
        {
            BufferWithMemory bufferWithMemory;
            fixed (void* p = &data[0])
            {
                bufferWithMemory = VContext.Instance.CreateBuffer(usageFlags, size, p);
            }
            return bufferWithMemory;
        }

    }
}

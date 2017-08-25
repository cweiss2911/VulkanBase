using Vulkan;
using VulkanBase.BufferObjects;
using VulkanBase.SafeComfort;

namespace VulkanBase
{
    public class BufferviewManager
    {
        public virtual BufferviewWithMemory CreateBufferview(BufferUsageFlags usageFlags, DeviceSize size, Matrix4[] data, Format format)
        {
            BufferviewWithMemory bufferviewWithMemory = new BufferviewWithMemory(VContext.Instance.BufferManager.CreateBuffer(usageFlags, size, data));
            bufferviewWithMemory.Bufferview = CreateBufferview(bufferviewWithMemory.Buffer, format, size);

            return bufferviewWithMemory;
        }

        public virtual BufferviewWithMemory CreateBufferview(BufferUsageFlags usageFlags, DeviceSize size, Vector3[] data, Format format)
        {
            BufferviewWithMemory bufferviewWithMemory = new BufferviewWithMemory(VContext.Instance.BufferManager.CreateBuffer(usageFlags, size, data));
            bufferviewWithMemory.Bufferview = CreateBufferview(bufferviewWithMemory.Buffer, format, size);

            return bufferviewWithMemory;
        }

        public virtual BufferviewWithMemory CreateBufferview(BufferUsageFlags usageFlags, DeviceSize size, Vector2[] data, Format format)
        {
            BufferviewWithMemory bufferviewWithMemory = new BufferviewWithMemory(VContext.Instance.BufferManager.CreateBuffer(usageFlags, size, data));
            bufferviewWithMemory.Bufferview = CreateBufferview(bufferviewWithMemory.Buffer, format, size);

            return bufferviewWithMemory;
        }

        public virtual BufferviewWithMemory CreateBufferview(BufferUsageFlags usageFlags, DeviceSize size, float[] data, Format format)
        {
            BufferviewWithMemory bufferviewWithMemory = new BufferviewWithMemory(VContext.Instance.BufferManager.CreateBuffer(usageFlags, size, data));
            bufferviewWithMemory.Bufferview = CreateBufferview(bufferviewWithMemory.Buffer, format, size);

            return bufferviewWithMemory;
        }


        public BufferView CreateBufferview(Buffer buffer, Format format, DeviceSize size)
        {
            BufferView bufferView = VContext.Instance.device.CreateBufferView
            (
                new BufferViewCreateInfo()
                {
                    Buffer = buffer,
                    Format = format,
                    Offset = 0,
                    Range = size
                }
            );

            return bufferView;
        }
    }
}
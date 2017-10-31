using Vulkan;

namespace VulkanBase.SafeComfort.VertexInputBinding
{
    public abstract class InputBinding
    {
        protected uint _index;
        protected BufferUsageFlags _bufferUsageFlags;

        public BufferWithMemory BufferWithMemory { get; protected set; }

        public InputBinding(BufferUsageFlags bufferUsageFlags,  int index)
        {
            _bufferUsageFlags = bufferUsageFlags;
            _index = (uint)index;
        }

        public void Bind(CommandBuffer commandBuffer)
        {
            commandBuffer.CmdBindVertexBuffer(_index, BufferWithMemory.Buffer, 0);
        }
    }
}

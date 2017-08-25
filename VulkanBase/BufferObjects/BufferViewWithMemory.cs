using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace VulkanBase.BufferObjects
{
    public class BufferviewWithMemory : BufferWithMemory
    {
        public BufferView Bufferview { get; set; }

        public BufferviewWithMemory()
        {
           
        }

        public BufferviewWithMemory(BufferWithMemory bufferWithMemory)
        {
            Buffer = bufferWithMemory.Buffer;
            Memory = bufferWithMemory.Memory;
            Size = bufferWithMemory.Size;
        }


    }
}

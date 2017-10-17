using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VulkanBase.SafeComfort
{
    public unsafe static class Copier
    {

        public static void CopyToMappedMemory(Matrix4 source, IntPtr target, uint size)
        {
            Util.CopyMemory(target, new IntPtr(&source), size);
        }

        public static void CopyToMappedMemory(Matrix4 source, IntPtr target)
        {
            Util.CopyMemory(target, new IntPtr(&source), Matrix4.SizeInBytes);
        }

        public static void CopyToMappedMemory(float[] source, IntPtr target, uint size)
        {
            fixed (void* p = &source[0])
            {
                Util.CopyMemory(target, new IntPtr(p), size);
            }
        }

        public static void CopyToMappedMemory(float[] source, IntPtr target)
        {
            fixed (void* p = &source[0])
            {
                Util.CopyMemory(target, new IntPtr(p), (uint)(sizeof(float) * source.Length));
            }
        }

        public static void CopyToMappedMemory(float[,] source, IntPtr target)
        {
            fixed (void* p = &source[0, 0])
            {
                Util.CopyMemory(target, new IntPtr(p), (uint)(sizeof(float) * source.GetLength(0) * source.GetLength(1)));
            }
        }




        public static void CopyFromMappedMemory(IntPtr source, float[] target, uint size)
        {
            fixed (void* p = &target[0])
            {
                Util.CopyMemory(new IntPtr(p), source, size);
            }
        }


        public static void CopyFromMappedMemory(IntPtr source, float[] target)
        {
            fixed (void* p = &target[0])
            {
                Util.CopyMemory(new IntPtr(p), source, (uint)(sizeof(float) * target.Length));
            }
        }

        public static void CopyFromMappedMemory(IntPtr source, float[,] target)
        {
            fixed (void* p = &target[0, 0])
            {
                Util.CopyMemory(new IntPtr(p), source, (uint)(sizeof(float) * target.GetLength(0) * target.GetLength(1)));
            }
        }

    }
}

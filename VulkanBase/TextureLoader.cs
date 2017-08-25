﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace VulkanBase
{
    public unsafe static class TextureLoader
    {
        private static Lazy<CommandBuffer> _commandBuffer = new Lazy<CommandBuffer>(() =>
            VContext.Instance.device.AllocateCommandBuffers
            (
                new CommandBufferAllocateInfo()
                {
                    CommandPool = VContext.Instance.commandPool,
                    CommandBufferCount = 1,
                    Level = CommandBufferLevel.Primary
                }
            ).First());


        public static ImageView CreateImageView(
            uint width,
            uint height,
            ImageUsageFlags imageUsageFlags,
            ImageLayout imageLayout,
            AccessFlags accessMask)

        {
            Vulkan.Image image = VContext.Instance.device.CreateImage
            (
                new ImageCreateInfo()
                {
                    ImageType = ImageType.Image2D,
                    Format = VContext.bmpColorFormat,
                    Extent = new Extent3D()
                    {
                        Width = width,
                        Height = height,
                        Depth = 1
                    },
                    MipLevels = 1,
                    ArrayLayers = 1,
                    Samples = SampleCountFlags.Count1,
                    Tiling = ImageTiling.Optimal,
                    Usage = imageUsageFlags,
                    SharingMode = SharingMode.Exclusive,
                    InitialLayout = ImageLayout.Undefined
                }
            );

            MemoryRequirements textureMemoryRequirements = VContext.Instance.device.GetImageMemoryRequirements(image);
            uint memoryTypeIndex = Util.GetMemoryTypeIndex(textureMemoryRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocal);

            DeviceMemory textureImageMemory = VContext.Instance.device.AllocateMemory
            (
                new MemoryAllocateInfo()
                {
                    AllocationSize = textureMemoryRequirements.Size,
                    MemoryTypeIndex = memoryTypeIndex
                }
            );
            VContext.Instance.device.BindImageMemory(image, textureImageMemory, 0);

            if (imageLayout != ImageLayout.Undefined)
            {
                CommandBuffer commandBuffer = _commandBuffer.Value;
                commandBuffer.Begin(new CommandBufferBeginInfo());

                ImageSubresourceRange subresourceRange = new ImageSubresourceRange()
                {
                    AspectMask = ImageAspectFlags.Color,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    LayerCount = 1,
                };


                ImageMemoryBarrier undefinedToTranserDstBarrier = new ImageMemoryBarrier()
                {
                    OldLayout = ImageLayout.Undefined,
                    NewLayout = imageLayout,
                    Image = image,
                    SubresourceRange = subresourceRange,
                    SrcAccessMask = 0,
                    DstAccessMask = accessMask
                };


                commandBuffer.CmdPipelineBarrier(
                    PipelineStageFlags.AllCommands,
                    PipelineStageFlags.AllCommands,
                    0,
                    null,
                    null,
                    undefinedToTranserDstBarrier);



                commandBuffer.CmdPipelineBarrier(
                    PipelineStageFlags.AllCommands,
                    PipelineStageFlags.AllCommands,
                    0,
                    null,
                    null,
                    undefinedToTranserDstBarrier);

                commandBuffer.End();


                Fence fence = VContext.Instance.device.CreateFence(new FenceCreateInfo());

                SubmitInfo submitInfo = new SubmitInfo()
                {
                    CommandBuffers = new CommandBuffer[] { commandBuffer }
                };


                VContext.Instance.deviceQueue.Submit(submitInfo, fence);

                VContext.Instance.device.WaitForFences(new Fence[] { fence }, true, ulong.MaxValue);
                commandBuffer.Reset(CommandBufferResetFlags.ReleaseResources);

                VContext.Instance.device.DestroyFence(fence);
            }

            ImageView textureImageView = VContext.Instance.CreateColorImageView(image);

            return textureImageView;
        }


        public static ImageView CreateImageView(
            Bitmap texture,
            bool forceLinear = false,
            ImageUsageFlags imageUsageFlags = ImageUsageFlags.Sampled,
            ImageLayout imageLayout = ImageLayout.ShaderReadOnlyOptimal)
        {
            System.Drawing.Imaging.BitmapData data = texture.LockBits(new System.Drawing.Rectangle(0, 0, texture.Width, texture.Height),
                                                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            int length = data.Stride * data.Height;

            DeviceSize imageSize = length;

            byte[] bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, length);

            texture.UnlockBits(data);

            BufferWithMemory stagingBuffer;
            fixed (byte* source = &bytes[0])
            {
                stagingBuffer = VContext.Instance.CreateBuffer(BufferUsageFlags.TransferSrc, imageSize, source);
            }




            List<BufferImageCopy> bufferCopyRegions = new List<BufferImageCopy>();
            DeviceSize offset = 0;
            for (uint i = 0; i < 1; i++)
            {
                BufferImageCopy bufferCopyRegion = new BufferImageCopy()
                {
                    ImageSubresource = new ImageSubresourceLayers()
                    {
                        AspectMask = ImageAspectFlags.Color,
                        MipLevel = i,
                        BaseArrayLayer = 0,
                        LayerCount = 1
                    },
                    ImageExtent = new Extent3D()
                    {
                        Width = (uint)texture.Width,
                        Height = (uint)texture.Height,
                        Depth = 1,
                    },
                    BufferOffset = offset
                };



                bufferCopyRegions.Add(bufferCopyRegion);

                offset += imageSize;
            }



            Vulkan.Image textureImage = VContext.Instance.device.CreateImage
            (
                new ImageCreateInfo()
                {
                    ImageType = ImageType.Image2D,
                    Format = VContext.bmpColorFormat,
                    Extent = new Extent3D()
                    {
                        Width = (uint)texture.Width,
                        Height = (uint)texture.Height,
                        Depth = 1
                    },
                    MipLevels = 1,
                    ArrayLayers = 1,
                    Samples = SampleCountFlags.Count1,
                    Tiling = ImageTiling.Optimal,
                    Usage = ImageUsageFlags.Sampled | ImageUsageFlags.TransferDst,
                    SharingMode = SharingMode.Exclusive,
                    InitialLayout = ImageLayout.Undefined
                }
            );

            MemoryRequirements textureMemoryRequirements = VContext.Instance.device.GetImageMemoryRequirements(textureImage);

            uint memoryTypeIndex = Util.GetMemoryTypeIndex(textureMemoryRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocal);


            DeviceMemory textureImageMemory = VContext.Instance.device.AllocateMemory
            (
                new MemoryAllocateInfo()
                {
                    AllocationSize = textureMemoryRequirements.Size,
                    MemoryTypeIndex = memoryTypeIndex
                }
            );

            VContext.Instance.device.BindImageMemory(textureImage, textureImageMemory, 0);

            CommandBuffer commandBuffer = _commandBuffer.Value;

            commandBuffer.Begin(new CommandBufferBeginInfo());

            ImageSubresourceRange subresourceRange = new ImageSubresourceRange()
            {
                AspectMask = ImageAspectFlags.Color,
                BaseMipLevel = 0,
                LevelCount = 1,
                LayerCount = 1,
            };


            ImageMemoryBarrier undefinedToTranserDstBarrier = new ImageMemoryBarrier()
            {
                OldLayout = ImageLayout.Undefined,
                NewLayout = ImageLayout.TransferDstOptimal,
                Image = textureImage,
                SubresourceRange = subresourceRange,
                SrcAccessMask = 0,
                DstAccessMask = AccessFlags.TransferWrite
            };


            commandBuffer.CmdPipelineBarrier(
                PipelineStageFlags.AllCommands,
                PipelineStageFlags.AllCommands,
                0,
                null,
                null,
                undefinedToTranserDstBarrier);


            // Copy mip levels from staging buffer	
            commandBuffer.CmdCopyBufferToImage(
                stagingBuffer.Buffer,
                textureImage,
                ImageLayout.TransferDstOptimal,
                bufferCopyRegions.ToArray());


            // Change texture image layout to shader read after all mip levels have been copied
            ImageMemoryBarrier transferDstToShaderReadBarrier = new ImageMemoryBarrier()
            {
                OldLayout = ImageLayout.TransferDstOptimal,
                NewLayout = ImageLayout.ShaderReadOnlyOptimal,
                Image = textureImage,
                SubresourceRange = subresourceRange,
                SrcAccessMask = AccessFlags.TransferWrite,
                DstAccessMask = AccessFlags.ShaderRead
            };


            commandBuffer.CmdPipelineBarrier(
               PipelineStageFlags.AllCommands,
                PipelineStageFlags.AllCommands,
                0,
                null,
                null,
                transferDstToShaderReadBarrier);


            commandBuffer.End();

            // Create a fence to make sure that the copies have finished before continuing	
            Fence copyFence = VContext.Instance.device.CreateFence(new FenceCreateInfo());

            SubmitInfo submitInfo = new SubmitInfo()
            {
                CommandBuffers = new CommandBuffer[] { commandBuffer }
            };


            VContext.Instance.deviceQueue.Submit(submitInfo, copyFence);

            VContext.Instance.device.WaitForFences(new Fence[] { copyFence }, true, ulong.MaxValue);
            commandBuffer.Reset(CommandBufferResetFlags.ReleaseResources);

            VContext.Instance.device.DestroyFence(copyFence);


            VContext.Instance.device.FreeMemory(stagingBuffer.Memory);
            VContext.Instance.device.DestroyBuffer(stagingBuffer.Buffer);


            ImageView textureImageView = VContext.Instance.CreateColorImageView(textureImage);

            return textureImageView;
        }
    }
}

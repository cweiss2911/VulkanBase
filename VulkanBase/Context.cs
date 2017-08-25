using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vulkan;
using VulkanBase.SafeComfort;

namespace VulkanBase
{
    public unsafe static class Context
    {
        public const Format bmpColorFormat = Format.B8G8R8A8Unorm;
        public const Format depthFormat = Format.D16Unorm;

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private unsafe delegate Bool32 DebugReportCallbackDelegate(DebugReportFlagsExt flags, DebugReportObjectTypeExt objectType, ulong @object, IntPtr location, int messageCode, string layerPrefix, string message, IntPtr userData);

        public static Instance instance { get; private set; }
        public static PhysicalDevice physicalDevice { get; private set; }
        public static Device device { get; private set; }
        public static CommandPool commandPool { get; private set; }

        public static uint queueFamilyIndexWithGraphics { get; private set; }

        public static Queue deviceQueue { get; private set; }

        public static SurfaceKhr surface { get; private set; }
        public static uint SurfaceWidth { get; private set; }
        public static uint SurfaceHeight { get; private set; }

        private static Image depthTextureImage;
        public static ImageView depthTextureImageView { get; private set; }

        public static RenderPass renderPass { get; private set; }
        public static Framebuffer[] framebuffers { get; private set; }
        public static PipelineLayout PipelineLayout { get; set; }
        public static Pipeline StaticPipeline { get; set; }
        public static Pipeline AnimationPipeline { get; set; }
        public static GraphicsPipelineCreateInfo DefaultGraphicsPipelineCreateInfo { get; private set; }

        private static Image[] swapchainImages;

        private static DebugReportCallbackDelegate debugReport = DebugReport;
        public static DescriptorPool descriptorPool;
        public static DescriptorSetLayout textureDescriptorSetLayout;


        public static Sampler sampler;
        public static DescriptorSetLayout texelBufferDescriptorSetLayout;

        private static uint imageCount;
        public static SwapchainKhr swapChain;
        private static Format swapChainColorFormat;
        public static readonly BufferManager BufferManager = new BufferManager();

        public static float[] ClearColorValue { get; set; } = new float[] { 0, 0, 0, 0 };

        public static void Init(IntPtr windowHandle)
        {
            CreateInstance();
            physicalDevice = instance.EnumeratePhysicalDevices().First();
            CreateDevice();
            CreateCommandPool();

            if (windowHandle != IntPtr.Zero)
            {
                CreateSurface(windowHandle);
                depthTextureImageView = CreateDepthImageView();
                CreateRenderPass();
                CreateSwapChain();
                CreateFramebufferForSwapchainImages();
                CreateSampler();
            }

            CreateDefaultGraphicsPipelineCreateInfo();
        }

        private static void CreateDefaultGraphicsPipelineCreateInfo()
        {
            DefaultGraphicsPipelineCreateInfo = new GraphicsPipelineCreateInfo()
            {
                InputAssemblyState = new PipelineInputAssemblyStateCreateInfo()
                {
                    Topology = PrimitiveTopology.TriangleList,
                },
                ViewportState = new PipelineViewportStateCreateInfo()
                {
                    Viewports = new Viewport[]
                    {
                        new Viewport()
                        {
                            X = 0f,
                            Y = 0f,
                            Width = SurfaceWidth,
                            Height = SurfaceHeight,
                            MinDepth = 0.1f,
                            MaxDepth = 1f,
                        }
                    },
                    Scissors = new Rect2D[]
                    {
                        new Rect2D()
                        {
                            Offset = new Offset2D()
                            {
                                X = 0,
                                Y = 0,
                            },
                            Extent = new Extent2D()
                            {
                                Width = Context.SurfaceWidth,
                                Height = Context.SurfaceHeight,
                            }
                        }
                    }
                },
                RasterizationState = new PipelineRasterizationStateCreateInfo()
                {
                    DepthClampEnable = true,
                    RasterizerDiscardEnable = false,
                    PolygonMode = PolygonMode.Fill,
                    CullMode = CullModeFlags.None,
                    FrontFace = FrontFace.CounterClockwise,
                    DepthBiasEnable = false,
                    DepthBiasConstantFactor = 0f,
                    DepthBiasClamp = 0f,
                    DepthBiasSlopeFactor = 0f,
                    LineWidth = 1.0f,
                },
                DepthStencilState = new PipelineDepthStencilStateCreateInfo()
                {
                    DepthTestEnable = true,
                    DepthWriteEnable = true,
                    DepthCompareOp = CompareOp.LessOrEqual,
                    DepthBoundsTestEnable = false,
                    MinDepthBounds = 0,
                    MaxDepthBounds = 1,
                    StencilTestEnable = false,
                },
                ColorBlendState = new PipelineColorBlendStateCreateInfo()
                {
                    AttachmentCount = 1,
                    Attachments = new PipelineColorBlendAttachmentState[]
                    {
                        new PipelineColorBlendAttachmentState()
                        {
                            ColorWriteMask = ColorComponentFlags.A|ColorComponentFlags.R|ColorComponentFlags.G|ColorComponentFlags.B,
                            BlendEnable = false,

                        }
                    }
                },
                MultisampleState = new PipelineMultisampleStateCreateInfo()
                {
                    RasterizationSamples = SampleCountFlags.Count1,
                    SampleShadingEnable = true,
                    MinSampleShading = 0f
                }
            };
        }


        private static void CreateInstance()
        {
            var enabledLayerNames = new string[]
            {
                "VK_LAYER_LUNARG_core_validation",
            };

            var enabledExtensionNames = new[]
            {
                "VK_KHR_surface",
                "VK_KHR_win32_surface",
                "VK_EXT_debug_report",
            };



            instance = new Instance
            (
                new InstanceCreateInfo()
                {
                    ApplicationInfo = new ApplicationInfo()
                    {
                        EngineVersion = 1,
                        ApiVersion = Vulkan.Version.Make(1, 0, 24)

                    },

                    EnabledLayerNames = enabledLayerNames,
                    EnabledExtensionNames = enabledExtensionNames,
                }
            );
            instance.CreateDebugReportCallbackEXT
            (
                new DebugReportCallbackCreateInfoExt()
                {
                    /**/
                    Flags = DebugReportFlagsExt.Error | DebugReportFlagsExt.Warning,
                    /*/
                    Flags = (DebugReportFlagsExt)31,
                    /**/
                    PfnCallback = Marshal.GetFunctionPointerForDelegate(debugReport)
                }
            );
        }


        private static Bool32 DebugReport(DebugReportFlagsExt flags, DebugReportObjectTypeExt objectType, ulong @object, IntPtr location, int messageCode, string layerPrefix, string message, IntPtr userData)
        {
            Console.WriteLine($"{flags}: {message} ([{messageCode}] {layerPrefix})");
            return true;
        }


        private static void CreateDevice()
        {
            QueueFamilyProperties[] queueFamilyProperties = physicalDevice.GetQueueFamilyProperties();

            queueFamilyIndexWithGraphics = (uint)Array.FindIndex(physicalDevice.GetQueueFamilyProperties(), qfp => (qfp.QueueFlags & QueueFlags.Graphics) == QueueFlags.Graphics);

            var enabledExtensionNames = new[]
           {
                "VK_KHR_swapchain",
            };

            var enabledLayerNames = new string[]
            {
                "VK_LAYER_LUNARG_core_validation",
            };

            device = physicalDevice.CreateDevice
            (
                new DeviceCreateInfo()
                {
                    EnabledFeatures = new PhysicalDeviceFeatures()
                    {
                        DepthClamp = true
                    },
                    QueueCreateInfos = new DeviceQueueCreateInfo[]
                    {
                        new DeviceQueueCreateInfo()
                        {
                            QueueCount = 1,
                            QueueFamilyIndex = queueFamilyIndexWithGraphics,
                            QueuePriorities = new float[] {0.5f}
                        }
                    },
                    EnabledExtensionNames = enabledExtensionNames,
                    EnabledLayerNames = enabledLayerNames,
                }
            );


            deviceQueue = device.GetQueue(queueFamilyIndexWithGraphics, 0);
        }

        private static void CreateCommandPool()
        {
            commandPool = device.CreateCommandPool
            (
                new CommandPoolCreateInfo()
                {
                    QueueFamilyIndex = Context.queueFamilyIndexWithGraphics,
                    Flags = CommandPoolCreateFlags.ResetCommandBuffer
                }
            );
        }

        internal static void CreateSurface(IntPtr windowHandle)
        {
            if (!Vulkan.Windows.PhysicalDeviceExtension.GetWin32PresentationSupportKHR(physicalDevice, queueFamilyIndexWithGraphics))
            {
                throw new Exception($"queue family does not support win32 surface");
            }

            surface = Vulkan.Windows.InstanceExtension.CreateWin32SurfaceKHR
            (
                instance,
                new Vulkan.Windows.Win32SurfaceCreateInfoKhr()
                {
                    Hwnd = windowHandle,
                    Hinstance = System.Diagnostics.Process.GetCurrentProcess().Handle
                }
            );

            SurfaceWidth = physicalDevice.GetSurfaceCapabilitiesKHR(surface).CurrentExtent.Width;
            SurfaceHeight = physicalDevice.GetSurfaceCapabilitiesKHR(surface).CurrentExtent.Height;
        }

        public static ImageView CreateDepthImageView()
        {
            FormatProperties formatProperties = Context.physicalDevice.GetFormatProperties(depthFormat);

            ImageTiling it = ImageTiling.Linear;
            if ((formatProperties.LinearTilingFeatures & FormatFeatureFlags.DepthStencilAttachment) != FormatFeatureFlags.DepthStencilAttachment)
            {
                if ((formatProperties.OptimalTilingFeatures & FormatFeatureFlags.DepthStencilAttachment) == FormatFeatureFlags.DepthStencilAttachment)
                {
                    it = ImageTiling.Optimal;
                }
                else
                {
                    throw new Exception("Image format not supported");
                }

            }

            Image depthTextureImage = Context.device.CreateImage
            (
                new ImageCreateInfo()
                {
                    ImageType = ImageType.Image2D,
                    Format = depthFormat,
                    Extent = new Extent3D()
                    {
                        Width = (uint)Context.SurfaceWidth,
                        Height = (uint)Context.SurfaceHeight,
                        Depth = 1
                    },
                    MipLevels = 1,
                    ArrayLayers = 1,
                    Samples = SampleCountFlags.Count1,
                    InitialLayout = Vulkan.ImageLayout.Undefined,
                    QueueFamilyIndexCount = 0,
                    SharingMode = SharingMode.Exclusive,
                    Usage = ImageUsageFlags.DepthStencilAttachment,
                    Tiling = it,
                }
            );

            MemoryRequirements textureMemoryRequirements = Context.device.GetImageMemoryRequirements(depthTextureImage);

            uint memoryTypeIndex = Util.GetMemoryTypeIndex(textureMemoryRequirements.MemoryTypeBits, 0);

            DeviceMemory textureImageMemory = Context.device.AllocateMemory
            (
                 new MemoryAllocateInfo()
                 {
                     AllocationSize = textureMemoryRequirements.Size,
                     MemoryTypeIndex = memoryTypeIndex
                 }
            );

            device.BindImageMemory(depthTextureImage, textureImageMemory, 0);

            ImageView depthTextureImageView = device.CreateImageView
            (
                new ImageViewCreateInfo()
                {
                    Image = depthTextureImage,
                    ViewType = ImageViewType.View2D,
                    Format = depthFormat,
                    Components = new ComponentMapping()
                    {
                        R = ComponentSwizzle.R,
                        G = ComponentSwizzle.G,
                        B = ComponentSwizzle.B,
                        A = ComponentSwizzle.A,
                    },
                    SubresourceRange = new ImageSubresourceRange()
                    {
                        AspectMask = ImageAspectFlags.Depth,
                        BaseMipLevel = 0,
                        LevelCount = 1,
                        BaseArrayLayer = 0,
                        LayerCount = 1,
                    }
                }
            );

            return depthTextureImageView;
        }

        private static void CreateRenderPass()
        {
            renderPass = Context.device.CreateRenderPass
            (
                new RenderPassCreateInfo()
                {
                    AttachmentCount = 2,
                    Attachments = new AttachmentDescription[]
                    {
                        new AttachmentDescription()
                        {
                            Format = bmpColorFormat,
                            Samples = SampleCountFlags.Count1,
                            LoadOp = AttachmentLoadOp.Clear,
                            StoreOp = AttachmentStoreOp.Store,
                            StencilLoadOp = AttachmentLoadOp.DontCare,
                            StencilStoreOp = AttachmentStoreOp.DontCare,
                            InitialLayout = ImageLayout.Undefined,
                            FinalLayout = ImageLayout.PresentSrcKhr
                        },
                        new AttachmentDescription()
                        {
                            Format =  Context.depthFormat,
                            Samples = SampleCountFlags.Count1,
                            LoadOp = AttachmentLoadOp.Clear,
                            StoreOp = AttachmentStoreOp.Store,
                            StencilLoadOp = AttachmentLoadOp.Load,
                            StencilStoreOp = AttachmentStoreOp.Store,
                            InitialLayout = ImageLayout.Undefined,
                            FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
                        }
                    },
                    Subpasses = new SubpassDescription[]
                    {
                        new SubpassDescription()
                        {
                            PipelineBindPoint = PipelineBindPoint.Graphics,
                            InputAttachmentCount = 0,
                            ColorAttachmentCount = 1,
                            ColorAttachments = new AttachmentReference[]
                            {
                                new AttachmentReference()
                                {
                                    Attachment = 0,
                                    Layout = ImageLayout.ColorAttachmentOptimal
                                }
                            },
                            DepthStencilAttachment = new AttachmentReference()
                            {
                                Attachment = 1,
                                Layout = ImageLayout.DepthStencilAttachmentOptimal
                            },
                            PreserveAttachmentCount = 0
                        }
                    }
                }
            );

        }


        private static void CreateSwapChain()
        {
            SurfaceCapabilitiesKhr surfaceCapabilitiesKhr = Context.physicalDevice.GetSurfaceCapabilitiesKHR(Context.surface);

            imageCount = Math.Min(3, surfaceCapabilitiesKhr.MaxImageCount);


            SurfaceFormatKhr[] surfaceFormats = Context.physicalDevice.GetSurfaceFormatsKHR(Context.surface);

            swapChainColorFormat = Format.B8G8R8A8Snorm;
            if (surfaceFormats.Length != 1 || surfaceFormats[0].Format != Format.Undefined)
            {
                swapChainColorFormat = surfaceFormats[0].Format;
            }

            Bool32 supp = Context.physicalDevice.GetSurfaceSupportKHR(Context.queueFamilyIndexWithGraphics, Context.surface);
            PresentModeKhr[] presentModes = Context.physicalDevice.GetSurfacePresentModesKHR(Context.surface);
            swapChain = Context.device.CreateSwapchainKHR
            (
                new SwapchainCreateInfoKhr()
                {
                    Surface = Context.surface,
                    MinImageCount = imageCount,
                    ImageFormat = swapChainColorFormat,
                    ImageColorSpace = ColorSpaceKhr.SrgbNonlinear,
                    ImageExtent = new Extent2D()
                    {
                        Height = Context.SurfaceHeight,
                        Width = Context.SurfaceWidth,
                    },
                    ImageArrayLayers = 1,
                    ImageUsage = ImageUsageFlags.ColorAttachment | ImageUsageFlags.TransferDst,
                    ImageSharingMode = SharingMode.Exclusive,
                    PreTransform = SurfaceTransformFlagsKhr.Identity,
                    CompositeAlpha = CompositeAlphaFlagsKhr.Opaque,
                    PresentMode = PresentModeKhr.Mailbox,
                    Clipped = false,
                }
            );
        }


        private static void CreateFramebufferForSwapchainImages()
        {
            swapchainImages = device.GetSwapchainImagesKHR(swapChain);

            framebuffers = new Framebuffer[imageCount];
            for (int i = 0; i < framebuffers.Length; i++)
            {
                ImageView swapchainImageView = CreateColorImageView(swapchainImages[i]);

                framebuffers[i] = device.CreateFramebuffer
                (
                    new FramebufferCreateInfo()
                    {
                        RenderPass = renderPass,
                        AttachmentCount = 2,
                        Attachments = new ImageView[] { swapchainImageView, depthTextureImageView },
                        Height = SurfaceHeight,
                        Width = SurfaceWidth,
                        Layers = 1
                    }
                );
            }
        }


        private static void CreateSampler()
        {
            Context.sampler = Context.device.CreateSampler
            (
                new SamplerCreateInfo()
                {
                    MagFilter = Filter.Linear,
                    MinFilter = Filter.Linear,
                    MipmapMode = SamplerMipmapMode.Linear,
                    AddressModeU = SamplerAddressMode.ClampToEdge,
                    AddressModeV = SamplerAddressMode.ClampToEdge,
                    AddressModeW = SamplerAddressMode.ClampToEdge,
                    MipLodBias = 0,
                    AnisotropyEnable = false,
                    MinLod = 0,
                    MaxLod = 5,
                    BorderColor = BorderColor.FloatTransparentBlack,
                    UnnormalizedCoordinates = false,
                }
            );
        }

        internal static void Destroy()
        {
            device.DestroyCommandPool(commandPool);
            device.Destroy();

            instance.Destroy();
        }

        public static CommandBuffer CreateCommandBuffer()
        {
            CommandBuffer commandBuffer = Context.device.AllocateCommandBuffers
            (
                new CommandBufferAllocateInfo()
                {
                    CommandPool = Context.commandPool,
                    CommandBufferCount = 1,
                    Level = CommandBufferLevel.Primary
                }
            ).First();

            return commandBuffer;
        }


        public static BufferWithMemory CreateBuffer(BufferUsageFlags usageFlags, DeviceSize size, void* data)
        {
            return CreateBuffer(usageFlags, size, new IntPtr(data));
        }

        public static BufferWithMemory CreateBuffer(BufferUsageFlags usageFlags, DeviceSize size, IntPtr data)
        {
            BufferWithMemory buffer = new BufferWithMemory();

            buffer.Size = (uint)size;

            buffer.Buffer = device.CreateBuffer
            (
                new BufferCreateInfo()
                {
                    Size = size,
                    Usage = usageFlags,
                    SharingMode = SharingMode.Exclusive,
                }
            );

            MemoryRequirements memoryRequirements = device.GetBufferMemoryRequirements(buffer.Buffer);
            uint memoryTypeIndex = Util.GetMemoryTypeIndex(memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.HostVisible);

            buffer.Memory = device.AllocateMemory
            (
                new MemoryAllocateInfo()
                {
                    AllocationSize = memoryRequirements.Size,
                    MemoryTypeIndex = memoryTypeIndex
                }
            );
            device.BindBufferMemory(buffer.Buffer, buffer.Memory, 0);

            if (data != IntPtr.Zero)
            {
                IntPtr mappedMemory = device.MapMemory(buffer.Memory, 0, memoryRequirements.Size);
                Util.CopyMemory(mappedMemory, data, (uint)memoryRequirements.Size);
                device.UnmapMemory(buffer.Memory);
            }

            return buffer;
        }

        public static BufferWithMemory CreateDeviceLocalBuffer(BufferUsageFlags usageFlags, DeviceSize size, IntPtr data)
        {
            BufferWithMemory buffer = new BufferWithMemory();

            buffer.Size = (uint)size;

            buffer.Buffer = device.CreateBuffer
            (
                new BufferCreateInfo()
                {
                    Size = size,
                    Usage = BufferUsageFlags.TransferDst | usageFlags,
                    SharingMode = SharingMode.Exclusive,
                }
            );

            MemoryRequirements memoryRequirements = device.GetBufferMemoryRequirements(buffer.Buffer);
            uint memoryTypeIndex = Util.GetMemoryTypeIndex(memoryRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocal);

            buffer.Memory = device.AllocateMemory
            (
                new MemoryAllocateInfo()
                {
                    AllocationSize = memoryRequirements.Size,
                    MemoryTypeIndex = memoryTypeIndex
                }
            );
            device.BindBufferMemory(buffer.Buffer, buffer.Memory, 0);


            if (data != IntPtr.Zero)
            {
                CopyDataOntoDeviceLocalBuffer(buffer, size, data);
            }
            return buffer;
        }

        public static void CopyDataOntoDeviceLocalBuffer(BufferWithMemory targetBuffer, DeviceSize size, IntPtr data)
        {
            BufferWithMemory stagingBuffer = Context.CreateBuffer(BufferUsageFlags.TransferSrc, size, data);

            List<BufferCopy> bufferCopyRegions = new List<BufferCopy>();
            DeviceSize offset = 0;
            for (uint i = 0; i < 1; i++)
            {
                BufferCopy bufferCopyRegion = new BufferCopy()
                {
                    SrcOffset = 0,
                    DstOffset = 0,
                    Size = size
                };


                bufferCopyRegions.Add(bufferCopyRegion);
            }

            CommandBuffer commandBuffer = CreateCommandBuffer();

            commandBuffer.Begin(new CommandBufferBeginInfo());

            commandBuffer.CmdCopyBuffer(stagingBuffer.Buffer, targetBuffer.Buffer, bufferCopyRegions.ToArray());

            commandBuffer.End();


            Fence copyFence = Context.device.CreateFence(new FenceCreateInfo());

            SubmitInfo submitInfo = new SubmitInfo()
            {
                CommandBuffers = new CommandBuffer[] { commandBuffer }
            };


            Context.deviceQueue.Submit(submitInfo, copyFence);

            Context.device.WaitForFence(copyFence, true, ulong.MaxValue);

            Context.device.DestroyFence(copyFence);


            Context.device.FreeMemory(stagingBuffer.Memory);
            Context.device.DestroyBuffer(stagingBuffer.Buffer);

            Context.device.FreeCommandBuffer(commandPool, commandBuffer);
        }





        #region Comfortzone
        public static void InitializeDescriptorPool(uint maxSets, List<Tuple<uint, DescriptorType>> poolSizes)
        {
            DescriptorPoolSize[] descriptorPoolSizes =
                poolSizes.Select(
                    ps =>
                    new DescriptorPoolSize()
                    {
                        DescriptorCount = ps.Item1,
                        Type = ps.Item2,
                    }).ToArray();

            descriptorPool = device.CreateDescriptorPool
            (
                new DescriptorPoolCreateInfo()
                {
                    MaxSets = maxSets,
                    PoolSizes = descriptorPoolSizes
                }
            );
        }

        public static void BeginCommandBuffer(CommandBuffer commandBuffer, CommandBufferUsageFlags usageFlags = 0)
        {
            commandBuffer.Begin(
                new CommandBufferBeginInfo()
                {
                    Flags = usageFlags
                }
            );
        }

        public static void BeginRenderPass(CommandBuffer commandBuffer, uint imageIndex)
        {
            BeginRenderPass(commandBuffer, Context.framebuffers[imageIndex]);
        }

        public static void BeginRenderPass(CommandBuffer commandBuffer, Framebuffer framebuffer)
        {
            commandBuffer.CmdBeginRenderPass
            (
                new RenderPassBeginInfo()
                {
                    RenderPass = Context.renderPass,
                    Framebuffer = framebuffer,
                    RenderArea = new Rect2D()
                    {
                        Offset = new Offset2D()
                        {
                            X = 0,
                            Y = 0,
                        },
                        Extent = new Extent2D()
                        {
                            Width = Context.SurfaceWidth,
                            Height = Context.SurfaceHeight,
                        }
                    },
                    ClearValues = new ClearValue[]
                        {
                            new ClearValue()
                            {
                                Color =new ClearColorValue(ClearColorValue),
                            },
                            new ClearValue()
                            {
                                DepthStencil = new ClearDepthStencilValue()
                                {
                                    Depth = 1f,
                                    Stencil = 0,
                                }
                            }
                        }
                },
                SubpassContents.Inline
            );
        }

        public static void SubmitCommandBuffer(CommandBuffer commandBuffer, Fence fence = null)
        {
            deviceQueue.Submit(
                new SubmitInfo()
                {
                    CommandBuffers = new CommandBuffer[]
                    {
                        commandBuffer
                    },
                },
                fence
            );
        }

        public static void PresentSwapchain(uint imageIndex)
        {
            deviceQueue.PresentKHR
            (
                new PresentInfoKhr()
                {
                    Swapchains = new SwapchainKhr[] { swapChain },
                    ImageIndices = new uint[] { imageIndex },
                }
            );
        }

        public static ImageView CreateColorImageView(Image image)
        {
            return device.CreateImageView
            (
                new ImageViewCreateInfo()
                {
                    Image = image,
                    ViewType = ImageViewType.View2D,
                    Format = bmpColorFormat,
                    Components = new ComponentMapping()
                    {
                        R = ComponentSwizzle.R,
                        G = ComponentSwizzle.G,
                        B = ComponentSwizzle.B,
                        A = ComponentSwizzle.A,
                    },
                    SubresourceRange = new ImageSubresourceRange()
                    {
                        AspectMask = ImageAspectFlags.Color,
                        BaseMipLevel = 0,
                        LevelCount = 1,
                        BaseArrayLayer = 0,
                        LayerCount = 1,
                    }
                }
            );
        }
        #endregion

    }
}

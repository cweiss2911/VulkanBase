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
    public unsafe class VContext
    {
        private static VContext contextInstance;

        public static VContext Instance
        {
            get
            {
                return contextInstance;
            }
            set
            {
                contextInstance = value;
            }
        }

        public const Format bmpColorFormat = Format.B8G8R8A8Unorm;
        public const Format depthFormat = Format.D16Unorm;

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        protected unsafe delegate Bool32 DebugReportCallbackDelegate(DebugReportFlagsExt flags, DebugReportObjectTypeExt objectType, ulong @object, IntPtr location, int messageCode, string layerPrefix, string message, IntPtr userData);

        public Instance instance { get; protected set; }
        public PhysicalDevice physicalDevice { get; protected set; }
        public Device device { get; protected set; }
        public CommandPool commandPool { get; protected set; }

        public uint UsedQueueFamilyIndex { get; protected set; }

        public Queue deviceQueue { get; protected set; }

        public SurfaceKhr surface { get; protected set; }
        public uint SurfaceWidth { get; protected set; }
        public uint SurfaceHeight { get; protected set; }

        public ImageView DepthTextureImageView { get; protected set; }

        public RenderPass RenderPass { get; protected set; }
        public Framebuffer[] Framebuffers { get; protected set; }
        public ImageView[] SwapchainImageViews { get; protected set; }

        public PipelineLayout PipelineLayout { get; set; }
        public Pipeline StaticPipeline { get; set; }
        public Pipeline AnimationPipeline { get; set; }
        public GraphicsPipelineCreateInfo DefaultGraphicsPipelineCreateInfo { get; protected set; }

        public Image[] SwapchainImages { get; protected set; }
        

        protected DebugReportCallbackDelegate debugReport = DebugReport;
        public DescriptorPool descriptorPool;
        public DescriptorSetLayout textureDescriptorSetLayout;


        public Sampler sampler;
        public DescriptorSetLayout texelBufferDescriptorSetLayout;

        protected uint imageCount;
        public SwapchainKhr swapChain;
        protected Format swapChainColorFormat;

        public BufferManager BufferManager { get; private set; } = new BufferManager();
        public BufferviewManager BufferviewManager { get; private set; } = new BufferviewManager();



        public List<Action<DebugReportFlagsExt, string, int, string>> DebugCallbacks { get; private set; } = new List<Action<DebugReportFlagsExt, string, int, string>>();
        public float[] ClearColorValue { get; set; } = new float[] { 0, 0, 0, 0 };

        public VContext()
        {
            
        }

        public void Init(IntPtr windowHandle)
        {
            CreateInstance();
            physicalDevice = instance.EnumeratePhysicalDevices().First();
            CreateDevice();
            CreateCommandPool();

            if (windowHandle != IntPtr.Zero)
            {
                CreateSurface(windowHandle);
                DepthTextureImageView = CreateDepthImageView();
                CreateRenderPass();
                CreateSwapChain();
                CreateFramebufferForSwapchainImages();
                CreateSampler();
            }

            CreateDefaultGraphicsPipelineCreateInfo();
        }

        private void CreateDefaultGraphicsPipelineCreateInfo()
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
                                Width = SurfaceWidth,
                                Height = SurfaceHeight,
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


        private void CreateInstance()
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

            DebugCallbacks.Add(LogToInternalConsole);

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
            for (int i = 0; i < Instance.DebugCallbacks.Count; i++)
            {
                Instance.DebugCallbacks[i](flags, message, messageCode, layerPrefix);
            }
            return true;
        }

        private void LogToInternalConsole(DebugReportFlagsExt flags, string message, int messageCode, string layerPrefix)
        {
            Console.WriteLine($"{flags}: {message} ([{messageCode}] {layerPrefix})");
        }

        protected virtual void CreateDevice()
        {
            QueueFamilyProperties[] queueFamilyProperties = physicalDevice.GetQueueFamilyProperties();

            UsedQueueFamilyIndex = (uint)Array.FindIndex(physicalDevice.GetQueueFamilyProperties(), qfp => (qfp.QueueFlags & QueueFlags.Graphics) == QueueFlags.Graphics);

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
                            QueueFamilyIndex = UsedQueueFamilyIndex,
                            QueuePriorities = new float[] {0.5f}
                        }
                    },
                    EnabledExtensionNames = enabledExtensionNames,
                    EnabledLayerNames = enabledLayerNames,
                }
            );


            deviceQueue = device.GetQueue(UsedQueueFamilyIndex, 0);
        }

        protected virtual void CreateCommandPool()
        {
            commandPool = device.CreateCommandPool
            (
                new CommandPoolCreateInfo()
                {
                    QueueFamilyIndex = UsedQueueFamilyIndex,
                    Flags = CommandPoolCreateFlags.ResetCommandBuffer 
                }
            );
        }

        internal void CreateSurface(IntPtr windowHandle)
        {
            if (!Vulkan.Windows.PhysicalDeviceExtension.GetWin32PresentationSupportKHR(physicalDevice, UsedQueueFamilyIndex))
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

        public ImageView CreateDepthImageView()
        {
            FormatProperties formatProperties = physicalDevice.GetFormatProperties(depthFormat);

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

            Image depthTextureImage = device.CreateImage
            (
                new ImageCreateInfo()
                {
                    ImageType = ImageType.Image2D,
                    Format = depthFormat,
                    Extent = new Extent3D()
                    {
                        Width = (uint)SurfaceWidth,
                        Height = (uint)SurfaceHeight,
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

            MemoryRequirements textureMemoryRequirements = device.GetImageMemoryRequirements(depthTextureImage);

            uint memoryTypeIndex = Util.GetMemoryTypeIndex(textureMemoryRequirements.MemoryTypeBits, 0);

            DeviceMemory textureImageMemory = device.AllocateMemory
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

        private void CreateRenderPass()
        {
            RenderPass = device.CreateRenderPass
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
                            Format =  depthFormat,
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


        private void CreateSwapChain()
        {
            SurfaceCapabilitiesKhr surfaceCapabilitiesKhr = physicalDevice.GetSurfaceCapabilitiesKHR(surface);

            imageCount = Math.Min(3, surfaceCapabilitiesKhr.MaxImageCount);


            SurfaceFormatKhr[] surfaceFormats = physicalDevice.GetSurfaceFormatsKHR(surface);

            swapChainColorFormat = Format.B8G8R8A8Snorm;
            if (surfaceFormats.Length != 1 || surfaceFormats[0].Format != Format.Undefined)
            {
                swapChainColorFormat = surfaceFormats[0].Format;
            }

            Bool32 supp = physicalDevice.GetSurfaceSupportKHR(UsedQueueFamilyIndex, surface);
            PresentModeKhr[] presentModes = physicalDevice.GetSurfacePresentModesKHR(surface);
            swapChain = device.CreateSwapchainKHR
            (
                new SwapchainCreateInfoKhr()
                {
                    Surface = surface,
                    MinImageCount = imageCount,
                    ImageFormat = swapChainColorFormat,
                    ImageColorSpace = ColorSpaceKhr.SrgbNonlinear,
                    ImageExtent = new Extent2D()
                    {
                        Height = SurfaceHeight,
                        Width = SurfaceWidth,
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


        private void CreateFramebufferForSwapchainImages()
        {
            SwapchainImages = device.GetSwapchainImagesKHR(swapChain);

            Framebuffers = new Framebuffer[imageCount];
            SwapchainImageViews = new ImageView[imageCount];
            for (int i = 0; i < Framebuffers.Length; i++)
            {
                SwapchainImageViews[i] = CreateColorImageView(SwapchainImages[i]);                
                Framebuffers[i] = device.CreateFramebuffer
                (
                    new FramebufferCreateInfo()
                    {
                        RenderPass = RenderPass,
                        AttachmentCount = 2,
                        Attachments = new ImageView[] { SwapchainImageViews[i], DepthTextureImageView },
                        Height = SurfaceHeight,
                        Width = SurfaceWidth,
                        Layers = 1
                    }
                );
            }
        }


        private void CreateSampler()
        {
            sampler = device.CreateSampler
            (
                new SamplerCreateInfo()
                {
                    MagFilter = Filter.Nearest,
                    MinFilter = Filter.Nearest,
                    MipmapMode = SamplerMipmapMode.Nearest,
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

        internal void Destroy()
        {
            device.DestroyCommandPool(commandPool);
            device.Destroy();

            instance.Destroy();
        }

        public CommandBuffer CreateCommandBuffer()
        {
            CommandBuffer commandBuffer = device.AllocateCommandBuffers
            (
                new CommandBufferAllocateInfo()
                {
                    CommandPool = commandPool,
                    CommandBufferCount = 1,
                    Level = CommandBufferLevel.Primary                    
                }
            ).First();

            return commandBuffer;
        }

        public BufferWithMemory CreateBuffer(BufferUsageFlags usageFlags, DeviceSize size, void* data)
        {
            return CreateBuffer(usageFlags, size, new IntPtr(data));
        }

        public BufferWithMemory CreateBuffer(BufferUsageFlags usageFlags, DeviceSize size, IntPtr data)
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


        public BufferWithMemory CreateDeviceLocalBuffer(BufferUsageFlags usageFlags, DeviceSize size, IntPtr data)
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

        public void CopyDataOntoDeviceLocalBuffer(BufferWithMemory targetBuffer, DeviceSize size, IntPtr data)
        {
            BufferWithMemory stagingBuffer = CreateBuffer(BufferUsageFlags.TransferSrc, size, data);

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


            Fence copyFence = device.CreateFence(new FenceCreateInfo());

            SubmitInfo submitInfo = new SubmitInfo()
            {
                CommandBuffers = new CommandBuffer[] { commandBuffer }
            };


            deviceQueue.Submit(submitInfo, copyFence);

            device.WaitForFence(copyFence, true, ulong.MaxValue);

            device.DestroyFence(copyFence);


            device.FreeMemory(stagingBuffer.Memory);
            device.DestroyBuffer(stagingBuffer.Buffer);

            device.FreeCommandBuffer(commandPool, commandBuffer);
        }

        #region Comfortzone

        [System.Obsolete]
        public void InitializeDescriptorPool(uint maxSets, List<Tuple<uint, DescriptorType>> poolSizes)
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

        public void InitializeDescriptorPool(uint maxSets, params Tuple<uint, DescriptorType>[] poolSizes)
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
                    PoolSizes = descriptorPoolSizes,
                    Flags = DescriptorPoolCreateFlags.FreeDescriptorSet,
                }
            );
        }

        public void BeginCommandBuffer(CommandBuffer commandBuffer, CommandBufferUsageFlags usageFlags = 0)
        {
            commandBuffer.Begin(
                new CommandBufferBeginInfo()
                {
                    Flags = usageFlags
                }
            );
        }

        public void BeginRenderPass(CommandBuffer commandBuffer, uint imageIndex)
        {
            BeginRenderPass(commandBuffer, Framebuffers[imageIndex]);
        }

        public void BeginRenderPass(CommandBuffer commandBuffer, Framebuffer framebuffer)
        {
            commandBuffer.CmdBeginRenderPass
            (
                new RenderPassBeginInfo()
                {
                    RenderPass = RenderPass,
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
                            Width = SurfaceWidth,
                            Height = SurfaceHeight,
                        }
                    },
                    ClearValues = new ClearValue[]
                        {
                            new ClearValue()
                            {
                                Color = new ClearColorValue(ClearColorValue),
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

        public void SubmitCommandBuffer(CommandBuffer commandBuffer, Fence fence = null)
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

        public void PresentSwapchain(uint imageIndex)
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

        public ImageView CreateColorImageView(Image image)
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

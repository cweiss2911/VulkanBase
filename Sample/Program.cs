using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheToolbox;
using Vulkan;
using VulkanBase;
using VulkanBase.ShaderParsing;

namespace Sample
{
    static class Program
    {
        private static MainForm mainForm;
        private static Vulkan.Semaphore presentCompleteSemaphore;
        private static Thread runThread;
        private static CommandBuffer commandBuffer;
        private static GenericGraphicsPipeline graphicsPipeline;
        private static BufferWithMemory uniformBuffer;
        private static DescriptorSet uniformDescriptorSet;

        public static VContext Context { get; private set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            SetupVulkan();
            Load();

            Time.StartExisting();
            Run();

            Application.EnableVisualStyles();
            Application.Run(mainForm);
            runThread.Abort();
        }

        private static void SetupVulkan()
        {
            mainForm = new MainForm(/**/FormWindowState.Normal/*/FormWindowState.Maximized/**/);


            VContext.Instance = new VContext();
            Context = VContext.Instance;
            Context.Init(mainForm.Handle);


            graphicsPipeline = new GenericGraphicsPipeline(
                new List<ShaderObject>()
                {
                    new ShaderObject(@"..\..\BaseSampleShader\shader.vert"),
                    new ShaderObject(@"..\..\BaseSampleShader\shader.frag"),
                }

            );

            Context.InitializeDescriptorPool(
                1,
                new Tuple<uint, DescriptorType>(1, DescriptorType.UniformBuffer)
            );

            SetupUniformMatrices();
            commandBuffer = Context.CreateCommandBuffer();
        }

        private static void Load()
        {
            // Load stuff here
        }


        private static void SetupUniformMatrices()
        {
            Matrix4 projectionMatrix =
                new Matrix4(
                    1.0f, 0.0f, 0.0f, 0.0f,
                    0.0f, -1.0f, 0.0f, 0.0f,
                    0.0f, 0.0f, 0.5f, 0.0f,
                    0.0f, 0.0f, 0.5f, 1.0f)
                * Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Context.SurfaceWidth / (float)Context.SurfaceHeight, 0.1f, 100.0f);


            VulkanBase.Camera.Position = new Vector3(0.0f, 10f, 0.0f);
            VulkanBase.Camera.Rotation = new Vector3(0, (float)Math.PI / 2f, 0);

            Matrix4 viewMatrix = VulkanBase.Camera.CalculateViewMatrix();


            uint bufferSize = graphicsPipeline.ShaderUniformSets[0].GetSize();

            Matrix4[] matrices = new Matrix4[]
            {
                projectionMatrix,
                viewMatrix
            };

            uniformBuffer = Context.BufferManager.CreateBuffer(BufferUsageFlags.UniformBuffer, bufferSize, matrices);


            uniformDescriptorSet = graphicsPipeline.CreateDescriptorSet(0, 0, uniformBuffer);
        }


        private static void Run()
        {
            presentCompleteSemaphore = Context.device.CreateSemaphore(new SemaphoreCreateInfo());

            runThread = new Thread(() =>
            {
                while (true)
                {
                    Update(Time.DifferenceSinceLastCall());

                    Render();
                }

            });

            runThread.Start();
        }



        private static void Update(long time)
        {
            // Update stuff here
        }

        private static void Render()
        {

            uint imageIndex = Context.device.AcquireNextImageKHR(Context.swapChain, ulong.MaxValue, presentCompleteSemaphore);


            Context.BeginCommandBuffer(commandBuffer, CommandBufferUsageFlags.RenderPassContinue);

            Context.BeginRenderPass(commandBuffer, imageIndex);

            commandBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, graphicsPipeline.Pipeline);
            commandBuffer.CmdBindDescriptorSet(PipelineBindPoint.Graphics, graphicsPipeline.PipelineLayout, 0, uniformDescriptorSet, null);

            //Render stuff here

            commandBuffer.CmdEndRenderPass();

            commandBuffer.End();


            Context.deviceQueue.Submit
            (
                new SubmitInfo()
                {
                    CommandBuffers = new CommandBuffer[] { commandBuffer },
                    WaitSemaphores = new Vulkan.Semaphore[] { presentCompleteSemaphore },
                    WaitDstStageMask = new PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutput },

                }
            );

            Context.device.WaitIdle();
            commandBuffer.Reset();


            Context.deviceQueue.PresentKHR
            (
                new PresentInfoKhr()
                {
                    Swapchains = new SwapchainKhr[] { Context.swapChain },
                    ImageIndices = new uint[] { imageIndex },
                }
            );
        }
    }
}
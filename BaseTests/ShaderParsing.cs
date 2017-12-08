using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;
using VulkanBase;
using Vulkan;
using VulkanBase.ShaderParsing;
using System.Collections.Generic;

namespace BaseTests
{
    [TestClass]
    public class ShaderParsing
    {
        private Form mainForm;

        public VContext Context { get; private set; }

        public ShaderParsing()
        {
            mainForm = new Form()
            {
                FormBorderStyle = FormBorderStyle.None,
                /**/
                WindowState = FormWindowState.Normal,
                /*/
                WindowState = FormWindowState.Maximized,
                /**/
                Width = 1024,
                Height = 768,
            };

            mainForm.KeyDown += (object sender, KeyEventArgs e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    mainForm.Close();
                }
            };




            VContext.Instance = new VContext();
            Context = VContext.Instance;
            Context.Init(mainForm.Handle);

            Context.InitializeDescriptorPool(
                56,
                new Tuple<uint, DescriptorType>(1, DescriptorType.UniformBuffer),
                new Tuple<uint, DescriptorType>(50, DescriptorType.CombinedImageSampler),
                new Tuple<uint, DescriptorType>(1, DescriptorType.StorageImage),
                new Tuple<uint, DescriptorType>(1, DescriptorType.StorageBuffer)
            );
        }



            

        [TestMethod]
        public void AnimationShader()
        {
            GenericGraphicsPipeline graphicsPipeline = new GenericGraphicsPipeline(
                new List<ShaderObject>()
                {
                     new ShaderObject(@"..\..\animationshader.vert"),
                     new ShaderObject(@"..\..\shader.frag"),
                }
            );
        }

        [TestMethod]
        public void Embedded()
        {

            new EmbeddedShaderObject("VulkanBase.Text.Shader.text.frag");
            
        }
    }
}

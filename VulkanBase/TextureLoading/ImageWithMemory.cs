using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace VulkanBase.TextureLoading
{
    public class ImageWithMemory
    {
        public Image Image { get; set; }
        public ImageView ImageView { get; set; }        
        public DeviceMemory Memory { get; internal set; }

        public void Destroy()
        {
            VContext.Instance.device.DestroyImageView(ImageView);
            VContext.Instance.device.DestroyImage(Image);
            VContext.Instance.device.FreeMemory(Memory);
        }
    }
}

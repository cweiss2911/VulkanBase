using System.Collections.Generic;
using System.Drawing;

namespace VulkanBase.Collada
{
    public static class TextureLibrary
    {
        private static Dictionary<string, Bitmap> imageLibrary = new Dictionary<string, Bitmap>();
        
        public static Bitmap ObtainImage(string path)
        {
            if (!ImageExists(path))
            {
                AddImage(path, new Bitmap(path));
            }

            return imageLibrary[path];
        }

        public static bool ImageExists(string path)
        {
            return imageLibrary.ContainsKey(path);
        }

        public static void AddImage(string path, Bitmap bitmap)
        {
            imageLibrary.Add(path, bitmap);
        }
    }
}
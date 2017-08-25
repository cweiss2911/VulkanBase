using System.Collections.Generic;
using System.Drawing;

namespace VulkanBase.Collada
{
    public static class TextureLibrary
    {
        private static Dictionary<string, Bitmap> imageLibrary = new Dictionary<string, Bitmap>();

        //private static Dictionary<TextureMinFilter, Dictionary<TextureMagFilter, Dictionary<Bitmap, int>>> theLibrary = new Dictionary<TextureMinFilter, Dictionary<TextureMagFilter, Dictionary<Bitmap, int>>>();

            /*
        public static int ObtainTextureId(TextureMinFilter textureMinFilter, TextureMagFilter textureMagFilter, Bitmap texture)
        {
            int textureId = 0;


            if (theLibrary.ContainsKey(textureMinFilter))
            {
                if (theLibrary[textureMinFilter].ContainsKey(textureMagFilter))
                {
                    if (theLibrary[textureMinFilter][textureMagFilter].ContainsKey(texture))
                    {
                        textureId = theLibrary[textureMinFilter][textureMagFilter][texture];
                    }
                    else
                    {
                        textureId = GenerateTextureId(textureMinFilter, textureMagFilter, texture);

                        theLibrary[textureMinFilter][textureMagFilter].Add(texture, textureId);
                    }
                }
                else
                {
                    textureId = GenerateTextureId(textureMinFilter, textureMagFilter, texture);

                    theLibrary[textureMinFilter].Add(textureMagFilter, new Dictionary<Bitmap, int>());
                    theLibrary[textureMinFilter][textureMagFilter].Add(texture, textureId);
                }
            }
            else
            {
                textureId = GenerateTextureId(textureMinFilter, textureMagFilter, texture);

                theLibrary.Add(textureMinFilter, new Dictionary<TextureMagFilter, Dictionary<Bitmap, int>>());
                theLibrary[textureMinFilter].Add(textureMagFilter, new Dictionary<Bitmap, int>());
                theLibrary[textureMinFilter][textureMagFilter].Add(texture, textureId);
            }

            return textureId;
        }
        */
/*
        private static int GenerateTextureId(TextureMinFilter textureMinFilter, TextureMagFilter textureMagFilter, Bitmap texture)
        {
            int textureId = GL.GenTexture();


            GL.BindTexture(TextureTarget.Texture2D, textureId);

            System.Drawing.Imaging.BitmapData data = texture.LockBits(new System.Drawing.Rectangle(0, 0, texture.Width, texture.Height),
                                                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            texture.UnlockBits(data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)textureMinFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)textureMagFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.BindTexture(TextureTarget.Texture2D, 0);

            return textureId;
        }
        */
        public static Bitmap ObtainImage(string path)
        {
            if (!imageLibrary.ContainsKey(path))
            {
                imageLibrary.Add(path, new Bitmap(path));
            }

            return imageLibrary[path];
        }
    }
}
namespace VulkanBase.Collada
{
    public class Texture
    {
        public int Id { get; set; }
        /*public TextureTarget TextureTarget { get; set; }
        public TextureUnit TextureUnit { get; set; }*/

        public Texture(int id)//, TextureTarget textureTarget, TextureUnit textureUnit)
        {
            Id = id;
            /*    TextureTarget = textureTarget;
                TextureUnit = textureUnit;*/
        }

        public void Bind()
        {
            /*
            GL.ActiveTexture(TextureUnit);
            GL.BindTexture(TextureTarget, Id);*/
        }

        public void Unbind()
        {
            /*
            GL.ActiveTexture(TextureUnit);
            GL.BindTexture(TextureTarget, 0);*/
        }

    }
}
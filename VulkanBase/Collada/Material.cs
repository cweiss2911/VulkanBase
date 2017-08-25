namespace VulkanBase.Collada
{
    public class Material
    {
        public bool IsSpecular { get; set; }
        public Vector3 SpecularColor { get; set; }
        public int Shininess { get; set; }
        public bool HasNormalMapping { get; set; }

        public Material(Vector3 specularColor, int shininess)
        {
            SpecularColor = specularColor;
            Shininess = shininess;
            IsSpecular = true;
        }

        public Material()
            : this(Vector3.Zero, 0)
        {
            IsSpecular = false;
        }
    }
}
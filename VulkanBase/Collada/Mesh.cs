using System.Collections.Generic;
using System.Drawing;

namespace VulkanBase.Collada
{
    public class Mesh
    {
        public string Name { get; set; }

        public Buffer<Vector3> VertexBuffer { get; set; } = new Buffer<Vector3>();
        public Buffer<Vector2> TextureCoordinateBuffer { get; set; } = new Buffer<Vector2>();
        public Buffer<Vector3> NormalBuffer { get; set; } = new Buffer<Vector3>();
        public Buffer<Vector3> TangentBuffer { get; set; } = new Buffer<Vector3>();
        public Buffer<Vector3> BitangentBuffer { get; set; } = new Buffer<Vector3>();
        public TextureManager TextureManager { get; set; } = new TextureManager();
        public Material Material { get; set; } = new Material();
        
        public Matrix4 BindShapeMatrix;

        public Dictionary<int, Joint[]> JointDict = new Dictionary<int, Joint[]>();
        public Dictionary<int, float[]> WeighDict = new Dictionary<int, float[]>();
        public List<Bitmap> Bitmaps = new List<Bitmap>();
        
        public Mesh()
        {
        }

        public Mesh(Mesh mesh)
        {
            Name = mesh.Name;

            VertexBuffer.Data = mesh.VertexBuffer.Data;
            TextureCoordinateBuffer.Data = mesh.TextureCoordinateBuffer.Data;
            NormalBuffer.Data = mesh.NormalBuffer.Data;
            TangentBuffer.Data = mesh.TangentBuffer.Data;
            BitangentBuffer.Data = mesh.BitangentBuffer.Data;

            TextureManager = mesh.TextureManager;
            Material = mesh.Material;

            BindShapeMatrix = mesh.BindShapeMatrix;

            JointDict = mesh.JointDict;
            WeighDict = mesh.WeighDict;

            Bitmaps = mesh.Bitmaps;
        }


    
        public override string ToString()
        {
            return Name;
        }


    }
}
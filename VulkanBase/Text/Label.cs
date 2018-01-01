using System;
using System.Collections.Generic;
using Vulkan;
using VulkanBase;
using VulkanBase.SafeComfort.VertexInputBinding;
using VulkanBase.ShaderParsing;
using VulkanBase.Utility;

namespace VulkanBase.Text
{
    public class Label
    {
        private string _text;
        private Font _font;
        private Matrix4 _modelMatrix;
        private float _totalWidth;

        private int _vertexCount;
        private MeshInputBinding _meshInputBinding = new MeshInputBinding();


        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                BuildLabel();
            }
        }

        private Vector2 _position;
        public Vector2 Position
        {
            get { return _position; }
            set
            {
                _position = value;
                _modelMatrix = Matrix4.CreateTranslation(new Vector3(_position.X, _position.Y, 0));
            }
        }


        public Label(string text, Vector2 position, Font font)
        {
            _font = font;
            Position = position;
            Text = text;
        }

        public Label(string text, Vector2 position) : this(text, position, new Font(Properties.Resources.Courier, Properties.Resources.CourierCharacterWidth))
        {
        }



        private void BuildLabel()
        {
            float FontHeight = 64f / VContext.Instance.SurfaceHeight;
            const float TextureFontSize = 1f / 16f;

            _meshInputBinding.Clear();

            _totalWidth = 0f;
            List<Vector3> vertexList = new List<Vector3>();
            List<Vector2> texCoordList = new List<Vector2>();


            Vector2 UpperLeft = new Vector2(-0f, 0f);
            Vector2 LowerRight = new Vector2(-0f, FontHeight);

            Vector2 Start;
            for (int i = 0; i < _text.Length; i++)
            {
                Start = new Vector2(LowerRight.X, UpperLeft.Y);

                int ascii = (int)_text[i];
                float width = (float)_font.CharWidth[ascii];
                _totalWidth += 2 * width / VContext.Instance.SurfaceWidth;
                LowerRight = new Vector2(Start.X + 2 * width / VContext.Instance.SurfaceWidth, LowerRight.Y);

                RectangleMaker.AddRectangleToList(vertexList, Start.X, LowerRight.X, Start.Y, LowerRight.Y);

                float xcoord = (ascii % 16) / 16f;
                float ycoord = (ascii / 16) / 16f;

                RectangleMaker.AddRectangleToList(texCoordList, xcoord, xcoord + TextureFontSize * width / 32, ycoord, ycoord + TextureFontSize);
            }

            _vertexCount = vertexList.Count;


            _meshInputBinding.AddInputBinding(new InputBindingV3(BufferUsageFlags.VertexBuffer, vertexList, 0));
            _meshInputBinding.AddInputBinding(new InputBindingV2(BufferUsageFlags.VertexBuffer, texCoordList, 1));
        }

        public void Render(CommandBuffer commandBuffer, GenericGraphicsPipeline textPipeline)
        {
            _meshInputBinding.Bind(commandBuffer);

            textPipeline.PushConstantManager.SetPushConstant(commandBuffer, "modelMatrix", _modelMatrix);

            commandBuffer.CmdDraw((uint)_vertexCount, 1, 0, 0);
        }
    }
}
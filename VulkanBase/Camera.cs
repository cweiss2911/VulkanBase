using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VulkanBase
{
    public static class Camera
    {        
        public static bool checkMove = true;

        public static Vector3 Position;        
        public static Vector3 Rotation;
        public static float Speed { get; set; }


        static Camera()
        {
            Position = new Vector3(4f, 4.5f, 10.0f);

            Rotation = new Vector3(0, (float)Math.PI / 12f, 0);
            //Rotation = new Vector3();
            Speed = 15f;
        }
        
        public static Matrix4 InitializeViewMatrix()
        {
            return
                Matrix4.CreateTranslation(-Position) *
                Matrix4.CreateRotationX(Rotation.Y * (float)(Math.Cos(Rotation.X))) *
                Matrix4.CreateRotationZ(Rotation.Y * (float)(Math.Sin(Rotation.X))) *
                Matrix4.CreateRotationY(Rotation.X);
        }
        /*
        public static void MouseMove(OpenTK.Input.MouseMoveEventArgs mouseMoveEventArgs)
        {
            activeViewHandler.HandleMouseMove(
                ref checkMove,
                mouseMoveEventArgs,
                new Point(
                    Game.GameWindow.Location.X + Game.GameWindow.Size.Width / 2,
                    Game.GameWindow.Location.Y + Game.GameWindow.Size.Height / 2));
        }*/
        /*
        public static void CenterMousePosition()
        {
            Cursor.Position =
            new Point(
                    Game.GameWindow.Location.X + Game.GameWindow.Size.Width / 2,
                    Game.GameWindow.Location.Y + Game.GameWindow.Size.Height / 2);
        }*/

            /*
        public static void HandleUp(double time)
        {
            activeViewHandler.HandleUp(time);
        }

        public static void HandleDown(double time)
        {
            activeViewHandler.HandleDown(time);
        }

        public static void HandleRight(double time)
        {
            activeViewHandler.HandleRight(time);
        }

        public static void HandleLeft(double time)
        {
            activeViewHandler.HandleLeft(time);
        }

        public static void Lerp()
        {
            activeViewHandler.Lerp();
        }

        */

        public static Matrix4 CalculateViewMatrix()
        {
            return
                Matrix4.CreateTranslation(-Camera.Position) *
                Matrix4.CreateRotationX(Camera.Rotation.Y * (float)(Math.Cos(Camera.Rotation.X))) *
                Matrix4.CreateRotationZ(Camera.Rotation.Y * (float)(Math.Sin(Camera.Rotation.X))) *
                Matrix4.CreateRotationY(Camera.Rotation.X);
        }
    }
}

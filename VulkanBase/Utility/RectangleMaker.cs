using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VulkanBase;

namespace VulkanBase.Utility
{
    public static class RectangleMaker
    {
        public static void AddRectangleToList(List<Vector3> list, float left, float right, float up, float down)
        {
            list.Add(new Vector3(left, up, 0f));
            list.Add(new Vector3(left, down, 0f));
            list.Add(new Vector3(right, down, 0f));
            list.Add(new Vector3(left, up, 0f));
            list.Add(new Vector3(right, down, 0f));
            list.Add(new Vector3(right, up, 0f));
        }

        public static void AddRectangleToList(List<Vector2> list, float left, float right, float up, float down)
        {
            list.Add(new Vector2(left, up));
            list.Add(new Vector2(left, down));
            list.Add(new Vector2(right, down));
            list.Add(new Vector2(left, up));
            list.Add(new Vector2(right, down));
            list.Add(new Vector2(right, up));
        }
    }
}

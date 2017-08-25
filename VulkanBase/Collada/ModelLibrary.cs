using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VulkanBase.Collada
{
    public static class ModelLibrary
    {
        private static Dictionary<string, Model> theLibrary = new Dictionary<string, Model>();


        private enum TextureType
        {
            Diffuse,
            Normal,
            AmbientOcclusion
        }

        public static Model ObtainModel(string modelPath)
        {
            Model model = null;
            if (theLibrary.ContainsKey(modelPath))
            {
                model = theLibrary[modelPath];
            }
            else
            {
                model = ColladaParser.ProcessXml(modelPath);
                theLibrary.Add(modelPath, model);
            }

            return model;
        }

    }
}

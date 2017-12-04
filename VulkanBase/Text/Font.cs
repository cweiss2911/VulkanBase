using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VulkanBase.Collada;

namespace VulkanBase.Text
{
    public class Font
    {
        public int[] CharWidth { get; private set; } = new int[256];
        public Bitmap Texture { get; private set; }

        public Font(Bitmap texture, string characterWidths)
        {
            Texture = texture;
            string[] line = characterWidths.Split('\n');
            ExtractCharWidths(line);
        }

        public Font(string fontPath)
        {
            Texture = TextureLibrary.ObtainImage(fontPath);

            string path = fontPath.Substring(0, fontPath.LastIndexOf(@"\") + 1);
            string fontName = fontPath.Substring(fontPath.LastIndexOf(@"\") + 1, fontPath.LastIndexOf('.') - fontPath.LastIndexOf(@"\") - 1);

            string widthFile = path + fontName + ".csv";
            if (File.Exists(widthFile))
            {
                string[] line = File.ReadAllLines(widthFile);
                ExtractCharWidths(line);
            }
            else
            {
                throw new Exception("No width file for " + fontPath);
            }
        }

        private void ExtractCharWidths(string[] line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                line[i] = line[i].Trim();
                string[] word = line[i].Split(' ');

                //Searching for Char 0 Base Width,10
                if (word.Length >= 3 && word[0] == "Char" && word[2] == "Base" && word[3].Length >= "Width,1".Length && word[3].Substring(0, "Width,".Length) == "Width,")
                {
                    int index = int.Parse(word[1]);
                    int width = int.Parse(word[3].Substring("Width,".Length));
                    CharWidth[index] = width;
                }
            }
        }


    }
}

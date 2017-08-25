using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vulkan;

namespace VulkanBase.ShaderParsing
{
    public class TypeConverter<T1, T2>
    {
        private Dictionary<T1, T2> _typeDictionary;

        public TypeConverter(Dictionary<T1, T2> typeDictionary)
        {
            _typeDictionary = typeDictionary;
        }

        public T2 Convert(T1 typeToConvert)
        {
            if (_typeDictionary.ContainsKey(typeToConvert))
            {
                return _typeDictionary[typeToConvert];
            }
            else
            {
                throw new Exception($"No value found for {typeToConvert.ToString()}. ({typeof(T1)} to {typeof(T2)})");
            }
        }

        public void AddPair(T1 key, T2 value)
        {
            _typeDictionary.Add(key, value);
        }
    }
}

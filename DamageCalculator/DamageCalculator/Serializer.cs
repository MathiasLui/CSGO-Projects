using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Damage_Calculator
{
    static class Serializer
    {
        public static void ToXml(object obj, string path)
        {
            var serializer = new XmlSerializer(obj.GetType());
            using(var writer = XmlWriter.Create(path))
            {
                serializer.Serialize(writer, obj);
            }
        }

        public static object FromXml(Type type, string pathToXml)
        {
            var serializer = new XmlSerializer(type);
            using (var reader = XmlReader.Create(pathToXml))
            {
                return serializer.Deserialize(reader);
            }
        }
    }
}

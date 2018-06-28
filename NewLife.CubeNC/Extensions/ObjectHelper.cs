using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using NewLife.Serialization;

namespace NewLife.CubeNC.Extensions
{
    public static class ObjectHelper
    {
        public static byte[] ToBytes(this Object obj)
        {
            if (obj == null) return null;

            using (var ms = new MemoryStream())
            {

                var formatter = new BinaryFormatter();
                //formatter.Serialize(ms, obj);
                //var bytes = ms.GetBuffer();
                //return bytes;
            }

            var binary = new Binary();
            binary.EnableTrace();
            SetExt(binary);
            binary.Write(obj);
            var bytes1 = binary.GetBytes();

            binary.Stream = new MemoryStream(bytes1);
            var res = binary.Read(obj.GetType());

            //binary.TryDispose();
            return bytes1;
          

            //return bytes;
        }

        public static T GetObject<T>(this byte[] bytes) where T : class
        {
            if (bytes == null)
                return null;
            using (var ms = new MemoryStream(bytes))
            {
                var formatter = new BinaryFormatter();
                //var obj = formatter.Deserialize(ms) as T;
                //return obj;
            }

            var binary = new Binary();
            SetExt(binary);
            binary.Stream = new MemoryStream(bytes);
            var obj2 = binary.Read(typeof(T)) as T;
            return obj2;

            using (var ms = new MemoryStream(bytes))
            {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(ms) as T;
            }
        }

        static void SetExt(Binary bn)
        {
            var ims = bn.IgnoreMembers;
            ims.Add("Points");
            ims.Add("Items");
            ims.Add("Container");
            ims.Add("Self");
            ims.Add("Bin");
        }

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using NewLife.Serialization;
using NewLife.Web;

namespace NewLife.CubeNC.Extensions
{
    /// <summary>
    /// 对象助手
    /// </summary>
    public static class ObjectHelper
    {
        /// <summary>
        /// 序列化对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] ToBytes(this Object obj)
        {
            if (obj == null) return null;

            //using (var ms = new MemoryStream())
            //{

            //    var formatter = new BinaryFormatter();
            //    //formatter.Serialize(ms, obj);
            //    //var bytes = ms.GetBuffer();
            //    //return bytes;
            //}

            var binary = new Binary();
            binary.EnableTrace();
            SetExt(binary);
            binary.Write(obj);
            var bytes1 = binary.GetBytes();

            //binary.Stream = new MemoryStream(bytes1);
            //var res = binary.Read(obj.GetType());

            return bytes1;
          

            //return bytes;
        }

        /// <summary>
        /// 反序列化对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <param name="settingAction"></param>
        /// <returns></returns>
        public static T GetObject<T>(this byte[] bytes,Action<IBinary> settingAction=null) where T : class
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
            settingAction?.Invoke(binary);
            binary.Stream = new MemoryStream(bytes);
            var obj2 = binary.Read(typeof(T)) as T;
            return obj2;

            using (var ms = new MemoryStream(bytes))
            {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(ms) as T;
            }
        }

        /// <summary>
        /// 添加需要排除的属性名
        /// </summary>
        /// <param name="bn"></param>
        static void SetExt(Binary bn)
        {
            var ims = bn.IgnoreMembers;
            ims.Add("Points");
            ims.Add("Items");
            ims.Add("Container");
            ims.Add("Self");
            ims.Add("Bin");
            ims.Add(nameof(Pager.PageIndex));
        }

    }
}

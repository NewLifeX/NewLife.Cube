using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using NewLife.Serialization;
using NewLife.Web;

namespace NewLife.Cube.Extensions
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
        public static Byte[] ToBytes(this Object obj)
        {
            if (obj == null) return null;
            var binary = new Binary();
            //binary.EnableTrace();
            SetExt(binary);
            binary.Write(obj);
            var bytes1 = binary.GetBytes();

            return bytes1;
        }

        /// <summary>
        /// 反序列化对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <param name="settingAction">反序列化设置，最常用的设置为设置本次反序列化需要忽略的属性</param>
        /// <returns></returns>
        public static T GetObject<T>(this Byte[] bytes, Action<IBinary> settingAction=null) where T : class
            //, new()
        {
            var obj = (T)(Object)bytes.GetObject(typeof(T), settingAction);
            return obj;
        }

        /// <summary>
        /// 反序列化对象
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="type">反序列化的对象类型同时传了T和Type优先使用Type</param> 
        /// <param name="settingAction">反序列化设置，最常用的设置为设置本次反序列化需要忽略的属性</param>
        /// <returns></returns>
        public static Object GetObject(this Byte[] bytes, Type type , Action<IBinary> settingAction = null)
        {
            if (bytes == null)
                return null;

            var binary = new Binary();
            SetExt(binary);
            settingAction?.Invoke(binary);
            binary.Stream = new MemoryStream(bytes);
            var obj2 = binary.Read(type);
            return obj2;
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

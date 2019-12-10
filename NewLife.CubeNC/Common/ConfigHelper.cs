using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace NewLife.Cube.Common
{
    /// <summary>Appsetting配置文件帮助类</summary>
    public class ConfigHelper
    {
        /// <summary>
        /// 获取配置文件对象
        /// 使用方法：conf.GetSection('xxx').Value 方式获取相关设置
        /// </summary>
        /// <param name="path"></param>
        /// <param name="reloadOnChange"></param>
        /// <returns></returns>
        public static IConfigurationRoot GetRoot(String path = "appsettings.json", Boolean reloadOnChange = true)
        {
            var conf = new ConfigurationBuilder()
             .Add(new JsonConfigurationSource { Path = "appsettings.json", ReloadOnChange = true })
             .Build();

            return conf;
        }
    }
}

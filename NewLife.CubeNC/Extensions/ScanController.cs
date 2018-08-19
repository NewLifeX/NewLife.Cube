using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NewLife.Log;
using NewLife.Reflection;
using XCode;
using XCode.Membership;

namespace NewLife.CubeNC.Extensions
{
    public static class ScanControllerExtensions
    {

        public static void ScanController()
        {
            var controllers = typeof(Controller).GetAllSubclasses(false).ToArray();
            var AreaName = "";
            var mf = ManageProvider.Menu;
            if (mf == null) return;

#if DEBUG
            XTrace.WriteLine("开始注册菜单...");
#endif

            foreach (var type in FindAllArea())
            {
                using (var tran = (mf as IEntityOperate).CreateTrans())
                {
                    AreaName = type.GetCustomAttributesData()
                        ?.FirstOrDefault(f => f.AttributeType == typeof(AreaAttribute))
                        ?.ConstructorArguments
                        ?.FirstOrDefault()
                        .Value
                        ?.ToString();
                    XTrace.WriteLine("初始化[{0}]的菜单体系", AreaName);
                    mf.ScanController(AreaName, type.Assembly, type.Namespace);

                    // 更新区域名称为友好中文名
                    var menu = mf.Root.FindByPath(AreaName);
                    if (menu != null && menu.DisplayName.IsNullOrEmpty())
                    {
                        var dis = type.GetDisplayName();
                        var des = type.GetDescription();

                        if (!dis.IsNullOrEmpty()) menu.DisplayName = dis;
                        if (!des.IsNullOrEmpty()) menu.Remark = des;

                        (menu as IEntity).Save();
                    }

                    tran.Commit();
                }
            }



        }

        /// <summary>
        /// 获取所有区域类型，只需要获取一个控制器类型，即可代表整个区域
        /// </summary>
        /// <returns></returns>
        static List<Type> FindAllArea()
        {
            var list = new List<Assembly>();
            var typeList = new List<Type>();
            var areaDic = new Dictionary<String, Type>();

            var controllers = typeof(Controller).GetAllSubclasses(false).ToArray();
            foreach (var item in controllers)
            {
                var areaName = item.GetCustomAttributesData()
                    ?.FirstOrDefault(f=>f.AttributeType== typeof(AreaAttribute))
                    ?.ConstructorArguments
                    ?.FirstOrDefault()
                    .Value
                    ?.ToString();
                if(areaName.IsNullOrEmpty()) continue;
                var asm = item.Assembly;
                if (!list.Contains(asm))
                {
                    list.Add(asm);
                    if (!areaDic.ContainsKey(areaName))
                    {
                        areaDic[areaName] = item;
                    }
                }
            }
            return areaDic.Select(s=>s.Value).ToList();
        }
    }
}

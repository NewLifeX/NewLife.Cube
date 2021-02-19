using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using NewLife.Cube.Entity;
using NewLife.Log;
using NewLife.Reflection;
using XCode;
using XCode.Membership;

namespace NewLife.Cube
{
    /// <summary>区域特性基类</summary>
    /// <remarks>
    /// 提供以下功能：
    /// 1，区域名称。从类名中截取。其中DisplayName特性作为菜单中文名。
    /// 2，静态构造注册一次视图引擎、绑定提供者、过滤器
    /// 3，注册区域默认路由
    /// </remarks>
    public class AreaBase : AreaAttribute
    {
        private static readonly ConcurrentDictionary<Type, Type> _areas = new ConcurrentDictionary<Type, Type>();

        /// <summary>实例化区域注册</summary>
        public AreaBase(String areaName) : base(areaName) => RegisterArea(GetType());

        /// <summary>注册区域，每个继承此区域特性的类的静态构造函数都调用此方法，以进行相关注册</summary>
        public static void RegisterArea<T>() where T : AreaBase => RegisterArea(typeof(T));

        /// <summary>注册区域，每个继承此区域特性的类的静态构造函数都调用此方法，以进行相关注册</summary>
        public static void RegisterArea(Type areaType)
        {
            if (!_areas.TryAdd(areaType, areaType)) return;

            var ns = areaType.Namespace + ".Controllers";
            var areaName = areaType.Name.TrimEnd("Area");
            XTrace.WriteLine("开始注册权限管理区域[{0}]，控制器命名空间[{1}]", areaName, ns);

            // 自动检查并添加菜单
            Task.Run(() =>
            {
                try
                {
                    ScanController(areaType);
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            });
        }

        /// <summary>自动扫描控制器，并添加到菜单</summary>
        /// <remarks>默认操作当前注册区域的下一级Controllers命名空间</remarks>
        protected static void ScanController(Type areaType)
        {
            var areaName = areaType.Name.TrimEnd("Area");
            XTrace.WriteLine("初始化[{0}]的菜单体系", areaName);

            var mf = ManageProvider.Menu;
            if (mf == null) return;

            // 初始化数据库
            _ = Menu.Meta.Count;

            using var tran = (mf as IEntityFactory).Session.CreateTrans();

            var menus = mf.ScanController(areaName, areaType.Assembly, areaType.Namespace + ".Controllers");

            // 更新区域名称为友好中文名
            var menu = mf.Root.FindByPath(areaName);
            if (menu != null && menu.DisplayName.IsNullOrEmpty())
            {
                var dis = areaType.GetDisplayName();
                var des = areaType.GetDescription();

                if (!dis.IsNullOrEmpty()) menu.DisplayName = dis;
                if (!des.IsNullOrEmpty()) menu.Remark = des;

                (menu as IEntity).Update();
            }

            tran.Commit();

            // 扫描模型表
            ScanModel(areaName, menus);
        }

        private static ICollection<String> _namespaces;

        /// <summary>判断控制器是否归属于魔方管辖</summary>
        /// <param name="controllerActionDescriptor"></param>
        /// <returns></returns>
        public static Boolean Contains(ControllerActionDescriptor controllerActionDescriptor)
        {
            // 判断控制器是否在管辖范围之内
            var controller = controllerActionDescriptor.ControllerTypeInfo;
            var ns = controller.Namespace;
            if (!ns.EndsWith(".Controllers")) return false;

            if (_namespaces == null) _namespaces = new HashSet<String>(_areas.Keys.Select(e => e.Namespace));

            // 该控制器父级命名空间必须有对应的区域注册类，才会拦截其异常
            ns = ns.TrimEnd(".Controllers");
            return _namespaces.Contains(ns);
        }

        private static void ScanModel(String areaName, IList<IMenu> menus)
        {
            var models = ModelTable.FindAll().Where(e => e.Category.EqualIgnoreCase(areaName)).ToList();
            foreach (var menu in menus.OrderByDescending(e => e.Sort))
            {
                if (menu.FullName.IsNullOrEmpty()) continue;

                var ctrl = menu.FullName.GetTypeEx();
                if (ctrl == null || !ctrl.BaseType.IsGenericType) continue;

                var entityType = ctrl.BaseType.GenericTypeArguments.FirstOrDefault();
                if (entityType == null || !entityType.As<IEntity>()) continue;

                var factory = entityType.AsFactory();
                if (factory == null) continue;

                var table = models.FirstOrDefault(e => e.Name == entityType.Name);
                if (table == null) table = new ModelTable { Name = entityType.Name, Enable = true };

                table.Category = areaName;
                table.Url = menu.Url.TrimStart("~");
                table.Controller = ctrl.FullName;

                table.Fill(factory.Table);
                table.Save();

                var columns = ModelColumn.FindAllByTableId(table.Id);
                var idx = 1;
                foreach (var field in factory.AllFields)
                {
                    if (!field.IsDataObjectField && field.Type.GetTypeCode() == TypeCode.Object) continue;

                    var column = columns.FirstOrDefault(e => e.Name == field.Name);
                    if (column == null)
                    {
                        column = new ModelColumn
                        {
                            Name = field.Name,
                            Enable = true,

                            ShowInList = true,
                            ShowInForm = true,
                        };
                        columns.Add(column);
                    }

                    column.TableId = table.Id;
                    column.Sort = idx++;

                    column.Fill(field);
                    //column.Save();
                }
                columns.Save();
            }
        }
    }
}
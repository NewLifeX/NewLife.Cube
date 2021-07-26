using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Threading;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>模型表</summary>
    [Area("Admin")]
    public class ModelTableController : EntityController<ModelTable>
    {
        static ModelTableController()
        {
            MenuOrder = 55;

            ListFields.RemoveField("Controller", "TableName", "ConnName");

            {
                var df = ListFields.AddDataField("Columns", "Enable");
                df.Header = "列集合";
                df.DisplayName = "列集合";
                df.Url = "ModelColumn?tableId={Id}";
            }

            ModelTableSetting = table =>
            {
                var columns = table.Columns;

                // 不在列表页显示
                var fields = columns.FindAll(fa =>
                    fa.ShowInList &&
                    (fa.Name.EqualIgnoreCase(ModelTable._.Controller)
                     || fa.Name.EqualIgnoreCase(ModelTable._.TableName)
                     || fa.Name.EqualIgnoreCase(ModelTable._.ConnName)));

                foreach (var field in fields)
                {
                    field.ShowInList = false;
                }

                // 调整列宽
                columns.Find(f => f.Name.EqualIgnoreCase(ModelTable._.Name)).Width = "115";
                columns.Find(f => f.Name.EqualIgnoreCase(ModelTable._.DisplayName)).Width = "115";
                columns.Find(f => f.Name.EqualIgnoreCase(ModelTable._.Url)).Width = "200";

                columns.Save();

                // 添加列
                var column = ModelColumn.FindByTableIdAndName(table.Id, "Columns") ?? new ModelColumn
                {
                    TableId = table.Id,
                    Name = "Columns",
                    DisplayName = "列集合",
                    //CellText = "列集合",
                    //CellTitle = "列集合",
                    CellUrl = "/Admin/ModelColumn?tableId={id}",
                    ShowInList = true,
                    Enable = true,
                    Sort = 5,
                    Width = "80",
                };

                column.Save();

                return table;
            };
        }

        private static Boolean _inited;
        static void Init()
        {
            if (_inited) return;

            _inited = true;

            // 扫描模型表
            //ModelTable.ScanModel(areaName, menus);
            ThreadPoolX.QueueUserWorkItem(() =>
            {
                var mf = ManageProvider.Menu;
                if (mf == null) return;

                foreach (var areaType in AreaBase.GetAreas())
                {
                    var areaName = areaType.Name.TrimEnd("Area");
                    var menus = mf.FindByFullName(areaName);

                    var root = mf.FindByFullName(areaType.Namespace + ".Controllers");
                    if (root == null) root = mf.Root.FindByPath(areaName);

                    if (root != null) ModelTable.ScanModel(areaName, root.Childs);
                }
            });
        }

        protected override IEnumerable<ModelTable> Search(Pager p)
        {
            //Init();

            var category = p["category"];
            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();
            var key = p["Q"];

            return ModelTable.Search(category, null, start, end, key, p);
        }

        /// <summary>菜单不可见</summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        protected override IDictionary<MethodInfo, Int32> ScanActionMenu(IMenu menu)
        {
            if (menu.Visible && !menu.Necessary)
            {
                menu.Visible = false;
                (menu as IEntity).Update();
            }

            return base.ScanActionMenu(menu);
        }
    }
}
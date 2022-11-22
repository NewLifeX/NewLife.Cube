using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Threading;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Cube.Controllers
{
    /// <summary>模型表</summary>
    [Area("Cube")]
    [Menu(59, true, Icon = "fa-table")]
    public class ModelTableController : EntityController<ModelTable>
    {
        static ModelTableController()
        {
            ListFields.RemoveField("ID", "Controller", "TableName", "ConnName");
            ListFields.RemoveCreateField();
            ListFields.RemoveUpdateField();

            {
                var df = ListFields.AddListField("Columns", "Enable");
                //df.Header = "列集合";
                df.DisplayName = "列集合";
                df.Url = "/Cube/ModelColumn?tableId={Id}";
            }

            ModelTableSetting = table =>
            {
                if (table == null) return null;

                var columns = table.GetColumns();

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

        private static void Init()
        {
            if (_inited) return;

            _inited = true;

            // 扫描模型表
            //ModelTable.ScanModel(areaName, menus);
            ThreadPool.QueueUserWorkItem(s =>
            {
                var mf = ManageProvider.Menu;
                if (mf == null) return;
                try
                {
                    foreach (var areaType in AreaBase.GetAreas())
                    {
                        var areaName = areaType.Name.TrimEnd("Area");
                        var menus = mf.FindByFullName(areaName);

                        var root = mf.FindByFullName(areaType.Namespace + ".Controllers");
                        root ??= mf.Root.FindByPath(areaName);

                        if (root != null) ModelTable.ScanModel(areaName, root.Childs);
                    }
                }
                catch { }
            });
        }

        /// <summary>
        /// 高级搜索
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override IEnumerable<ModelTable> Search(Pager p)
        {
            Init();

            var category = p["category"];
            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();
            var key = p["Q"];

            return ModelTable.Search(category, null, start, end, key, p);
        }
    }
}
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Cube.Controllers
{
    /// <summary>模型列</summary>
    [Area("Cube")]
    [Menu(56, true, Icon = "fa-table")]
    public class ModelColumnController : EntityController<ModelColumn>
    {
        static ModelColumnController() 
        { 
            ListFields.RemoveField("Id", "TableId", "IsDataObjectField", "Description", "ShowInList", "ShowInAddForm", "ShowInEditForm", "ShowInDetailForm",
                "ShowInSearch", "Sort", "Width", "CellText", "CellTitle", "CellUrl", "HeaderText", "HeaderTitle", "HeaderUrl", "DataAction", "DataSource");
            ListFields.RemoveCreateField();
            ListFields.RemoveUpdateField();
        }

        /// <summary>
        /// 高级搜索
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override IEnumerable<ModelColumn> Search(Pager p)
        {
            var tableId = p["tableId"].ToInt(-1);
            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();
            var key = p["Q"];

            if (p.Sort.IsNullOrEmpty()) p.Sort = ModelColumn._.Sort.Asc();

            return ModelColumn.Search(tableId, null, start, end, key, p);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewLife.Cube.ViewModels
{
    /// <summary>
    /// select --下拉树形菜单
    /// </summary>
    public class SelectTree
    {
        /// <summary>
        /// ID 唯一标识
        /// </summary>
        public String ID { get; set; }

        /// <summary>
        /// option-name
        /// </summary>
        public String name { get; set; }

        /// <summary>
        /// option-value 作为判断时，必须是唯一
        /// </summary>
        /// <value></value>
        public String value { get; set; }

        /// <summary>
        /// select-value
        /// </summary>
        /// <value></value>
        public String parentID { get; set; }

        /// <summary>
        /// 数据库字段类型
        /// </summary>
        /// <value></value>
        public String fieldType { get; set; }

        /// <summary>
        /// select-value
        /// </summary>
        /// <value></value>
        public Boolean disabled { get; set; }

        /// <summary>
        /// select-value
        /// </summary>
        /// <value></value>
        public IList<SelectTree> children { get; set; }

        /// <summary>
        /// 菜单类型
        /// </summary>
        public String MenuType { get; set; }

        /// <summary>
        /// 使用递归方法建树
        /// </summary>
        public List<SelectTree> GetSelectTreeList(List<SelectTree> treeNodes, List<SelectTree> newTreeList, String pID)
        {
            newTreeList = new List<SelectTree>();
            var tempList = treeNodes.Where(c => c.parentID == pID).ToList();
            if (tempList.Count == 0)
            {
                return null;
            }
            for (var i = 0; i < tempList.Count; i++)
            {
                var node = new SelectTree();
                node.ID = tempList[i].ID;
                node.name = tempList[i].name;
                node.value = tempList[i].value;
                node.disabled = tempList[i].disabled;
                node.fieldType = tempList[i].fieldType;
                node.MenuType = tempList[i].MenuType;
                var id = string.Empty;
                if (!string.IsNullOrEmpty(node.ID))
                {
                    id = node.ID;
                }
                else
                {
                    id = node.value;
                }
                node.children = GetSelectTreeList(treeNodes, newTreeList, id);
                newTreeList.Add(node);
            }
            return newTreeList;
        }

        /// <summary>
        /// 使用递归方法建树--过滤
        /// </summary>
        public List<SelectTree> GetSelectTreeFilterList(List<SelectTree> treeNodes, List<SelectTree> newTreeList, string menuType)
        {
            newTreeList = new List<SelectTree>();
            if (treeNodes == null)
            {
                return null;
            }
            for (var i = 0; i < treeNodes.Count; i++)
            {
                var node = treeNodes[i];
                var chidList = new List<SelectTree>();
                if (node.children != null && node.children.Count >= 1)
                {
                    for (var j = 0; j < node.children.Count; j++)
                    {
                        var child = node.children[j];
                        if (child.MenuType != null && child.MenuType.Contains(menuType))
                        {
                            child.children = GetSelectTreeFilterList(child.children?.ToList(), newTreeList, menuType);
                            chidList.Add(child);

                        }
                    }
                    if (chidList.Count >= 1)
                    {
                        node.children = chidList;
                        newTreeList.Add(node);
                    }
                }
            }
            return newTreeList;
        }
    }
}

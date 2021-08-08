using System.Collections.Generic;
using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Components;
using NewLife.Cube.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;

namespace NewLife.Cube.Blazor.Views.Blazor
{
    public partial class MainLayout
    {
        private bool UseTabSet { get; set; } = true;

        private string Theme { get; set; } = "";

        private bool IsOpen { get; set; }

        private bool IsFixedHeader { get; set; } = true;

        private bool IsFixedFooter { get; set; } = true;

        private bool IsFullSide { get; set; } = true;

        private bool ShowFooter { get; set; } = true;

        private List<MenuItem> Menus { get; set; }

        private Dictionary<string, string> TabItemTextDictionary { get; set; }

        private static List<MenuItem> GetIconSideMenuItems()
        {
            var menus = new List<MenuItem>
            {
                new MenuItem() { Text = "返回组件库", Icon = "fa fa-fw fa-home", Url = "https://www.blazor.zone/components" },
                new MenuItem() { Text = "Index", Icon = "fa fa-fw fa-fa", Url = "/" , Match = NavLinkMatch.All},
                new MenuItem() { Text = "Counter", Icon = "fa fa-fw fa-check-square-o", Url = "/counter" },
                new MenuItem() { Text = "FetchData", Icon = "fa fa-fw fa-database", Url = "fetchdata" },
                new MenuItem() { Text = "Table", Icon = "fa fa-fw fa-table", Url = "table" }
            };

            return menus;
        }

        [Parameter]
        public IList<MenuTree> menus { get; set; }

        /// <summary>
        /// 菜单折叠
        /// </summary>
        public bool IsCollapsed { get; set; }

        private string ClassString => CssBuilder.Default("menu-demo-bar")
                .AddClass("is-collapsed", IsCollapsed)
                .Build();

        public IEnumerable<MenuItem> IconSideMenuItems { get; set; }

        #region 初始化组件
        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            // 菜单获取可以通过数据库获取，此处为示例直接拼装的菜单集合
            TabItemTextDictionary = new()
            {
                [""] = "Index"
            };
            Menus = GetIconSideMenuItems();
        }
        #endregion

        /// <summary>
        /// 设置参数前
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        //public override async Task SetParametersAsync(ParameterView parameters)
        //{
        //    await base.SetParametersAsync(parameters);
        //}

        #region 设置参数之后
        protected override void OnParametersSet()
        {
            var cmenus = new List<MenuItem>();
            foreach (var item in menus)
            {
                var childs = new List<MenuItem>();
                var parent = new MenuItem(item.Name, item.Url, "fa fa-fa");
                if (item.Children != null)
                    foreach (var child in item.Children)
                        childs.Add(new MenuItem(child.Name, child.Url, "fa fa-fa")
                        {
                            Parent = parent
                        });
                parent.Items = childs;
                cmenus.Add(parent);
            }
            var first = cmenus.FirstOrDefault();
            if (first != null) first.IsActive = true;
            IconSideMenuItems = cmenus;
            base.OnParametersSet();
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
        }
        #endregion

        #region 组件呈现之后
        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            base.OnAfterRenderAsync(firstRender);
        }
        #endregion
    }
}

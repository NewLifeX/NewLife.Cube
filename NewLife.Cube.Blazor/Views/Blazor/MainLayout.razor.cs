using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using NewLife.Common;
using NewLife.Cube.ViewModels;
using NewLife.Model;
using XCode.Membership;

namespace NewLife.Cube.Blazor.Views.Blazor
{
    public partial class MainLayout
    {
        [Inject] private IManageProvider _provider { get; set; }

        private IManageUser User { get => _provider.Current; }
        private SysConfig Config { get => SysConfig.Current; }

        /// <summary>
        /// 菜单列表
        /// </summary>
        public IEnumerable<MenuItem> MenuItems { get; set; }

        /// <summary>
        /// 展示头
        /// </summary>
        public Tab SideTabItems { get; set; } = new Tab();

        /// <summary>
        /// 主框架操作列表
        /// </summary>
        public IEnumerable<MenuItem> MainInfoItems { get; set; }



        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected override async Task OnInitializedAsync()
        {
            #region 菜单转换
            var menus = GetMenu();
            if (menus != null)
            {
                var cmenus = new List<MenuItem>();
                menus.ToList().ForEach(item =>
                {
                    var childs = new List<MenuItem>();
                    var parent = new MenuItem(item.Name, item.Url, "fa fa-fa");
                    if (item.Children != null)
                        item.Children.ToList().ForEach(child =>
                        {
                            childs.Add(new MenuItem(child.Name, child.Url, "fa fa-fa")
                            {
                                Parent = parent
                            });
                        });
                    parent.Items = childs;
                    cmenus.Add(parent);
                });
                cmenus.FirstOrDefault().IsActive = true;
                MenuItems = cmenus;
            }
            #endregion

            MainInfoItems = new List<MenuItem>
                {
                    new MenuItem(User.NickName)
                    {
                        IsActive = true,
                        Items = new List<MenuItem>
                        {
                            new("网站首页", icon:"fa fa-cog"),
                            new("个人信息", icon:"fa fa-user"),
                            new("注销", icon:"fa fa-power-off"),
                        }
                    },
                };

            await base.OnInitializedAsync();
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task CallBackMenuClick(MenuItem item)
        {
            if (item.Url.Contains("/")
                && SideTabItems.Items.FirstOrDefault(x => x.Text == item.Text) == null)
            {
                SideTabItems.AddTab(new Dictionary<string, object?>
                {
                    [nameof(TabItem.Text)] = item.Text,
                    [nameof(TabItem.IsActive)] = true,
                    [nameof(TabItem.Icon)] = item.Icon,
                    [nameof(TabItem.Url)] = item.Url,
                    [nameof(TabItem.ChildContent)] = new RenderFragment(builder =>
                    {
                        var index = 0;
                        builder.OpenElement(index++, "div");
                        builder.OpenElement(index++, "iframe");
                        builder.AddAttribute(index++, "src", "Blazor/" + item.Url);
                        builder.AddAttribute(index++, "width", "100%");
                        builder.AddAttribute(index++, "height", "100%");
                        builder.CloseElement();
                        builder.CloseElement();
                    })
                });
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// 获取菜单
        /// </summary>
        /// <returns>菜单列表</returns>
        private IList<MenuTree> GetMenu()
        {
            var user = _provider.Current as IUser ?? XCode.Membership.User.FindAll().FirstOrDefault();

            var fact = ManageProvider.Menu;
            var menus = fact.Root.Childs;
            if (user?.Role != null)
            {
                menus = fact.GetMySubMenus(fact.Root.ID, user, true);
            }

            // 如果顶级只有一层，并且至少有三级目录，则提升一级
            if (menus.Count == 1 && menus[0].Childs.All(m => m.Childs.Count > 0)) { menus = menus[0].Childs; }

            var menuTree = MenuTree.GetMenuTree(pMenuTree =>
            {
                var subMenus = fact.GetMySubMenus(pMenuTree.ID, user, true);
                return subMenus;
            }, list =>
            {

                var menuList = (from menu in list
                                    // where m.Visible
                                select new MenuTree
                                {
                                    ID = menu.ID,
                                    Name = menu.Name,
                                    DisplayName = menu.DisplayName ?? menu.Name,
                                    Url = menu.Url,
                                    Icon = menu.Icon,
                                    Visible = menu.Visible,
                                    ParentID = menu.ParentID,
                                    Permissions = menu.Permissions
                                }).ToList();
                return menuList.Count > 0 ? menuList : null;
            }, menus);

            return menuTree;
        }
    }
}

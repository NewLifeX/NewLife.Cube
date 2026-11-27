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
    /// <summary>Blazor 主布局，提供菜单、页签与主框架操作区</summary>
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



        /// <summary>组件初始化</summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        /// <summary>组件异步初始化，构建菜单数据</summary>
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

        /// <summary>设置组件参数</summary>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);
        }

        /// <summary>参数设置完成</summary>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
        }

        /// <summary>参数设置完成（异步）</summary>
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();
        }

        /// <summary>组件渲染完成</summary>
        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
        }

        /// <summary>组件渲染完成（异步）</summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task CallBackMenuClick(MenuItem item)
        {
            if (item.Url.Contains("/")
                && SideTabItems.Items.FirstOrDefault(x => x.Text == item.Text) == null)
            {
                SideTabItems.AddTab(new Dictionary<string, object>
                {
                    [nameof(TabItem.Text)] = item.Text,
                    [nameof(TabItem.IsActive)] = true,
                    [nameof(TabItem.Icon)] = item.Icon,
                    [nameof(TabItem.Url)] = item.Url,
                    [nameof(TabItem.ChildContent)] = new RenderFragment(builder =>
                    {
                        builder.OpenElement(0, "div");
                        builder.OpenElement(1, "iframe");
                        builder.AddAttribute(2, "src", "Blazor" + item.Url);
                        builder.AddAttribute(3, "width", "100%");
                        builder.AddAttribute(4, "height", "100%");
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
            var menus = NewLife.Cube.Membership.MenuHelper.FilterAvailable(fact.Root.Childs);
            if (user?.Role != null)
            {
                menus = NewLife.Cube.Membership.MenuHelper.FilterAvailable(fact.GetMySubMenus(fact.Root.Id, user, true));
            }

            // 如果顶级只有一层，并且至少有三级目录，则提升一级
            if (menus.Count == 1 && menus[0].Childs.All(m => m.Childs.Count > 0)) { menus = menus[0].Childs; }

            var menuTree = MenuTree.GetMenuTree(pMenuTree =>
            {
                var subMenus = NewLife.Cube.Membership.MenuHelper.FilterAvailable(fact.GetMySubMenus(pMenuTree.Id, user, true));
                return subMenus;
            }, list =>
            {

                var menuList = (from menu in list
                                    // where m.Visible
                                select new MenuTree
                                {
                                    Id = menu.Id,
                                    Name = menu.Name,
                                    DisplayName = NewLife.Cube.Membership.MenuHelper.GetDisplayName(menu),
                                    Url = menu.Url,
                                    Icon = menu.Icon,
                                    Visible = menu.Visible,
                                    ParentId = menu.ParentId,
                                    Permissions = NewLife.Cube.Membership.MenuHelper.GetPermissions(menu)
                                }).ToList();
                return menuList.Count > 0 ? menuList : null;
            }, menus);

            return menuTree;
        }
    }
}


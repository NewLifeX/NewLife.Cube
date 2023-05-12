using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NewLife.Web;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.Areas.Admin.Controllers;

/// <summary>角色控制器</summary>
[DisplayName("角色")]
[Description("系统基于角色授权，每个角色对不同的功能模块具备添删改查以及自定义权限等多种权限设定。")]
[AdminArea]
[Menu(90, true, Icon = "fa-user-plus")]
public class RoleController : EntityController<Role, RoleModel>
{
    static RoleController()
    {
        ListFields.RemoveField("Ex1", "Ex2", "Ex3", "Ex4", "Ex5", "Ex6", "UpdateUserID", "UpdateIP", "Remark");
        ListFields.RemoveCreateField();

        {
            var df = ListFields.AddListField("Remark", "UpdateUser");
        }
    }

    /// <summary>搜索数据集</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected override IEnumerable<Role> Search(Pager p)
    {
        var id = p["id"].ToInt(-1);
        if (id > 0)
        {
            var list = new List<Role>();
            var entity = Role.FindByID(id);
            if (entity != null) list.Add(entity);
            return list;
        }

        return Role.Search(p["dtStart"].ToDateTime(), p["dtEnd"].ToDateTime(), p["Q"], p);
    }

    /// <summary>
    /// 添加权限授权
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public override async Task<ApiResponse<Role>> Insert(Role entity)
    {
        // 检测避免乱用Add/id
        if (Factory.Unique.IsIdentity && entity[Factory.Unique.Name].ToInt() != 0) throw new Exception("我们约定添加数据时路由id部分默认没有数据，以免模型绑定器错误识别！");

        if (!Valid(entity, DataObjectMethodType.Insert, true))
            //ViewBag.StatusMessage = "验证失败！";
            //ViewBag.Fields = AddFormFields;

            //return View("AddForm", entity);

            return new ApiResponse<Role>(500, "验证失败！", null);

        var rs = false;
        var err = "";
        try
        {
            //SaveFiles(entity);
            entity.CreateTime = DateTime.Now;
            entity.CreateIP = GetHostAddresses();
            entity.Enable = true;

            // 保存权限项
            var menus = XCode.Membership.Menu.Root.AllChilds;
            var dels = new List<Int32>();
            // 遍历所有权限资源
            foreach (var item in menus)
            {
                // 是否授权该项
                var has = GetBool("p" + item.ID);
                if (!has)
                    dels.Add(item.ID);
                else
                {
                    // 遍历所有权限子项
                    var any = false;

                    foreach (var pf in item.Permissions)
                    {
                        var has2 = GetBool("pf" + item.ID + "_" + pf.Key);

                        if (has2)
                            entity.Set(item.ID, (PermissionFlags)pf.Key);
                        else
                            entity.Reset(item.ID, (PermissionFlags)pf.Key);

                        any |= has2;
                    }
                    // 如果原来没有权限，这是首次授权，且右边没有勾选任何子项，则授权全部
                    if (!any & !entity.Has(item.ID)) entity.Set(item.ID);
                }
            }

            // 删除已经被放弃权限的项
            foreach (var item in dels)
                if (entity.Has(item)) entity.Permissions.Remove(item);

            OnInsert(entity);

            var fs = await SaveFiles(entity);
            if (fs.Count > 0) OnUpdate(entity);
            if (LogOnChange) LogProvider.Provider.WriteLog("Insert", entity);

            rs = true;

            //var masterCode = Entity<TEntity>.Meta.Factory.AllFields.Find(x => {
            //    return x.Field.ColumnName.Equals("QrCode");
            //});
            //if (masterCode != null)
            //{
            //    //更新二维码
            //    entity.SetValue("QrCode", entity["QrCode"].ToString() + "?data={\"name\":\""+ entity["Name"]+ "\",\"code\":\""+entity["Code"]+"\"}";
            //    entity.Update();
            //}
        }
        catch (ArgumentException aex)
        {
            err = aex.Message;
            ModelState.AddModelError(aex.ParamName, aex.Message);
        }
        catch (Exception ex)
        {
            err = ex.Message;
            ModelState.AddModelError("", ex.Message);
        }

        var msg = "";
        if (!rs)
        {
            WriteLog("Add", false, err);

            msg = SysConfig.Develop ? "添加失败！" + err : "添加失败！";
            //ViewBag.StatusMessage = msg;

            // 添加失败，ID清零，否则会显示保存按钮
            entity[Role.Meta.Unique.Name] = 0;

            return new ApiResponse<Role>(500, msg);
        }

        //ViewBag.StatusMessage = "添加成功！";
        msg = "添加成功！";

        //添加明细
        rs = AddDetailed(entity);
        if (!rs)
        {
            WriteLog("Edit", false, err);

            msg = SysConfig.Develop ? "添加明细失败！" + err : "添加明细失败！";
            //ViewBag.StatusMessage = msg;

            // 添加失败，ID清零，否则会显示保存按钮
            entity[Role.Meta.Unique.Name] = 0;

            return new ApiResponse<Role>(500, msg);
        }

        return new ApiResponse<Role>(0, msg, entity);
    }

    /// <summary>保存</summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public override async Task<ApiResponse<Role>> Update(Role entity)
    {
        // 保存权限项
        var menus = XCode.Membership.Menu.Root.AllChilds;
        //var pfs = EnumHelper.GetDescriptions<PermissionFlags>().Where(e => e.Key > PermissionFlags.None);
        var dels = new List<Int32>();
        // 遍历所有权限资源
        foreach (var item in menus)
        {
            // 是否授权该项
            var has = GetBool("p" + item.ID);
            if (!has)
                dels.Add(item.ID);
            else
            {
                // 遍历所有权限子项
                var any = false;
                foreach (var pf in item.Permissions)
                {
                    var has2 = GetBool("pf" + item.ID + "_" + pf.Key);

                    if (has2)
                        entity.Set(item.ID, (PermissionFlags)pf.Key);
                    else
                        entity.Reset(item.ID, (PermissionFlags)pf.Key);
                    any |= has2;
                }
                // 如果原来没有权限，这是首次授权，且右边没有勾选任何子项，则授权全部
                if (!any & !entity.Has(item.ID)) entity.Set(item.ID);
            }
        }
        // 删除已经被放弃权限的项
        foreach (var item in dels)
            if (entity.Has(item)) entity.Permissions.Remove(item);

        return await base.Update(entity);
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    /// <returns></returns>
    protected virtual String GetHostAddresses() => HttpContext.GetUserHost();

    /// <summary>添加实体主表对应的从表记录</summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    protected virtual Boolean AddDetailed(IEntity entity)
    {
        if (entity == null)
            return false;
        // TO DO
        return true;
    }

    /// <summary>验证实体对象</summary>
    /// <param name="entity"></param>
    /// <param name="type"></param>
    /// <param name="post"></param>
    /// <returns></returns>
    protected override Boolean Valid(Role entity, DataObjectMethodType type, Boolean post)
    {
        var rs = base.Valid(entity, type, post);

        // 清空缓存
        if (post) Role.Meta.Session.ClearCache($"{type}-{entity}", true);

        return rs;
    }

    private Boolean GetBool(String name)
    {
        var v = GetRequest(name);
        if (v.IsNullOrEmpty()) return false;

        v = v.Split(",")[0];

        return !v.EqualIgnoreCase("true", "false") ? throw new XException("非法布尔值Request[{0}]={1}", name, v) : v.ToBoolean();
    }
}
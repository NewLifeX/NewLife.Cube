using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Entity;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>委托代理</summary>
    [Area("Admin")]
    public class PrincipalAgentController : EntityController<PrincipalAgent>
    {
        protected override Boolean Valid(PrincipalAgent entity, DataObjectMethodType type, Boolean post)
        {
            if (!post && type == DataObjectMethodType.Insert)
            {
                entity.Enable = true;
                entity.Times = 1;
                entity.Expire = DateTime.Now.AddMinutes(20);
            }

            return base.Valid(entity, type, post);
        }
    }
}
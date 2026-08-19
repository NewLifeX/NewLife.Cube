using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NewLife.Cube.Models;
using NewLife.Reflection;
using NewLife.Remoting;
using XCode.Configuration;
using XCode.Membership;

namespace NewLife.Cube;

// OSC-260819e483 P3：PATCH 局部字段更新与批量改字段（仅 WebAPI 工程编译；勿写入被 CubeNC Link 的 EntityController2.cs）
public partial class EntityController<TEntity, TModel>
{
    /// <summary>PATCH 局部字段更新请求体</summary>
    public class PatchFieldsRequest
    {
        /// <summary>主键（可能为字符串，兼容 Int64 雪花 ID）</summary>
        public String Id { get; set; }

        /// <summary>待更新字段值（键为字段名，白名单内）</summary>
        public Dictionary<String, Object> Values { get; set; }
    }

    /// <summary>批量字段更新请求体</summary>
    public class BatchUpdateFieldsRequest
    {
        /// <summary>主键集合，逗号分隔</summary>
        public String Keys { get; set; }

        /// <summary>待更新字段名（白名单内）</summary>
        public String Field { get; set; }

        /// <summary>字段值</summary>
        public Object Value { get; set; }
    }

    /// <summary>字段补丁结果（部分失败仍 Code=0，由 Ok/Fail 计数与 Errors 明细表达）</summary>
    public class FieldPatchResult
    {
        /// <summary>成功行数</summary>
        public Int32 Ok { get; set; }

        /// <summary>失败行数</summary>
        public Int32 Fail { get; set; }

        /// <summary>失败明细</summary>
        public List<FieldPatchError> Errors { get; set; } = [];
    }

    /// <summary>字段补丁单行错误</summary>
    public class FieldPatchError
    {
        /// <summary>主键</summary>
        public String Id { get; set; }

        /// <summary>错误消息</summary>
        public String Message { get; set; }
    }

    /// <summary>局部字段更新（PATCH）：只改白名单字段，避免 PUT 绑 TModel 默认值把未提交列打脏</summary>
    /// <param name="request">请求体</param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [DisplayName("局部更新{type}")]
    [HttpPatch("/api/[area]/[controller]")]
    public virtual ApiResponse<FieldPatchResult> PatchFields([FromBody] PatchFieldsRequest request)
    {
        if (request == null || request.Id.IsNullOrEmpty() || request.Values == null || request.Values.Count == 0)
            throw new ApiException(CubeCode.ParamError.ToInt(), "参数错误：缺少 id 或 values");

        var fields = BuildPatchWhitelist();
        // 未知字段整单 400（与错误表一致）
        foreach (var key in request.Values.Keys)
        {
            if (!fields.ContainsKey(key))
                throw new ApiException(CubeCode.ParamError.ToInt(), $"未知字段：{key}");
        }

        var ok = 0;
        var fail = 0;
        var errors = new List<FieldPatchError>();
        try
        {
            var entity = FindData(request.Id);
            if (entity == null) throw new Exception("数据不存在");

            foreach (var kv in request.Values)
            {
                var fi = fields[kv.Key];
                entity.SetItem(fi.Name, ChangeTypeValue(kv.Value, fi.Type));
            }

            if (!Valid(entity, DataObjectMethodType.Update, true)) throw new Exception("验证失败");

            // 校验头或 EnableFieldValidation 为真时走现有 ValidateEntityFields（与 Insert/Update 同规则）
            if (EnableFieldValidationRequested)
            {
                var fieldErrors = ValidateEntityFields(entity, DataObjectMethodType.Update);
                if (fieldErrors != null) throw new Exception(fieldErrors[0].Message);
            }

            // JSON 无 Form，OnUpdate 现码会落到 entity.Update()，子类 override 仍生效；自动化已由 AutomationPersistence 入队
            OnUpdate(entity);
            ok = 1;
        }
        catch (Exception ex)
        {
            fail = 1;
            errors.Add(new FieldPatchError { Id = request.Id, Message = ex.GetTrue()?.Message ?? ex.Message });
        }

        return new ApiResponse<FieldPatchResult>
        {
            Code = 0,
            Message = fail > 0 ? "部分更新失败" : "更新成功",
            Data = new FieldPatchResult { Ok = ok, Fail = fail, Errors = errors },
        };
    }

    /// <summary>批量改字段（POST）：对全部 keys 逐行 Valid + OnUpdate，部分失败继续后续行</summary>
    /// <param name="request">请求体</param>
    /// <returns></returns>
    [EntityAuthorize(PermissionFlags.Update)]
    [DisplayName("批量更新{type}")]
    [HttpPost("/api/[area]/[controller]/BatchUpdateFields")]
    public virtual ApiResponse<FieldPatchResult> BatchUpdateFields([FromBody] BatchUpdateFieldsRequest request)
    {
        if (request == null || request.Keys.IsNullOrEmpty() || request.Field.IsNullOrEmpty())
            throw new ApiException(CubeCode.ParamError.ToInt(), "参数错误：缺少 keys 或 field");

        var ids = ParseKeys(request.Keys);
        if (ids.Length == 0) throw new ApiException(CubeCode.ParamError.ToInt(), "参数错误：keys 为空");
        if (ids.Length > 500) throw new ApiException(CubeCode.ParamError.ToInt(), "参数错误：批量更新不能超过 500 条");

        var fields = BuildPatchWhitelist();
        if (!fields.TryGetValue(request.Field, out var fi))
            throw new ApiException(CubeCode.ParamError.ToInt(), $"未知字段：{request.Field}");

        // 值类型转换失败 → 全部行 fail（不逐行重复报）
        Object value;
        try
        {
            value = ChangeTypeValue(request.Value, fi.Type);
        }
        catch (Exception ex)
        {
            var allErr = ex.GetTrue()?.Message ?? ex.Message;
            return new ApiResponse<FieldPatchResult>
            {
                Code = 0,
                Message = "批量更新失败",
                Data = new FieldPatchResult
                {
                    Ok = 0,
                    Fail = ids.Length,
                    Errors = ids.Select(id => new FieldPatchError { Id = id + "", Message = allErr }).ToList(),
                },
            };
        }

        var ok = 0;
        var fail = 0;
        var errors = new List<FieldPatchError>();
        foreach (var id in ids)
        {
            try
            {
                var entity = FindData(id);
                if (entity == null) throw new Exception("数据不存在");

                entity.SetItem(fi.Name, value);

                if (!Valid(entity, DataObjectMethodType.Update, true)) throw new Exception("验证失败");

                if (EnableFieldValidationRequested)
                {
                    var fieldErrors = ValidateEntityFields(entity, DataObjectMethodType.Update);
                    if (fieldErrors != null) throw new Exception(fieldErrors[0].Message);
                }

                OnUpdate(entity);
                ok++;
            }
            catch (Exception ex)
            {
                fail++;
                errors.Add(new FieldPatchError { Id = id + "", Message = ex.GetTrue()?.Message ?? ex.Message });
            }
        }

        return new ApiResponse<FieldPatchResult>
        {
            Code = 0,
            Message = fail > 0 ? "部分更新失败" : "更新成功",
            Data = new FieldPatchResult { Ok = ok, Fail = fail, Errors = errors },
        };
    }

    /// <summary>构建 PATCH/批量白名单：EditFormFields ∩ !ReadOnly ∩ 非主键</summary>
    /// <returns>字段名（忽略大小写）→ FieldItem</returns>
    private Dictionary<String, FieldItem> BuildPatchWhitelist()
    {
        var dic = new Dictionary<String, FieldItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var df in EditFormFields)
        {
            if (df == null || df.PrimaryKey || df.ReadOnly) continue;
            var fi = df.Field;
            if (fi == null) continue;
            dic[fi.Name] = fi;
        }
        return dic;
    }

    /// <summary>把请求值转换到字段类型；可空类型解包。失败抛异常由调用方计入该行 fail</summary>
    /// <param name="value">原始值</param>
    /// <param name="type">字段类型</param>
    /// <returns></returns>
    private static Object ChangeTypeValue(Object value, Type type)
    {
        if (value == null) return null;
        var t = Nullable.GetUnderlyingType(type) ?? type;
        if (t.IsInstanceOfType(value)) return value;
        return value.ChangeType(t);
    }
}

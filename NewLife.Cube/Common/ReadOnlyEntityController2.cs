using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NewLife.Common;
using NewLife.Cube.AI;
using NewLife.Cube.ViewModels;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Web;
using XCode;
using XCode.Membership;
using XCode.Model;

namespace NewLife.Cube;

/// <summary>ֻ��ʵ�����������</summary>
public partial class ReadOnlyEntityController<TEntity>
{
    #region ����
    /// <summary>ʵ�幤��</summary>
    public static IEntityFactory Factory => Entity<TEntity>.Meta.Factory;

    /// <summary>ʵ��ı�ʱд��־��Ĭ��false</summary>
    protected static Boolean LogOnChange { get; set; }

    /// <summary>ϵͳ����</summary>
    public SysConfig SysConfig { get; set; }

    /// <summary>��ǰ�б�ҳ�Ĳ�ѯ��������Key</summary>
    protected static String CacheKey => $"CubeView_{typeof(TEntity).FullName}";
    #endregion

    #region ����
    static ReadOnlyEntityController()
    {
        // ǿ��ʵ����һ�Σ���ʼ��ʵ�����
        var entity = new TEntity();
    }

    /// <summary>���캯��</summary>
    public ReadOnlyEntityController()
    {
        var set = PageSetting;
        set.IsReadOnly = true;

#if MVC
        set.EnableTableDoubleClick = CubeSetting.Current.EnableTableDoubleClick;
#endif

        if (set.OrderByKey)
        {
            // ����100��������ʱ��Ĭ�ϲ������������������򣬱������ݿ�ѡ�������������¸��Ӳ�ѯ����
            if (Entity<TEntity>.Meta.ShardPolicy == null && Entity<TEntity>.Meta.Count > 1_000_000)
                set.OrderByKey = false;
        }

        SysConfig = SysConfig.Current;
    }
    #endregion

    #region ���ݻ�ȡ
    /// <summary>�������ݼ�</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected virtual IEnumerable<TEntity> Search(Pager p)
    {
        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();
        var key = p["Q"];

        var whereExpression = Entity<TEntity>.SearchWhereByKeys(key);
        if (start > DateTime.MinValue || end > DateTime.MinValue)
        {
            var masterTime = Factory.MasterTime;
            if (masterTime != null)
                whereExpression &= masterTime.Between(start, end);
        }

        //// ����ģ�������ã�ƴ����Ϊ�����ֶε��ֶ�
        //var modelTable = ModelTable;
        //var modelCols = modelTable?.GetColumns()?.Where(w => w.ShowInSearch)?.ToList() ?? new List<ModelColumn>();

        //foreach (var col in modelCols)
        //{
        //    var val = p[col.Name];
        //    if (val.IsNullOrWhiteSpace()) continue;
        //    whereExpression &= col.Field == val;
        //}

        // ����ӳ���ֶβ�ѯ
        foreach (var field in Factory.Fields)
        {
            var val = p[field.Name];
            if (!val.IsNullOrWhiteSpace())
            {
                whereExpression &= field.Equal(val.ChangeType(field.Type));
            }
        }

        return Entity<TEntity>.FindAll(whereExpression, p);
    }

    /// <summary>�������ݣ�֧������Ȩ��</summary>
    /// <param name="p"></param>
    /// <returns></returns>
    protected virtual IEnumerable<TEntity> SearchData(Pager p)
    {
        // ����Ȩ��
        var builder = CreateWhere();
        if (builder != null)
        {
            builder.Data2 ??= p.Items;
            p.State = builder;
        }

        // ������������Ĭ�Ͻ���
        if (PageSetting.OrderByKey && p.Sort.IsNullOrEmpty() && p.OrderBy.IsNullOrEmpty())
        {
            var uk = Factory.Unique;
            if (uk != null && uk.Type.IsInt())
            {
                p.OrderBy = uk.Desc();
            }
        }

        return Search(p);
    }

    /// <summary>���ҵ�������</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected virtual TEntity Find(Object key)
    {
        // �ֱ���Ҫ���⴦��
        var fact = Factory;
        var shardField = fact.ShardPolicy?.Field;
        if (shardField != null)
        {
            var dt = GetRequest(shardField.Name).ToDateTime();
            if (dt.Year > 2000)
            {
                var entity = new TEntity();
                entity[fact.Unique.Name] = key;
                entity[shardField.Name] = dt;
                return FindByKey(entity);
            }
        }

        return FindByKey(key);
    }

    private TEntity FindByKey(Object key)
    {
        var fact = Factory;
        if (fact.Unique == null)
        {
            var pks = fact.Table.PrimaryKeys;
            if (pks.Length > 0)
            {
                var exp = new WhereExpression();
                foreach (var item in pks)
                {
                    // ���ǰ��û�д�ֵ����Ҫ���빹���ѯ
                    var val = GetRequest(item.Name);

                    // 2021.04.18 ����
                    // ���ṹû��Ψһ����ֻ����������������id������һ��������
                    // ��id��Ϊ·�ɲ����������Request�л�ȡ����ֵ��
                    // ���յ������������ı���ѯ�������ݣ�ֻ�õ�����Ϊ��id������
                    if (val == null && item.Name.EqualIgnoreCase("id")) val = key.ToString();

                    if (val != null) exp &= item.Equal(val);
                }

                return Entity<TEntity>.Find(exp);
            }
        }

        return Entity<TEntity>.FindByKeyForEdit(key);
    }

    /// <summary>���ҵ������ݣ����ж�����Ȩ��</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    protected TEntity FindData(Object key)
    {
        // �Ȳ���������ж�����Ȩ��
        var entity = Find(key);
        if (entity != null)
        {
            // ����Ȩ��
            var builder = CreateWhere();
            if (builder != null && !builder.Eval(entity)) throw new InvalidOperationException($"�Ƿ���������[{key}]");
        }

        return entity;
    }

    /// <summary>������ѯ��������������Ҫ��������Ȩ��</summary>
    /// <returns></returns>
    protected virtual WhereBuilder CreateWhere()
    {
        var exp = "";
        var att = GetType().GetCustomAttribute<DataPermissionAttribute>();
        if (att != null)
        {
            // �ѵ�¼�û��ж�ϵͳ��ɫ��δ��¼ʱ���ж�
            var user = HttpContext.Items["CurrentUser"] as IUser;
            user ??= ManageProvider.User;
            if (user == null || !user.Roles.Any(e => e.IsSystem) && !att.Valid(user.Roles))
                exp = att.Expression;
        }

        // ���⻧
        var set = CubeSetting.Current;
        if (set.EnableTenant)
        {
            var ctxTenant = TenantContext.Current;
            if (ctxTenant != null && IsTenantSource)
            {
                // ���ö��⻧�ҽ��������̨��TenantId=0��ʱ���������⻧���ݣ�������̨Ҫ�ܿ��������⻧������
                var tenant = ctxTenant.Tenant;
                tenant ??= Tenant.FindById(ctxTenant.TenantId);
                if (tenant != null)
                {
                    // WhereBuilder �ڲ���� HttpContext.Items ��ȡ TenantId
                    HttpContext.Items["TenantId"] = tenant.Id;

                    if (typeof(TEntity) == typeof(Tenant))
                    {
                        if (!exp.IsNullOrEmpty())
                            exp = "Id={#TenantId} and " + exp;
                        else
                            exp = "Id={#TenantId}";
                    }
                    else
                    {
                        if (!exp.IsNullOrEmpty())
                            exp = "TenantId={#TenantId} and " + exp;
                        else
                            exp = "TenantId={#TenantId}";
                    }
                }
            }
        }

        if (exp.IsNullOrEmpty()) return null;

        var builder = new WhereBuilder
        {
            Factory = Factory,
            Expression = exp,
        };
        builder.SetData(Session);
        builder.SetData2(HttpContext.Items.ToDictionary(e => e.Key + "", e => e.Value));

        return builder;
    }

    /// <summary>�Ƿ��⻧ʵ����</summary>
    protected virtual Boolean IsTenantSource => typeof(TEntity).GetInterfaces().Any(e => e == typeof(ITenantScope));

    /// <summary>��ȡѡ�м�</summary>
    /// <returns></returns>
    protected virtual String[] SelectKeys => GetRequest("Keys")?.Split(",");

    /// <summary>��ȡ����ķ�ҳ�����ں���ѯ��������������</summary>
    /// <returns></returns>
    protected virtual Pager GetCachePager()
    {
        var request = WebHelper.Params;
        var queryData = request["_query"];
        if (!queryData.IsNullOrEmpty())
        {
            queryData = queryData.ToBase64().ToStr();
            var p = new Pager();
            p.Parse(queryData);
            return p;
        }
        else
        {
            // ����Ŀ���������������ƻ����������Ҫnewһ���¶���
            var p = Session[CacheKey] as Pager;
            return new Pager(p);
        }
    }

    /// <summary>��ε�������</summary>
    /// <returns></returns>
    protected virtual IEnumerable<TEntity> ExportData(Int32 max = 0)
    {
        var set = CubeSetting.Current;
        if (max <= 0) max = set.MaxExport;

        var p = GetCachePager();
        p.RetrieveTotalCount = true;
        p.PageIndex = 1;
        p.PageSize = 1;
        SearchData(p);
        p.PageSize = 20_000;

        //!!! �������ܴ�����ʱ������ʱ������ʱ���Ƭ����������ͳһ��ҳ����
        //if (Factory.Count > 100_000)
        if (p.TotalCount > 100_000)
        {
            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();
            if (start.Year > 2000 /*&& end.Year > 2000*/)
            {
                if (end.Year < 2000) end = DateTime.Now;

                // ���㲽����80%���ݼ�����20%ʱ���ϣ��չ�ÿҳ10000
                //var speed = (p.TotalCount * 0.8) / (24 * 3600 * 0.2);
                var speed = (Double)p.TotalCount / (24 * 3600);
                var step = p.PageSize / speed;

                XTrace.WriteLine("[{0}]��������[{1:n0}]��ʱ�����䣨{2},{3}������Ƭ����{4:n0}��", Factory.EntityType.FullName, p.TotalCount, start, end, step);

                return ExportDataByDatetime((Int32)step, max);
            }
        }

        XTrace.WriteLine("[{0}]��������[{1:n0}]����[{2:n0}]ҳ", Factory.EntityType.FullName, p.TotalCount, p.PageCount);

        return ExportDataByPage(p.PageSize, max);
    }

    /// <summary>��ҳ��������</summary>
    /// <param name="pageSize">ҳ��С��Ĭ��10_000</param>
    /// <param name="max">�������</param>
    /// <returns></returns>
    protected virtual IEnumerable<TEntity> ExportDataByPage(Int32 pageSize, Int32 max)
    {
        // ����ͷ��һЩҳ����������ǰҳ�Լ��Ժ������
        //var p = Session[CacheKey] as Pager;
        //p = new Pager(p)
        //{
        //    // ��Ҫ���¼��
        //    RetrieveTotalCount = false,
        //    PageIndex = 1,
        //    PageSize = pageSize
        //};
        var p = GetCachePager();
        p.RetrieveTotalCount = false;
        p.PageIndex = 1;
        p.PageSize = pageSize;

        while (max > 0)
        {
            if (HttpContext.RequestAborted.IsCancellationRequested) yield break;
            if (p.PageSize > max) p.PageSize = max;

            var list = SearchData(p);

            var count = list.Count();
            if (count == 0) break;
            max -= count;

            foreach (var item in list)
            {
                yield return item;
            }

            if (count < p.PageSize) break;

            p.PageIndex++;
        }

        // �����ڴ�
        GC.Collect();
    }

    /// <summary>ʱ���Ƭ��������</summary>
    /// <param name="step">��Ƭ������Ĭ��60</param>
    /// <param name="max">�������</param>
    /// <returns></returns>
    protected virtual IEnumerable<TEntity> ExportDataByDatetime(Int32 step, Int32 max)
    {
        // ����ͷ��һЩҳ����������ǰҳ�Լ��Ժ������
        //var p = Session[CacheKey] as Pager;
        //p = new Pager(p)
        //{
        //    // ��Ҫ���¼��
        //    RetrieveTotalCount = false,
        //    PageIndex = 1,
        //    PageSize = 0,
        //};
        var p = GetCachePager();
        p.RetrieveTotalCount = false;
        p.PageIndex = 1;
        p.PageSize = 0;

        var start = p["dtStart"].ToDateTime();
        var end = p["dtEnd"].ToDateTime();
        if (end.Year < 2000) end = DateTime.Now;

        //!!! ǰ��ͬһ���������
        if (start == start.Date && end == end.Date) end = end.AddDays(1);

        var dt = start;
        while (max > 0 && dt < end)
        {
            if (HttpContext.RequestAborted.IsCancellationRequested) yield break;

            var dt2 = dt.AddSeconds(step);
            if (dt2 > end) dt2 = end;

            p["dtStart"] = dt.ToFullString();
            p["dtEnd"] = dt2.ToFullString();

            var list = SearchData(p);

            var count = list.Count();
            //if (count == 0) break;

            foreach (var item in list)
            {
                yield return item;
            }

            dt = dt2;
            max -= count;
        }

        // �����ڴ�
        GC.Collect();
    }
    #endregion

    #region AI ����
    /// <summary>AI ���ݶ��졣���ݵ�ǰ��ѯ�����ռ����ݲ����ɷ�������</summary>
    /// <param name="think">�Ƿ������������</param>
    /// <param name="stream">�Ƿ���ʽ��� SSE</param>
    /// <param name="maxRows">�����������</param>
    /// <returns></returns>
    [DisplayName("AI ���ݶ���")]
    [EntityAuthorize(PermissionFlags.Detail)]
    [HttpGet]
    public virtual async Task<ActionResult> AiInsight(Boolean think = false, Boolean stream = true, Int32 maxRows = 100)
    {
        var set = CubeSetting.Current;
        if (!set.AISwitch) return Json(500, null, "AI δ���ã�����ϵͳ�����п��� AISwitch");

        var svc = HttpContext.RequestServices.GetService<IAIService>();
        if (svc == null) return Json(500, null, "AI ����δע��");

        // ���� _query ������ȡ��ѯ����
        var pager = GetCachePager();
        if (pager == null)
        {
            pager = new Pager(WebHelper.Params)
            {
                RetrieveTotalCount = true,
                PageIndex = 1,
                PageSize = 1,
            };
        }

        // �ռ����ݲ����� Prompt
        var ctx = AiInsightHelper.Collect<TEntity>(Factory, pager, maxRows);
        var prompt = AiInsightHelper.BuildPrompt(ctx);

        if (stream)
        {
            // SSE ��ʽ���
            Response.Headers["Content-Type"] = "text/event-stream; charset=utf-8";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";

            // ����Ԫ�����¼�
            var metaJson = new { type = "meta", model = set.AIModel, thinking = think }.ToJson();
            await Response.WriteAsync($"data: {metaJson}\n\n", HttpContext.RequestAborted);
            await Response.Body.FlushAsync(HttpContext.RequestAborted);

            await foreach (var chunk in svc.AnalyzeDataStreamAsync(prompt, think, HttpContext.RequestAborted))
            {
                if (chunk.IsNullOrEmpty()) continue;
                var eventJson = new { type = "text", content = chunk }.ToJson();
                await Response.WriteAsync($"data: {eventJson}\n\n", HttpContext.RequestAborted);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);
            }

            // ��������¼�
            await Response.WriteAsync($"data: {{\"type\":\"done\"}}\n\n", HttpContext.RequestAborted);
            await Response.Body.FlushAsync(HttpContext.RequestAborted);

            return new EmptyResult();
        }
        else
        {
            // һ���Է��� JSON
            var result = await svc.AnalyzeDataAsync(prompt, think, HttpContext.RequestAborted);

            return Json(0, null, new { result, model = set.AIModel, thinking = think });
        }
    }
    #endregion

    #region ʵ���������
    /// <summary>��֤ʵ�����</summary>
    /// <param name="entity">ʵ�����</param>
    /// <param name="type">��������</param>
    /// <param name="post">�Ƿ��ύ���ݽ׶�</param>
    /// <returns></returns>
    protected virtual Boolean Valid(TEntity entity, DataObjectMethodType type, Boolean post)
    {
        if (!ValidPermission(entity, type, post))
        {
            switch (type)
            {
                case DataObjectMethodType.Select: throw new NoPermissionException(PermissionFlags.Detail, "��Ȩ�鿴����");
                case DataObjectMethodType.Update: throw new NoPermissionException(PermissionFlags.Update, "��Ȩ��������");
                case DataObjectMethodType.Insert: throw new NoPermissionException(PermissionFlags.Insert, "��Ȩ��������");
                case DataObjectMethodType.Delete: throw new NoPermissionException(PermissionFlags.Delete, "��Ȩɾ������");
            }
        }

        if (post && LogOnChange)
        {
            // ������ǰд�޸���־�������޸ĺ�������ʧЧ���������־Ϊ��
            switch (type)
            {
                case DataObjectMethodType.Insert:
                case DataObjectMethodType.Delete:
                case DataObjectMethodType.Update when (entity as IEntity).HasDirty:
                    LogProvider.Provider.WriteLog(type + "", entity);
                    break;
            }
        }

        return true;
    }

    /// <summary>��֤ʵ�����</summary>
    /// <param name="entity">ʵ�����</param>
    /// <param name="type">��������</param>
    /// <param name="post">�Ƿ��ύ���ݽ׶�</param>
    /// <returns></returns>
    protected virtual Boolean ValidPermission(TEntity entity, DataObjectMethodType type, Boolean post) => true;
    #endregion

    #region �б��ֶκͱ����ֶ�
    private static FieldCollection _ListFields;
    /// <summary>�б��ֶι���</summary>
    protected static FieldCollection ListFields => _ListFields ??= new FieldCollection(Factory, ViewKinds.List);

    //private static FieldCollection _FormFields;
    ///// <summary>�����ֶι���</summary>
    //[Obsolete]
    //protected static FieldCollection FormFields => _FormFields ??= new FieldCollection(Factory, "Form");

    private static FieldCollection _AddFormFields;
    /// <summary>�����ֶι���</summary>
    protected static FieldCollection AddFormFields => _AddFormFields ??= new FieldCollection(Factory, ViewKinds.AddForm);

    private static FieldCollection _EditFormFields;
    /// <summary>�����ֶι���</summary>
    protected static FieldCollection EditFormFields => _EditFormFields ??= new FieldCollection(Factory, ViewKinds.EditForm);

    private static FieldCollection _DetailFields;
    /// <summary>�����ֶι���</summary>
    protected static FieldCollection DetailFields => _DetailFields ??= new FieldCollection(Factory, ViewKinds.Detail);

    private static FieldCollection _SearchFields;
    /// <summary>�����ֶι���</summary>
    protected static FieldCollection SearchFields => _SearchFields ??= new FieldCollection(Factory, ViewKinds.Search);

    /// <summary>��ȡ�ֶ���Ϣ��֧���û����ز����������Ķ��ƽ���</summary>
    /// <param name="kind">�ֶ����ͣ�1-�б�List��2-����Detail��3-����AddForm��4-�༭EditForm��5-����Search</param>
    /// <param name="model">��ȡ�ֶ��б�ʱ�����ģ�ͣ�������ʵ������ʵ���б���������������Ҫ��ʾ���ֶ�</param>
    /// <returns></returns>
    protected virtual FieldCollection OnGetFields(ViewKinds kind, Object model)
    {
        var fields = kind switch
        {
            ViewKinds.List => ListFields,
            ViewKinds.Detail => DetailFields,
            ViewKinds.AddForm => AddFormFields,
            ViewKinds.EditForm => EditFormFields,
            ViewKinds.Search => SearchFields,
            _ => ListFields,
        };
        fields = fields.Clone();

        // ����Ƕ�������ֶ�
        if ((kind == ViewKinds.EditForm || kind == ViewKinds.Detail) && model is TEntity entity)
        {
            // ��ȡ��������չ����������Ϊ�����ֶ�
            foreach (var item in fields.ToArray())
            {
                var field = (item as FormField)?.Expand;
                var p = field?.Decode?.Invoke(entity);
                if (p != null && p is not String)
                {
                    if (field.Name.IsNullOrEmpty()) field.Name = item.Name;
                    if (field.Category.IsNullOrEmpty()) field.Category = item.Category;
                    if (field.Prefix.IsNullOrEmpty()) field.Prefix = item.Name + "_";

                    var fs = OnExpandFields(field, entity, p);
                    if (fs != null && fs.Count > 0)
                    {
                        fields.AddRange(fs);

                        if (!field.Retain) fields.Remove(item);
                    }
                }
            }
        }

        return fields;
    }

    /// <summary>չ���ֶ�</summary>
    protected virtual FieldCollection OnExpandFields(ExpandField field, TEntity entity, Object parameter) => field.Expand(entity, parameter);
    #endregion
}
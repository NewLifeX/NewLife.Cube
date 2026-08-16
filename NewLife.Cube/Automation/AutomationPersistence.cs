using System.ComponentModel;
using NewLife.Data;
using NewLife.Log;
using XCode;

namespace NewLife.Cube.Automation;

/// <summary>包装 IEntityPersistence，在 SQL 成功后入队</summary>
public sealed class AutomationPersistence : IEntityPersistence
{
    private readonly IEntityPersistence _inner;

    /// <summary>内层持久化</summary>
    public AutomationPersistence(IEntityPersistence inner) => _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc />
    public IEntityFactory Factory => _inner.Factory;

    static String[] Capture(IEntity entity)
    {
        if (entity == null) return [];
        var fact = EntityFactory.CreateFactory(entity.GetType());
        if (fact == null) return [];
        return fact.Fields.Where(f => entity.Dirtys[f.Name]).Select(f => f.Name).ToArray();
    }

    static void After(IEntity entity, DataMethod method, String[] snap)
    {
        try { AutomationTrigger.OnPersisted(entity, method, snap); }
        catch (Exception ex) { XTrace.WriteException(ex); }
    }

    /// <inheritdoc />
    public Int32 Insert(IEntitySession session, IEntity entity)
    {
        var snap = Capture(entity);
        var rs = _inner.Insert(session, entity);
        if (rs > 0) After(entity, DataMethod.Insert, snap);
        return rs;
    }

    /// <inheritdoc />
    public Int32 Update(IEntitySession session, IEntity entity)
    {
        var snap = Capture(entity);
        var rs = _inner.Update(session, entity);
        if (rs > 0) After(entity, DataMethod.Update, snap);
        return rs;
    }

    /// <inheritdoc />
    public Int32 Delete(IEntitySession session, IEntity entity)
    {
        var snap = Capture(entity);
        var rs = _inner.Delete(session, entity);
        if (rs > 0) After(entity, DataMethod.Delete, snap);
        return rs;
    }

    /// <inheritdoc />
    public async Task<Int32> InsertAsync(IEntitySession session, IEntity entity)
    {
        var snap = Capture(entity);
        var rs = await _inner.InsertAsync(session, entity);
        if (rs > 0) After(entity, DataMethod.Insert, snap);
        return rs;
    }

    /// <inheritdoc />
    public async Task<Int32> UpdateAsync(IEntitySession session, IEntity entity)
    {
        var snap = Capture(entity);
        var rs = await _inner.UpdateAsync(session, entity);
        if (rs > 0) After(entity, DataMethod.Update, snap);
        return rs;
    }

    /// <inheritdoc />
    public async Task<Int32> DeleteAsync(IEntitySession session, IEntity entity)
    {
        var snap = Capture(entity);
        var rs = await _inner.DeleteAsync(session, entity);
        if (rs > 0) After(entity, DataMethod.Delete, snap);
        return rs;
    }

    /// <inheritdoc />
    public Int32 Insert(IEntitySession session, String[] names, Object[] values)
    {
        var rs = _inner.Insert(session, names, values);
        if (rs > 0) TryAfterNamesValues(DataMethod.Insert, names, values);
        return rs;
    }

    /// <inheritdoc />
    public Int32 Update(IEntitySession session, String setClause, String whereClause) =>
        _inner.Update(session, setClause, whereClause);

    /// <inheritdoc />
    public Int32 Update(IEntitySession session, String[] setNames, Object[] setValues, String[] whereNames, Object[] whereValues)
    {
        var affected = LoadByNames(whereNames, whereValues);
        var rs = _inner.Update(session, setNames, setValues, whereNames, whereValues);
        if (rs > 0 && affected != null)
        {
            foreach (var e in affected)
            {
                ApplyNames(e, setNames, setValues);
                After(e, DataMethod.Update, setNames ?? []);
            }
        }
        return rs;
    }

    /// <inheritdoc />
    public Int32 Delete(IEntitySession session, String whereClause, Int32 maximumRows = 0) =>
        _inner.Delete(session, whereClause, maximumRows);

    /// <inheritdoc />
    public Int32 Delete(IEntitySession session, String[] names, Object[] values)
    {
        var affected = LoadByNames(names, values);
        var rs = _inner.Delete(session, names, values);
        if (rs > 0 && affected != null)
        {
            foreach (var e in affected)
                After(e, DataMethod.Delete, names ?? []);
        }
        return rs;
    }

    void TryAfterNamesValues(DataMethod method, String[] names, Object[] values)
    {
        try
        {
            var fact = Factory;
            if (fact == null || AutomationTrigger.ShouldSkip(fact.EntityType)) return;
            var entity = fact.Create() as IEntity;
            if (entity == null) return;
            ApplyNames(entity, names, values);
            After(entity, method, names ?? []);
        }
        catch (Exception ex) { XTrace.WriteException(ex); }
    }

    IList<IEntity> LoadByNames(String[] names, Object[] values)
    {
        try
        {
            var fact = Factory;
            if (fact == null || names == null || values == null || names.Length == 0) return null;
            if (AutomationTrigger.ShouldSkip(fact.EntityType)) return null;
            Expression exp = null;
            for (var i = 0; i < names.Length && i < values.Length; i++)
            {
                var fi = fact.Fields?.FirstOrDefault(f => f.Name.EqualIgnoreCase(names[i]));
                if (fi == null) return null;
                var piece = fi.Equal(values[i]);
                exp = exp == null ? piece : (exp & piece);
            }
            if (exp == null) return null;
            return fact.FindAll(exp, null, null, 0, 50);
        }
        catch
        {
            return null;
        }
    }

    static void ApplyNames(IEntity entity, String[] names, Object[] values)
    {
        if (entity == null || names == null || values == null) return;
        for (var i = 0; i < names.Length && i < values.Length; i++)
        {
            try { entity.SetItem(names[i], values[i]); }
            catch { /* 忽略单字段 */ }
        }
    }

    /// <inheritdoc />
    public WhereExpression GetPrimaryCondition(IEntity entity) => _inner.GetPrimaryCondition(entity);

    /// <inheritdoc />
    public String GetSql(IEntitySession session, IEntity entity, DataObjectMethodType methodType) => _inner.GetSql(session, entity, methodType);

    /// <inheritdoc />
    public String InsertSQL(IEntitySession session) => _inner.InsertSQL(session);
}

/// <summary>启动时包装所有工厂 Persistence</summary>
public static class AutomationHost
{
    static Int32 _registered;

    /// <summary>注册 Worker 所需服务并包装持久化</summary>
    public static void Register(IServiceProvider services)
    {
        AutomationRuntime.Services = services;
        WrapAll();
    }

    /// <summary>包装已注册工厂</summary>
    public static void WrapAll()
    {
        foreach (var kv in EntityFactory.Entities)
        {
            if (kv.Value == null) continue;
            try { Ensure(kv.Value); }
            catch (Exception ex) { XTrace.WriteLine("Automation wrap {0}: {1}", kv.Value.EntityType?.Name, ex.Message); }
        }
        Interlocked.Exchange(ref _registered, 1);
    }

    /// <summary>懒补挂单个工厂（启动后注册的实体由触发侧/Worker 调用；幂等，跳过类型不包装）</summary>
    /// <param name="fact">实体工厂</param>
    /// <returns>是否完成包装</returns>
    public static Boolean Ensure(IEntityFactory fact)
    {
        if (fact?.Persistence == null || fact.Persistence is AutomationPersistence) return false;
        if (AutomationTrigger.ShouldSkip(fact.EntityType)) return false;
        fact.Persistence = new AutomationPersistence(fact.Persistence);
        return true;
    }
}

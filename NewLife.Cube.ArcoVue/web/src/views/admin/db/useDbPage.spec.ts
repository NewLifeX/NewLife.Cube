import { describe, expect, it } from 'vitest';
import { dbEntityNameText, dbEntityRemarkText, dbItemOf, mergeDbEntityRows } from './useDbPage';

describe('useDbPage 纯函数', () => {
  it('dbItemOf 兼容 PascalCase/camelCase 并丢弃无名称行', () => {
    expect(
      dbItemOf({ Name: 'Cube', Type: 'SQLite', Version: '3.40', Backups: 2 }),
    ).toEqual({ name: 'Cube', type: 'SQLite', version: '3.40', backups: 2 });
    expect(dbItemOf({ name: 'X', type: 'MySql', version: '8.0', backups: 0 })).toEqual({
      name: 'X',
      type: 'MySql',
      version: '8.0',
      backups: 0,
    });
    expect(dbItemOf({ Type: 'SQLite' })).toBeNull();
  });

  it('dbEntityNameText/dbEntityRemarkText 拆分描述首句与余下部分', () => {
    const record = {
      name: 'AccessRule',
      description: '访问规则。控制系统访问的安全访问规则，放行或拦截或限流',
      tableName: 'AccessRule',
      count: 5,
    };
    expect(dbEntityNameText(record)).toBe('访问规则');
    expect(dbEntityRemarkText(record)).toBe('控制系统访问的安全访问规则，放行或拦截或限流');
    expect(dbEntityNameText({ name: 'Lov', tableName: 'Lov', count: null })).toBe('Lov');
    expect(dbEntityRemarkText({ name: 'Lov', tableName: 'Lov', count: null })).toBe('');
  });

  it('mergeDbEntityRows 追加无实体模型的纯表并标记', () => {
    const entities = [{ name: 'AccessRule', tableName: 'AccessRule', count: 5 }];
    const merged = mergeDbEntityRows(entities, [
      { name: 'AccessRule', tableName: 'AccessRule', description: '访问规则', count: 5 },
      { name: 'LovDefinition', tableName: 'LovDefinition', count: 7 },
    ]);

    expect(merged).toHaveLength(2);
    expect(merged[0].pureTable).toBeUndefined();
    expect(merged[1]).toEqual({
      name: 'LovDefinition',
      tableName: 'LovDefinition',
      count: 7,
      pureTable: true,
    });
  });

  it('纯表无描述时备注显示占位说明', () => {
    expect(
      dbEntityRemarkText({ name: 'LovDefinition', tableName: 'LovDefinition', count: 7, pureTable: true }),
    ).toBe('无实体模型（仅数据表）');
  });
});

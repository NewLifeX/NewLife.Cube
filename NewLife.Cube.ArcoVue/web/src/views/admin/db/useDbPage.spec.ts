import { describe, expect, it } from 'vitest';
import { dbItemOf } from './useDbPage';

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
});

import { describe, expect, it } from 'vitest';
import { Auth } from '@newlifex/page-utils';
import { flattenDiff, getDbActionPermissions } from './dbPage';

describe('数据库页纯函数', () => {
  it('flattenDiff 展开同一表的多个差异列', () => {
    expect(flattenDiff([{
      name: 'User',
      tableName: 'User',
      displayName: '用户',
      hasEntityModel: true,
      columns: [
        { name: 'ExtraA', columnName: 'ExtraA', dataType: 'String' },
        { name: 'ExtraB', columnName: 'ExtraB', dataType: 'Int32' },
      ],
    }])).toEqual([
      {
        name: 'User',
        tableName: 'User',
        displayName: '用户',
        hasEntityModel: true,
        columnName: 'ExtraA',
        dataType: 'String',
      },
      {
        name: 'User',
        tableName: 'User',
        displayName: '用户',
        hasEntityModel: true,
        columnName: 'ExtraB',
        dataType: 'Int32',
      },
    ]);
  });

  it('只有 Update 权限时不显示查看动作但显示压缩', () => {
    expect(getDbActionPermissions({ [String(Auth.EDIT)]: '编辑' })).toEqual({
      canInspect: false,
      canCompact: true,
    });
  });
});

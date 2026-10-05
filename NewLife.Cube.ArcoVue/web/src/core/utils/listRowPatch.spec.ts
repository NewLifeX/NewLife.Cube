import { describe, expect, it } from 'vitest';
import { assignRowFields, deleteFollowUp, patchRowInList, removeListRow } from './listRowPatch';

describe('assignRowFields', () => {
  it('跳过主键，并写到已有的大小写翻转键', () => {
    const row: Record<string, unknown> = { Id: 7, name: '旧' };
    assignRowFields(row, { id: 9, Name: '新', remark: '补' }, 'Id');
    expect(row.Id).toBe(7);
    expect(row.name).toBe('新');
    expect(row.remark).toBe('补');
  });
});

describe('patchRowInList', () => {
  it('写回源行并换新数组引用', () => {
    const a = { Id: 1, Enable: true };
    const b = { Id: 2, Enable: false };
    const rows = [a, b];
    const result = patchRowInList(rows, 'Id', 2, { Enable: true });
    expect(result.row).toBe(b);
    expect(b.Enable).toBe(true);
    expect(result.rows).not.toBe(rows);
    expect(result.rows[1]).toBe(b);
  });

  it('找不到主键时不换数组', () => {
    const rows = [{ Id: 1, Enable: true }];
    const result = patchRowInList(rows, 'Id', 9, { Enable: false });
    expect(result.row).toBeNull();
    expect(result.rows).toBe(rows);
  });
});

describe('removeListRow', () => {
  const rows = [{ Id: 1, Name: '甲' }, { id: 2, Name: '乙' }];

  it('按主键去掉一行', () => {
    const result = removeListRow(rows, 'Id', 2);
    expect(result.removed).toBe(true);
    expect(result.rows).toEqual([{ Id: 1, Name: '甲' }]);
    expect(rows).toHaveLength(2);
  });

  it('找不到时不改原数组', () => {
    const result = removeListRow(rows, 'Id', 9);
    expect(result.removed).toBe(false);
    expect(result.rows).toBe(rows);
  });
});

describe('deleteFollowUp', () => {
  it('本页还有行或已在第一页时本地删除', () => {
    expect(deleteFollowUp(2, 3)).toBe('local');
    expect(deleteFollowUp(0, 0)).toBe('local');
  });

  it('非首页删空后回到上一页', () => {
    expect(deleteFollowUp(2, 0)).toBe('prevPage');
  });
});

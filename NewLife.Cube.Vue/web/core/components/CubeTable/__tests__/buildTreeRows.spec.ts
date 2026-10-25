import { describe, expect, it } from 'vitest';
import { buildTreeRows } from '../cubeTable.logic';

describe('buildTreeRows', () => {
  it('按 parentID 组装父子层级并保持同层顺序', () => {
    const rows = [
      { id: 1, parentID: 0, name: 'root' },
      { id: 2, parentID: 1, name: 'child-a' },
      { id: 3, parentID: 1, name: 'child-b' },
      { id: 4, parentID: 2, name: 'grandchild' },
    ];

    const tree = buildTreeRows(rows as Record<string, unknown>[]);

    expect(tree).toHaveLength(1);
    expect(tree[0].name).toBe('root');
    const children = tree[0].children as Record<string, unknown>[];
    expect(children.map((row) => row.name)).toEqual(['child-a', 'child-b']);
    expect((children[0].children as Record<string, unknown>[])[0].name).toBe('grandchild');
  });

  it('找不到父节点的行按根节点处理，不丢行', () => {
    const rows = [
      { id: 5, parentID: 99, name: 'orphan' },
      { id: 6, parentID: null, name: 'root' },
    ];

    const tree = buildTreeRows(rows as Record<string, unknown>[]);

    expect(tree.map((row) => row.name)).toEqual(['orphan', 'root']);
  });

  it('没有父字段时原样返回', () => {
    const rows = [{ id: 1, name: 'plain' }];

    expect(buildTreeRows(rows as Record<string, unknown>[])).toBe(rows);
  });

  it('不修改入参', () => {
    const rows = [{ id: 1, parentID: 0 }, { id: 2, parentID: 1 }];

    buildTreeRows(rows as Record<string, unknown>[]);

    expect(rows[0]).not.toHaveProperty('children');
    expect(rows[1]).not.toHaveProperty('children');
  });
});

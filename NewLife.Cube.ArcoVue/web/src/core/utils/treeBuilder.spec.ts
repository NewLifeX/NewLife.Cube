import { describe, expect, it } from 'vitest';
import {
  buildTree,
  buildTreeByParentId,
  buildTreeByPath,
  canBuildTree,
  hasParentIdData,
  hasPathData,
  type TreeRow,
} from './treeBuilder';

/** 泛型业务接口：模拟 FastJson camelCase 输出的行 */
interface MenuRow {
  id: number;
  parentId: number;
  name: string;
}

function flat(rows: Record<string, unknown>[]): Record<string, unknown>[] {
  return rows;
}

function names(rows: TreeRow<Record<string, unknown>>[]): string[] {
  return rows.map((r) => String((r as { name?: unknown }).name ?? ''));
}

function pathTree(): Record<string, unknown>[] {
  return [
    { name: '根', path: '/根' },
    { name: '子1', path: '/根/子1' },
    { name: '孙1', path: '/根/子1/孙1' },
    { name: '子2', path: '/根/子2' },
  ];
}

describe('canBuildTree', () => {
  it('true 当行含 ParentID + id', () => {
    expect(canBuildTree([{ id: 1, ParentID: 0 }])).toBe(true);
    expect(canBuildTree([{ Id: 1, parentId: 0 }])).toBe(true);
  });

  it('true 当行含 path', () => {
    expect(canBuildTree([{ name: 'x', path: '/x' }])).toBe(true);
  });

  it('false 当仅含 id 无父级/路径元数据，或空数组', () => {
    expect(canBuildTree([{ id: 1, name: 'x' }])).toBe(false);
    expect(canBuildTree([])).toBe(false);
  });

  it('hasParentIdData 需同时含父级与主键', () => {
    expect(hasParentIdData([{ id: 1, ParentID: 0 }])).toBe(true);
    expect(hasParentIdData([{ id: 1 }])).toBe(false);
    expect(hasParentIdData([{ ParentID: 0 }])).toBe(false);
  });

  it('hasPathData 匹配 path 系列字段', () => {
    expect(hasPathData([{ Path: '/a' }])).toBe(true);
    expect(hasPathData([{ fullPath: '/a' }])).toBe(true);
    expect(hasPathData([{ name: 'x' }])).toBe(false);
  });

  it('hasPathData / canBuildTree 识别 parentPath 字段（部门/地区结构）', () => {
    expect(hasPathData([{ name: 'x', path: '/x', parentPath: '' }])).toBe(true);
    expect(hasPathData([{ name: 'x', parentPath: '/x' }])).toBe(true);
    expect(canBuildTree([{ name: 'x', path: '/x', parentPath: '' }])).toBe(true);
    expect(canBuildTree([{ name: 'x', parentPath: '/x' }])).toBe(true);
  });
});

describe('buildTreeByParentId', () => {
  it('按 parentId 组装多级树，子节点挂 children', () => {
    const rows = [
      { id: 1, parentId: 0, name: '根' },
      { id: 2, parentId: 1, name: '子1' },
      { id: 3, parentId: 1, name: '子2' },
      { id: 4, parentId: 2, name: '孙1' },
    ];
    const tree = buildTreeByParentId(rows);
    expect(tree).toHaveLength(1);
    expect(names(tree)).toEqual(['根']);
    expect(names(tree[0].children)).toEqual(['子1', '子2']);
    expect(names(tree[0].children[0].children)).toEqual(['孙1']);
  });

  it('支持多根与字符串 id（Int64AsString）', () => {
    const rows = [
      { id: '1', parentId: '0', name: '根A' },
      { id: '2', parentId: '1', name: '子A' },
      { id: '3', parentId: '0', name: '根B' },
    ];
    const tree = buildTreeByParentId(rows);
    expect(names(tree)).toEqual(['根A', '根B']);
    expect(names(tree[0].children)).toEqual(['子A']);
  });

  it('parentId 为 null/undefined/-1 视为根', () => {
    const rows = [
      { id: 1, parentId: null, name: 'a' },
      { id: 2, parentId: undefined, name: 'b' },
      { id: 3, parentId: -1, name: 'c' },
    ];
    expect(buildTreeByParentId(rows)).toHaveLength(3);
  });

  it('父级缺失/自引用提升为根，避免丢数据', () => {
    const rows = [
      { id: 1, parentId: 99, name: '孤儿' },
      { id: 2, parentId: 2, name: '自引用' },
      { id: 3, parentId: 1, name: '子' },
    ];
    const tree = buildTreeByParentId(rows);
    expect(names(tree)).toEqual(['孤儿', '自引用']);
    // 孤儿被提升为根，但它的子节点仍正常挂载
    expect(names(tree[0].children)).toEqual(['子']);
  });

  it('字段名大小写不敏感（PascalCase 后端）', () => {
    const rows = [
      { ID: 1, ParentID: 0, Name: '根' },
      { ID: 2, ParentID: 1, Name: '子' },
    ];
    const tree = buildTreeByParentId(rows);
    expect(tree).toHaveLength(1);
    expect(tree[0].children).toHaveLength(1);
  });

  it('默认浅拷贝不污染原数据', () => {
    const rows = [
      { id: 1, parentId: 0, name: '根' },
      { id: 2, parentId: 1, name: '子' },
    ];
    buildTreeByParentId(rows);
    expect('children' in rows[0]).toBe(false);
    expect('children' in rows[1]).toBe(false);
  });

  it('clone:false 原地附加 children', () => {
    const rows = [
      { id: 1, parentId: 0, name: '根' },
      { id: 2, parentId: 1, name: '子' },
    ];
    const tree = buildTreeByParentId(rows, { clone: false });
    expect(rows[0]).toBe(tree[0]);
    expect(Array.isArray((rows[0] as { children?: unknown }).children)).toBe(true);
  });

  it('支持泛型类型，返回 TreeRow<MenuRow>', () => {
    const rows: MenuRow[] = [
      { id: 1, parentId: 0, name: '根' },
      { id: 2, parentId: 1, name: '子' },
    ];
    const tree: TreeRow<MenuRow>[] = buildTreeByParentId(rows);
    expect(tree[0].id).toBe(1);
    expect(tree[0].children[0].name).toBe('子');
  });
});

describe('buildTreeByPath', () => {
  it('按路径段组装多级树', () => {
    const tree = buildTreeByPath(pathTree());
    expect(names(tree)).toEqual(['根']);
    expect(names(tree[0].children)).toEqual(['子1', '子2']);
    expect(names(tree[0].children[0].children)).toEqual(['孙1']);
  });

  it('空路径/根路径作为根', () => {
    const rows = [
      { name: 'a', path: '' },
      { name: 'b', path: '/' },
      { name: 'c', path: 'x' },
    ];
    const tree = buildTreeByPath(rows);
    expect(tree).toHaveLength(3);
  });

  it('父路径缺失提升为根', () => {
    const rows = [
      { name: '孙', path: '/缺失/孙' },
      { name: '子', path: '/子' },
    ];
    const tree = buildTreeByPath(rows);
    expect(names(tree)).toEqual(['孙', '子']);
  });

  it('同时含 path 与 parentPath 时优先用 parentPath 关联（名称含分隔符也不歧义）', () => {
    // 节点名含 '/'：从 path 末段推导会错误提升为根，parentPath 可精确关联
    const rows = [
      { name: '根', path: '/根', parentPath: '' },
      { name: '销售/华东', path: '/根/销售/华东', parentPath: '/根' },
    ];
    const tree = buildTreeByPath(rows);
    expect(names(tree)).toEqual(['根']);
    expect(names(tree[0].children)).toEqual(['销售/华东']);
  });

  it('parentPath 组装多级树（部门/地区结构）', () => {
    const rows = [
      { name: '根', path: '/根', parentPath: '' },
      { name: '子1', path: '/根/子1', parentPath: '/根' },
      { name: '孙1', path: '/根/子1/孙1', parentPath: '/根/子1' },
    ];
    const tree = buildTreeByPath(rows);
    expect(names(tree)).toEqual(['根']);
    expect(names(tree[0].children)).toEqual(['子1']);
    expect(names(tree[0].children[0].children)).toEqual(['孙1']);
  });

  it('parentPath 为根路径（/）时作为根', () => {
    const rows = [
      { name: '根', path: '/根', parentPath: '/' },
      { name: '子', path: '/根/子', parentPath: '/根' },
    ];
    const tree = buildTreeByPath(rows);
    expect(tree).toHaveLength(1);
    expect(names(tree[0].children)).toEqual(['子']);
  });

  it('parentPath 匹配不到父时回退 path 推导', () => {
    const rows = [
      { name: '根', path: '/根', parentPath: '' },
      { name: '子', path: '/根/子', parentPath: '/不存在' },
    ];
    const tree = buildTreeByPath(rows);
    expect(names(tree)).toEqual(['根']);
    expect(names(tree[0].children)).toEqual(['子']);
  });

  it('支持 parentPathKey 自定义字段名', () => {
    const rows = [
      { name: '根', full: '/根', parentFull: '' },
      { name: '子', full: '/根/子', parentFull: '/根' },
    ];
    const tree = buildTreeByPath(rows, { pathKey: 'full', parentPathKey: 'parentFull' });
    expect(tree).toHaveLength(1);
    expect(names(tree[0].children)).toEqual(['子']);
  });
});

describe('buildTree', () => {
  it('优先 ParentID+id，其次 Path', () => {
    const rows = [
      { id: 1, parentId: 0, name: '根', path: '/根' },
      { id: 2, parentId: 1, name: '子', path: '/根/子' },
    ];
    const tree = buildTree(rows);
    expect(tree).toHaveLength(1);
    expect(tree[0].children).toHaveLength(1);
  });

  it('无 ParentID 时回退 Path', () => {
    const tree = buildTree(pathTree());
    expect(names(tree)).toEqual(['根']);
    expect(names(tree[0].children)).toEqual(['子1', '子2']);
  });

  it('无元数据时原样返回（不挂 children）', () => {
    const rows = flat([{ id: 1, name: 'x' }, { id: 2, name: 'y' }]);
    const tree = buildTree(rows);
    expect(tree).toHaveLength(2);
    expect('children' in tree[0]).toBe(false);
  });
});

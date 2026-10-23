import { describe, expect, it } from 'vitest';
import { canUseAction, parsePermissionMap } from '../utils/permission';
import { defaultColumnVisibility, joinMultiParams, mergeColumnVisibility } from '../engine/logic';
import { normalizePageMeta } from '../utils/fieldMeta';
import type { CubePageMeta } from '../engine/types';

describe('页面权限解析', () => {
  it('应该把用户权限串解析为菜单编号到权限位的映射', () => {
    const map = parsePermissionMap('1#15, 2#4, bad#8, 0#1');
    expect(map.get(1)).toBe(15);
    expect(map.get(2)).toBe(4);
    expect(map.has(0)).toBe(false);
  });

  it('应该按菜单权限位判断动作权限', () => {
    const map = parsePermissionMap('1#6');
    expect(canUseAction(map, 1, 4)).toBe(true);
    expect(canUseAction(map, 1, 8)).toBe(false);
    expect(canUseAction(map, 2, 8)).toBe(true);
    expect(canUseAction(map, undefined, 8)).toBe(true);
  });

  it('系统管理员不受菜单权限限制', () => {
    expect(canUseAction(new Map(), 1, 8, true)).toBe(true);
  });
});

describe('多选查询参数', () => {
  it('只把声明的多选字段数组转成逗号分隔', () => {
    const params = joinMultiParams({ Ids: [1, 2], Range: ['a', 'b'], Name: 'x' }, ['Ids']);
    expect(params).toEqual({ Ids: '1,2', Range: ['a', 'b'], Name: 'x' });
  });
});

describe('列默认显隐', () => {
  it('visible=false 的列默认隐藏，用户配置优先', () => {
    const columns = [
      { key: 'Name', label: '名称' },
      { key: 'Remark', label: '备注', visible: false },
    ];
    expect(defaultColumnVisibility(columns)).toEqual({ Name: true, Remark: false });
    expect(mergeColumnVisibility(columns, { Remark: true })).toEqual({ Name: true, Remark: true });
  });

  it('search 中的 keyword 项单独暴露且不再进入高级条件', () => {
    const meta: CubePageMeta = {
      setting: {},
      search: [
        { name: 'q', kind: 'keyword', typeName: 'String', displayName: '关键字' },
        { name: 'Name', typeName: 'String', displayName: '名称' },
      ],
    };
    const normalized = normalizePageMeta(meta);
    expect(normalized.keyword?.name).toBe('q');
    expect(normalized.searchFields.map((f) => f.name)).toEqual(['Name']);
  });

  it('列表字段直接保留 visible 标记', () => {
    const meta: CubePageMeta = {
      setting: {},
      list: [
        { name: 'Name', typeName: 'String' },
        { name: 'Remark', typeName: 'String', visible: false },
      ],
    };
    const fields = normalizePageMeta(meta).listFields;
    expect(fields.map((f) => [f.name, f.visible])).toEqual([
      ['Name', undefined],
      ['Remark', false],
    ]);
  });
});

/**
 * 地区级联纯函数单测
 *
 * 覆盖 ApiResponse / 裸数组归一、目标层级叶子判定、父级路径排序。
 */
import { describe, expect, it } from 'vitest';
import { areaPathIds, createLazyGuard, normalizeAreaResponse, toAreaOptions } from '../areaSelect.logic';

describe('AreaSelect/logic', () => {
  it('应兼容 ApiResponse 信封与裸数组', () => {
    const envelope = { code: 0, data: [{ id: 1, name: '北京', level: 1 }] };
    expect(normalizeAreaResponse(envelope)).toEqual([{ id: 1, name: '北京', level: 1 }]);
    expect(normalizeAreaResponse([{ id: 2, name: '上海', level: 1 }])).toEqual([
      { id: 2, name: '上海', level: 1 },
    ]);
  });

  it('应兼容 PascalCase 字段并过滤无效项', () => {
    const nodes = normalizeAreaResponse([
      { ID: 3, Name: '广东', Level: 1 },
      { id: 0, name: '空编号' },
      { id: 4, name: '' },
      null,
    ]);
    expect(nodes).toEqual([{ id: 3, name: '广东', level: 1 }]);
  });

  it('达到目标层级即叶子', () => {
    const options = toAreaOptions(
      [
        { id: 1, name: '省', level: 1 },
        { id: 2, name: '市', level: 2 },
      ],
      1,
    );
    expect(options[0]?.leaf).toBe(true);
    expect(options[1]?.leaf).toBe(true);

    const cityLevel = toAreaOptions([{ id: 1, name: '省', level: 1 }], 3);
    expect(cityLevel[0]?.leaf).toBe(false);
  });

  it('后端未返回 level 时按级联深度兜底判定叶子', () => {
    // 上海三级选择：根 → 省(1) → 市辖区(2) → 宝山(3)
    const provinces = toAreaOptions([{ id: 310000, name: '上海', level: 0 }], 3, 1);
    expect(provinces[0]?.level).toBe(1);
    expect(provinces[0]?.leaf).toBe(false);

    const cities = toAreaOptions([{ id: 310100, name: '市辖区', level: 0 }], 3, 2);
    expect(cities[0]?.level).toBe(2);
    expect(cities[0]?.leaf).toBe(false);

    const districts = toAreaOptions([{ id: 310113, name: '宝山', level: 0 }], 3, 3);
    expect(districts[0]?.level).toBe(3);
    expect(districts[0]?.leaf).toBe(true);

    // 两级选择：市级即叶子
    const twoLevel = toAreaOptions([{ id: 310100, name: '市辖区', level: 0 }], 2, 2);
    expect(twoLevel[0]?.leaf).toBe(true);
  });

  it('父级路径按层级升序并排除根节点', () => {
    const path = areaPathIds([
      { id: 0, name: '根', level: 0 },
      { id: 3, name: '区', level: 3 },
      { id: 1, name: '省', level: 1 },
      { id: 2, name: '市', level: 2 },
    ]);
    expect(path).toEqual([1, 2, 3]);
  });
});

describe('AreaSelect/logic createLazyGuard（风暴短路守卫）', () => {
  it('同一节点只放行首次请求，mark 后即短路', () => {
    const guard = createLazyGuard();
    expect(guard.canLoad(0)).toBe(true);
    guard.mark(0);
    expect(guard.canLoad(0)).toBe(false);
    // 重复 mark 幂等
    guard.mark(0);
    expect(guard.canLoad(0)).toBe(false);
  });

  it('不同节点互不影响', () => {
    const guard = createLazyGuard();
    guard.mark(0);
    expect(guard.canLoad(0)).toBe(false);
    expect(guard.canLoad(110000)).toBe(true);
  });

  it('负数与非整数编号同样可守卫（防御 0/NaN 根级重复拉取）', () => {
    const guard = createLazyGuard();
    expect(guard.canLoad(Number.NaN)).toBe(true);
    guard.mark(Number.NaN);
    expect(guard.canLoad(Number.NaN)).toBe(false);
  });

  it('guard 实例相互独立（每个 AreaSelect 实例各自守卫，不跨实例误伤）', () => {
    const a = createLazyGuard();
    const b = createLazyGuard();
    a.mark(5);
    expect(a.canLoad(5)).toBe(false);
    expect(b.canLoad(5)).toBe(true);
  });
});

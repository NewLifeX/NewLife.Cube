/**
 * lovStore 核心契约单测（防回归 · 守护 INV-5）。
 *
 * 关注点：`getSelectedLabel` 是 LIST 回显的"最后一道翻译"——只查 labelCache，
 * 缺失即回退原始值（数字 id），**绝不做"按 id 回退源数据"的作弊**。
 *
 * 为什么必须有这条测试：
 *   LovSelect 的逻辑单测（LovSelect.test.ts）用 `vi.mock('./lovStore')` 把 getSelectedLabel
 *   整个桩成 `label-${v}`，从未走真实解析路径——若有人在真实 lovStore 里重新引入源兜底，
 *   逻辑单测照样绿，缺陷被 mock 一起 mock 掉。本文件在真实模块上锁定契约，堵住这个盲区。
 *
 * 覆盖：
 *  1. 未登记 → 回退原始值（数字 id）
 *  2. registerRows（列表行登记）后 → 返回文本 label
 *  3. registerSelectedRow（选择登记，最高优先级）覆盖已有翻译
 *  4. registerRows 不覆盖已存在的精确翻译（选择登记优先于批量登记）
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';

// 阻断 lov-api 真实网络（本测试不触发 getListData/resolveSelectedLabel，仅锁契约）
vi.mock('@newlifex/cube-vue/core/utils/request', () => ({
  default: { get: vi.fn(), post: vi.fn() },
  get: vi.fn(),
  post: vi.fn(),
}));
vi.mock('@newlifex/cube-vue/core/utils/lov-api', () => ({
  fetchLovMeta: vi.fn(),
  fetchLovListData: vi.fn(),
  fetchLovListDataDirect: vi.fn(),
  shouldDirectRequest: vi.fn(() => false),
}));

import { getSelectedLabel, registerRows, registerSelectedRow, invalidateLov } from './lovStore';

describe('lovStore 回显契约（INV-5）', () => {
  beforeEach(() => invalidateLov());

  it('getSelectedLabel 未登记时回退原始值（数字 id），不欺骗成文本', () => {
    expect(getSelectedLabel('List.Test.User', 1)).toBe('1');
    expect(getSelectedLabel('List.Test.User', '2')).toBe('2');
    expect(getSelectedLabel('List.Test.User', undefined)).toBe('');
  });

  it('registerRows 后 getSelectedLabel 返回文本 label', () => {
    registerRows('List.Test.User', [
      { id: 1, name: '管理员' },
      { id: 2, name: '普通用户' },
    ]);
    expect(getSelectedLabel('List.Test.User', 1)).toBe('管理员');
    expect(getSelectedLabel('List.Test.User', 2)).toBe('普通用户');
  });

  it('registerSelectedRow（选择登记，最高优先级）覆盖已有翻译', () => {
    registerRows('List.Test.User', [{ id: 1, name: '旧名' }]);
    expect(getSelectedLabel('List.Test.User', 1)).toBe('旧名');
    registerSelectedRow('List.Test.User', { id: 1, name: '新名' });
    expect(getSelectedLabel('List.Test.User', 1)).toBe('新名');
  });

  it('registerRows 不覆盖已存在的精确翻译（选择登记优先于批量登记）', () => {
    registerSelectedRow('List.Test.User', { id: 1, name: '精确名' });
    registerRows('List.Test.User', [{ id: 1, name: '批量名' }]);
    expect(getSelectedLabel('List.Test.User', 1)).toBe('精确名');
  });
});

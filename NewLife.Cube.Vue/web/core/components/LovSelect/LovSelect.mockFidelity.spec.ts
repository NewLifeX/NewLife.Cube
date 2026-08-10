/**
 * CT lovStore 桩忠实性断言（防回归 · 守护 INV-6）。
 *
 * 背景（本轮会话踩坑）：
 *   曾有一次 CT 桩 `getSelectedLabel` 带「按 id 回退 SAMPLE 源」的作弊兜底，
 *   把本应显示的数字 id 重写成文本（如 `1` → `管理员`），导致「LIST 多选/单选回显数字」
 *   的真实缺陷被测试抹平——CT 全绿，但生产显示数字。
 *
 * 本测试锁定桩的契约：getSelectedLabel 只查 labelCache，缺失即回退原始值（数字 id），
 * 绝不做源数据兜底。这与真实 `lovStore.getSelectedLabel` 行为一致。
 * 若有人重新引入作弊兜底，本测试会第一时间变红，暴露"测试在骗自己"。
 *
 * 该桩是 CT 配置通过别名替换真实 lovStore 的，这里直接相对导入桩文件做契约校验，
 * 不依赖 CT runner。
 */
import { describe, it, expect, beforeEach } from 'vitest';
import {
  getSelectedLabel,
  registerRows,
  registerSelectedRow,
  invalidateLov,
} from '../../../ct/mocks/lovStore';

describe('CT lovStore 桩忠实性（不得作弊掩盖缺陷）', () => {
  beforeEach(() => invalidateLov());

  it('getSelectedLabel 未登记时回退原始数字 id（不欺骗成文本）', () => {
    expect(getSelectedLabel('List.Test.User', 1)).toBe('1');
    expect(getSelectedLabel('List.Test.User', '2')).toBe('2');
    expect(getSelectedLabel('List.Test.User', undefined)).toBe('');
  });

  it('getSelectedLabel 登记行（registerRows）后返回文本 label', () => {
    registerRows('List.Test.User', [{ id: 1, name: '管理员' }]);
    expect(getSelectedLabel('List.Test.User', 1)).toBe('管理员');
  });

  it('getSelectedLabel 单选登记（registerSelectedRow）后返回文本 label', () => {
    registerSelectedRow('List.Test.User', { id: 2, name: '普通用户' });
    expect(getSelectedLabel('List.Test.User', 2)).toBe('普通用户');
  });
});

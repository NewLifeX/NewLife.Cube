/**
 * 工作台部件辅助函数单元测试
 *
 * 覆盖弱类型 Widget 数据的文本化、HTML 链接提取、角色归一化、
 * KPI 配色白名单与 12 栅格收敛。
 * 运行：pnpm test:unit core/__tests__/workbenchWidget.spec.ts
 */
import { describe, it, expect } from 'vitest';
import {
  asRecord,
  extractLink,
  formatCell,
  isExternalLink,
  normalizeCols,
  normalizeKpiColor,
  normalizeRoles,
} from '../pages/workbench/widget';

describe('formatCell — 展示文本归一化', () => {
  it('空值显示短横线', () => {
    expect(formatCell(null)).toBe('-');
    expect(formatCell(undefined)).toBe('-');
    expect(formatCell('')).toBe('-');
  });

  it('剥离 HTML 标签并保留可读文本', () => {
    expect(formatCell('<a href="/x">查看 <b>详情</b> »</a>')).toBe('查看 详情 »');
  });

  it('数字/布尔/对象按预期展示', () => {
    expect(formatCell(0)).toBe('0');
    expect(formatCell(false)).toBe('false');
    expect(formatCell({ count: 2 })).toBe('{"count":2}');
  });
});

describe('extractLink — 后端 HTML 链接提取', () => {
  it('提取站内链接的 href 与文本', () => {
    expect(extractLink('<a href="/Admin/Index/Main" target="_blank">查看完整服务器信息 »</a>')).toEqual({
      href: '/Admin/Index/Main',
      text: '查看完整服务器信息 »',
    });
  });

  it('不是链接时返回 null', () => {
    expect(extractLink('普通文本')).toBeNull();
    expect(extractLink({ href: '/x' })).toBeNull();
  });

  it('识别站外链接', () => {
    expect(isExternalLink('https://newlifex.com')).toBe(true);
    expect(isExternalLink('/Admin/User')).toBe(false);
  });
});

describe('normalizeRoles — 角色字段归一化', () => {
  it('逗号分隔字符串拆成数组并去空白', () => {
    expect(normalizeRoles('Administrators, 运营 ,')).toEqual(['Administrators', '运营']);
  });

  it('数组过滤空值，空输入返回空数组', () => {
    expect(normalizeRoles(['A', '', 'B'])).toEqual(['A', 'B']);
    expect(normalizeRoles(undefined)).toEqual([]);
  });
});

describe('normalizeKpiColor / normalizeCols — 白名单与边界收敛', () => {
  it('合法配色原样返回并忽略大小写', () => {
    expect(normalizeKpiColor('RED')).toBe('red');
    expect(normalizeKpiColor('cyan')).toBe('cyan');
  });

  it('未知配色回退蓝色', () => {
    expect(normalizeKpiColor('pink')).toBe('blue');
    expect(normalizeKpiColor(undefined)).toBe('blue');
  });

  it('栅格宽度缺省 6，越界收敛到 1-12', () => {
    expect(normalizeCols(undefined)).toBe(6);
    expect(normalizeCols(0)).toBe(1);
    expect(normalizeCols(99)).toBe(12);
    expect(normalizeCols(4.6)).toBe(5);
  });
});

describe('asRecord — 弱类型数据收窄', () => {
  it('普通对象原样返回，数组与原始值返回空对象', () => {
    expect(asRecord({ a: 1 })).toEqual({ a: 1 });
    expect(asRecord([1, 2])).toEqual({});
    expect(asRecord('x')).toEqual({});
    expect(asRecord(null)).toEqual({});
  });
});

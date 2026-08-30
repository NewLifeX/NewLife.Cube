/**
 * 工作台 KPI 布局逻辑单元测试
 *
 * 覆盖：layoutKey camelCase 归一化（后端 Json 输出强制 camelCase）、
 * KPI 可见过滤 + 排序（隐藏剔除、未排序按后端顺序兜底）、已隐藏 KPI 提取（恢复面板）。
 */
import { describe, expect, it } from 'vitest';
import { filterHiddenKpis, layoutKey, sortKpisByLayout } from '../index';
import type { DashboardKpi } from '@/hooks/useDashboard';

/** 布局项 */
interface LayoutItem {
  sort?: number;
  hide?: boolean;
}

describe('工作台 KPI 布局', () => {
  it('layoutKey：部件名/卡片名统一转首字母小写', () => {
    expect(layoutKey('UserCount')).toBe('userCount');
    expect(layoutKey('MyLogins')).toBe('myLogins');
    expect(layoutKey('Log24h')).toBe('log24h');
    expect(layoutKey('kpi')).toBe('kpi');
    expect(layoutKey('monitor')).toBe('monitor');
    expect(layoutKey(undefined)).toBe('');
    expect(layoutKey('')).toBe('');
  });

  it('sortKpisByLayout：按布局排序（camelCase key），未排序项按后端顺序兜底', () => {
    const all: DashboardKpi[] = [
      { name: 'UserCount', label: '用户总数', value: '1' },
      { name: 'TodayLogin', label: '今日登录', value: '2' },
      { name: 'MyLogins', label: '我的登录', value: '3' },
    ];
    const layout: Record<string, LayoutItem> = {
      userCount: { sort: 1 },
      myLogins: { sort: 0 },
    };
    const rs = sortKpisByLayout(all, layout);
    expect(rs.map((k) => k.name)).toEqual(['MyLogins', 'UserCount', 'TodayLogin']);
  });

  it('sortKpisByLayout：隐藏项剔除（hide=true 不渲染）', () => {
    const all: DashboardKpi[] = [
      { name: 'UserCount', label: '用户总数', value: '1' },
      { name: 'MyLogins', label: '我的登录', value: '2' },
    ];
    const layout: Record<string, LayoutItem> = { userCount: { hide: true } };
    const rs = sortKpisByLayout(all, layout);
    expect(rs.map((k) => k.name)).toEqual(['MyLogins']);
  });

  it('sortKpisByLayout：无布局时保持后端顺序', () => {
    const all: DashboardKpi[] = [
      { name: 'MyLogins', label: '我的登录', value: '1' },
      { name: 'UserCount', label: '用户总数', value: '2' },
    ];
    const rs = sortKpisByLayout(all, {});
    expect(rs.map((k) => k.name)).toEqual(['MyLogins', 'UserCount']);
  });

  it('filterHiddenKpis：提取用户已隐藏 KPI（供恢复面板）', () => {
    const all: DashboardKpi[] = [
      { name: 'UserCount', label: '用户总数', value: '1' },
      { name: 'MyLogins', label: '我的登录', value: '2' },
      { name: 'CpuRate', label: 'CPU使用率', value: '3' },
    ];
    const layout: Record<string, LayoutItem> = { userCount: { hide: true }, cpuRate: { hide: false } };
    const rs = filterHiddenKpis(all, layout);
    expect(rs.map((k) => k.name)).toEqual(['UserCount']);
  });

  it('filterHiddenKpis：无隐藏返回空', () => {
    const all: DashboardKpi[] = [{ name: 'MyLogins', label: '我的登录', value: '1' }];
    expect(filterHiddenKpis(all, {})).toEqual([]);
  });
});

import { describe, expect, it } from 'vitest';
import { aiGreetingName, aiQuickItems, aiQuickItemsForTab, aiWelcomeSubtitle } from './aiWelcome';

describe('aiGreetingName', () => {
  it('无名 → 管理员', () => {
    expect(aiGreetingName(undefined)).toBe('管理员');
    expect(aiGreetingName('  ')).toBe('管理员');
    expect(aiGreetingName('胜平')).toBe('胜平');
  });
});

describe('aiWelcomeSubtitle', () => {
  it('按 page', () => {
    expect(aiWelcomeSubtitle('list')).toContain('列表');
    expect(aiWelcomeSubtitle('form')).toContain('表单');
    expect(aiWelcomeSubtitle('detail')).toContain('检查');
  });
});

describe('aiQuickItems', () => {
  it('list 推荐与分析均含分析当前数据', () => {
    const items = aiQuickItems('list');
    expect(items.some((i) => i.tab === 'recommend' && i.message === '分析当前列表数据')).toBe(true);
    expect(items.some((i) => i.tab === 'analyze' && i.message === '分析当前列表数据')).toBe(true);
  });

  it('form 推荐含填表；提问 Tab 无项', () => {
    expect(aiQuickItems('form').some((i) => i.message === '帮我填写当前表单')).toBe(true);
    expect(aiQuickItemsForTab('list', 'ask')).toEqual([]);
  });

  it('detail 仅诊断', () => {
    const rec = aiQuickItemsForTab('detail', 'recommend');
    expect(rec).toHaveLength(1);
    expect(rec[0].message).toBe('检查系统运行状态');
  });
});

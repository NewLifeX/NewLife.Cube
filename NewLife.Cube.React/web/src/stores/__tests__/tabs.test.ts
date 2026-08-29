/**
 * 多标签 Store 单元测试
 */
import { beforeEach, describe, expect, it } from 'vitest';
import { useTabsStore } from '@/stores/tabs';

describe('useTabsStore 多标签', () => {
  beforeEach(() => {
    useTabsStore.getState().reset();
  });

  it('添加标签：不同路径追加', () => {
    useTabsStore.getState().addTab({ path: '/', title: '首页', closable: true, fixed: true });
    useTabsStore.getState().addTab({ path: '/Admin/User', title: '用户', closable: true });
    const { tabs, activePath } = useTabsStore.getState();
    expect(tabs).toHaveLength(2);
    expect(tabs[1].title).toBe('用户');
    expect(activePath).toBe('/Admin/User');
  });

  it('添加标签：同路径去重，标题变化时更新', () => {
    // 菜单异步加载前先用回退标题，加载后补全为菜单名
    useTabsStore.getState().addTab({ path: '/Admin/User', title: '默认页面', closable: true });
    useTabsStore.getState().addTab({ path: '/Admin/User', title: '用户', closable: true });
    const { tabs, activePath } = useTabsStore.getState();
    expect(tabs).toHaveLength(1);
    expect(tabs[0].title).toBe('用户');
    expect(activePath).toBe('/Admin/User');
  });

  it('添加标签：标题相同不重建标签数组', () => {
    useTabsStore.getState().addTab({ path: '/Admin/User', title: '用户', closable: true });
    const before = useTabsStore.getState().tabs;
    useTabsStore.getState().addTab({ path: '/Admin/User', title: '用户', closable: true });
    // 引用不变：标题未变化时不触发多余渲染
    expect(useTabsStore.getState().tabs).toBe(before);
  });
});

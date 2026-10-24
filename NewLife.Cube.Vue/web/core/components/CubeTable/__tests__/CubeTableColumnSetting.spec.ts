/**
 * `CubeTableColumnSetting`（设置列弹层）组件测试
 *
 * 这里只测渲染与事件转发：显隐合并与 localStorage 在 `useCubeTableColumns` 测。
 * 关键口径是「未记录 = 显示」——新加的字段若默认隐藏，用户会以为列丢了。
 *
 * 运行：pnpm test:unit core/components/CubeTable/__tests__/CubeTableColumnSetting.spec.ts
 */
import './domShims';
import { afterEach, describe, expect, it } from 'vitest';
import { nextTick } from 'vue';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import CubeTableColumnSetting from '../CubeTableColumnSetting.vue';

const ITEMS = [
  { key: 'Name', label: '名称' },
  { key: 'Count', label: '数量' },
];

/**
 * 弹层内容 teleport 到 body，卸载包装器才会带走它；上一条用例残留的弹层会串进
 * 本条用例的 body 查询（表现：第二条用例数到 4 个勾选框），所以每条都清一次。
 */
afterEach(() => {
  document.body.querySelectorAll('.el-popper').forEach((n) => n.remove());
});

/** 弹层里的勾选框（afterEach 已清残留，body 里最多只剩当前这一份） */
const boxes = () =>
  Array.from(document.body.querySelectorAll('.el-popper .ctcs-item input')) as HTMLInputElement[];

async function mountSetting(visibility: Record<string, boolean> = {}) {
  const wrapper = mount(CubeTableColumnSetting, {
    props: { items: ITEMS, visibility },
    global: { plugins: [ElementPlus] },
  });
  await nextTick();
  await wrapper.find('.ctcs-trigger').trigger('click');
  await nextTick();
  return wrapper;
}

describe('CubeTableColumnSetting', () => {
  it('应该列出全部可配置列 当渲染时', async () => {
    await mountSetting();
    const labels = Array.from(document.body.querySelectorAll('.el-popper .ctcs-item label')).map((l) =>
      l.textContent?.trim(),
    );
    expect(labels).toEqual(['名称', '数量']);
  });

  it('应该把未记录的列视为显示 当 visibility 为空时', async () => {
    await mountSetting();
    expect(boxes().map((i) => i.checked)).toEqual([true, true]);
  });

  it('应该按记录取消勾选 当某列被显式关闭时', async () => {
    await mountSetting({ Name: false, Count: true });
    expect(boxes().map((i) => i.checked)).toEqual([false, true]);
  });

  it('应该抛出 set 事件并带列键 当勾选变化时', async () => {
    const wrapper = await mountSetting();
    // 弹层内容 teleport 到 body，但组件树仍在 wrapper 里，按组件发事件最稳
    wrapper.findAllComponents({ name: 'ElCheckbox' })[0]?.vm.$emit('change', false);
    await nextTick();
    expect(wrapper.emitted('set')?.[0]).toEqual(['Name', false]);
  });

  it('应该抛出 reset 事件 当点恢复默认时', async () => {
    const wrapper = await mountSetting();
    const boxesComp = wrapper.findAllComponents({ name: 'ElCheckbox' });
    expect(boxesComp).toHaveLength(2);
    const footer = document.body.querySelector('.el-popper .ctcs-footer button');
    expect(footer).toBeTruthy();
    footer?.dispatchEvent(new MouseEvent('click'));
    await nextTick();
    expect(wrapper.emitted('reset')).toBeTruthy();
  });

  it('应该抛出完整列序 当点下移第一列时', async () => {
    const wrapper = await mountSetting();
    const first = document.body.querySelectorAll('.el-popper .ctcs-item')[0];
    const down = first?.querySelector('button[title="下移"]') as HTMLButtonElement | null;

    down?.dispatchEvent(new MouseEvent('click'));
    await nextTick();

    expect(wrapper.emitted('order')?.[0]).toEqual([['Count', 'Name']]);
  });

  it('应该禁用首列上移与末列下移 当列序到达边界时', async () => {
    await mountSetting();
    const items = Array.from(document.body.querySelectorAll('.el-popper .ctcs-item'));
    const upOf = (item: Element) => item.querySelector('button[title="上移"]') as HTMLButtonElement;
    const downOf = (item: Element) => item.querySelector('button[title="下移"]') as HTMLButtonElement;

    expect(upOf(items[0]).disabled).toBe(true);
    expect(downOf(items[0]).disabled).toBe(false);
    expect(upOf(items[1]).disabled).toBe(false);
    expect(downOf(items[1]).disabled).toBe(true);
  });
});

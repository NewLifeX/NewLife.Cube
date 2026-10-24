/**
 * `CubeTableFormDialog` 的 preset 集成测试
 *
 * 纯函数合并已由 `pagePreset.spec` 覆盖；这里钉住「页面 preset 真的传到了表单渲染器」，
 * 防止引擎合并成功但弹窗仍按默认两列/自动分组渲染。
 *
 * 运行：pnpm test:unit core/components/CubeTable/__tests__/CubeTableFormDialog.spec.ts
 */
import './domShims';
import { describe, expect, it } from 'vitest';
import { nextTick } from 'vue';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import CubeTableFormDialog from '../CubeTableFormDialog.vue';
import FormContent from '../../../views/components/FormContent.vue';
import { FIELDS, makeFakeCtx } from './fakeCtx';
import { testI18n } from '../../../__tests__/helpers/i18n';

describe('CubeTableFormDialog preset 集成', () => {
  it('应该把 preset 的表单列数与分组方式传给 FormContent', async () => {
    const ctx = makeFakeCtx({
      formFields: [FIELDS.name, FIELDS.count],
      preset: {
        form: { columns: 3, groupBy: 'category' },
      },
    });
    ctx.dialogVisible.value = true;

    const wrapper = mount(CubeTableFormDialog, {
      props: { ctx },
      global: { plugins: [ElementPlus, testI18n] },
    });
    await nextTick();
    await nextTick();

    const content = wrapper.findComponent(FormContent);
    expect(content.exists()).toBe(true);
    expect(content.props('columns')).toBe(3);
    expect(content.props('groupBy')).toBe('category');
  });

  it('应该按字段数自适应 当没有 preset 时', async () => {
    const ctx = makeFakeCtx({ formFields: [FIELDS.name, FIELDS.count] });
    ctx.dialogVisible.value = true;

    const wrapper = mount(CubeTableFormDialog, {
      props: { ctx },
      global: { plugins: [ElementPlus, testI18n] },
    });
    await nextTick();
    await nextTick();

    const content = wrapper.findComponent(FormContent);
    expect(content.props('columns')).toBe(2);
    expect(content.props('groupBy')).toBe('auto');
  });
});

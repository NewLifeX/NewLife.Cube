import { flushPromises, mount } from '@vue/test-utils';
import { defineComponent, h } from 'vue';
import { describe, expect, it, vi } from 'vitest';
import { DefaultListPageKey, DefaultObjectPageKey } from '../../composables/useSections';
import i18n from '../../i18n';
import request from '../../utils/request';
import AutoPage from '../AutoPage.vue';

vi.mock('vue-router', () => ({ useRoute: () => ({ path: '/test/example' }) }));
vi.mock('../../utils/request', () => ({ default: vi.fn() }));

function makePage(kind: string) {
  return defineComponent({
    props: { title: String },
    setup: (props) => () => h('div', { 'data-kind': kind }, props.title),
  });
}

describe('AutoPage 默认页面注入', () => {
  it.each([
    ['list', { code: 0, data: [], page: {} }],
    ['object', { code: 0, data: { name: 'Example' } }],
  ] as const)('按接口响应渲染注入的 %s 页面', async (kind, response) => {
    vi.mocked(request).mockResolvedValueOnce(response);
    const wrapper = mount(AutoPage, {
      props: { title: 'Example page' },
      global: {
        plugins: [i18n],
        provide: {
          [DefaultListPageKey]: makePage('list'),
          [DefaultObjectPageKey]: makePage('object'),
        },
      },
    });

    await flushPromises();

    expect(wrapper.get(`[data-kind="${kind}"]`).text()).toBe('Example page');
    expect(wrapper.findAll('[data-kind]')).toHaveLength(1);
    wrapper.unmount();
  });
});

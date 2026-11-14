/**
 * `AreaSelect` 组件测试
 *
 * 纯函数归一/叶子判定已由 `areaSelect.logic.spec` 覆盖；这里只钉住组件接线：
 * 回显父级路径、选择后只向 v-model 抛出最后一级编号。
 *
 * 运行：pnpm test:unit core/components/AreaSelect/__tests__/AreaSelect.spec.ts
 */
import { describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';
import { mount } from '@vue/test-utils';
import ElementPlus from 'element-plus';
import AreaSelect from '../index.vue';

const requestStub = vi.hoisted(() => vi.fn(async (..._args: unknown[]): Promise<unknown> => undefined));
vi.mock('../../../utils/request', () => ({
  default: (...args: unknown[]) => requestStub(...args),
}));

describe('AreaSelect 组件', () => {
  it('应该按 AreaParents 回显完整父级路径', async () => {
    requestStub.mockClear();
    requestStub.mockResolvedValueOnce({
      data: [
        { id: 1, name: '省', level: 1 },
        { id: 2, name: '市', level: 2 },
        { id: 3, name: '区', level: 3 },
      ],
    });

    const wrapper = mount(AreaSelect, {
      props: { modelValue: 3 },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    await vi.waitFor(() => {
      const cascader = wrapper.findComponent({ name: 'ElCascader' });
      expect(cascader.props('modelValue')).toEqual([1, 2, 3]);
    });
    expect(requestStub.mock.calls[0][0]).toMatchObject({
      url: '/Cube/AreaParents',
      method: 'get',
      params: { id: 3, isContainSelf: true },
    });
  });

  it('应该只向 v-model 抛出最后一级编号 当用户选择完整路径时', async () => {
    requestStub.mockResolvedValue({ data: [] });
    const wrapper = mount(AreaSelect, {
      props: { modelValue: null, level: 3 },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    wrapper.findComponent({ name: 'ElCascader' }).vm.$emit('update:model-value', [1, 2, 3]);
    await nextTick();

    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([3]);
  });

  it('应该按单段 CubeController 特性路由请求子级 当级联懒加载触发时', async () => {
    requestStub.mockClear();
    requestStub.mockResolvedValue({
      data: [{ id: 11, name: '省', level: 1 }],
    });

    const wrapper = mount(AreaSelect, {
      props: { modelValue: null },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    // 直接触发 cascader 的 lazyLoad（等价于面板展开时 el-cascader 的调用）
    const cascader = wrapper.findComponent({ name: 'ElCascader' });
    const lazyProps = cascader.props('props') as {
      lazyLoad: (node: { value?: unknown } | undefined, resolve: (options?: unknown) => void) => void;
    };
    lazyProps.lazyLoad({ value: 0 }, () => {});
    await vi.waitFor(() => {
      expect(requestStub.mock.calls[0]?.[0]).toMatchObject({
        url: '/Cube/AreaChilds',
        method: 'get',
        params: { id: 0 },
      });
    });
  });

  it('应该吃真身 ApiResponse 驼峰投影并按单段特性路由请求 当懒加载返回真数据形态时', async () => {
    requestStub.mockClear();
    // CubeController.AreaChilds 真身：Json(0,null,new{ID,Name,FullName,BigArea} 投影)，序列化驼峰且无 level；
    // 用非根节点（el-cascader mount 时会自动 lazyLoad 根节点 0；成功加载不再 mark，
    // 手动直调非根 110000 避免与自动根调用的请求断言相互污染）
    requestStub.mockResolvedValue({
      code: 0,
      message: null,
      data: [
        { id: 110101, name: '东城区', fullName: '北京市东城区', bigArea: false },
        { id: 110102, name: '西城区', fullName: '北京市西城区', bigArea: false },
      ],
    });

    const wrapper = mount(AreaSelect, {
      props: { modelValue: null },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    const cascader = wrapper.findComponent({ name: 'ElCascader' });
    const lazyProps = cascader.props('props') as {
      lazyLoad: (node: { value?: unknown } | undefined, resolve: (options?: unknown) => void) => void;
    };
    let resolved: unknown[] | undefined;
    lazyProps.lazyLoad({ value: 110000 }, (options) => {
      resolved = options as unknown[];
    });
    await vi.waitFor(() => {
      const call = requestStub.mock.calls.find(
        (c) => (c[0] as { params?: { id?: number } } | undefined)?.params?.id === 110000,
      );
      expect(call?.[0]).toMatchObject({
        url: '/Cube/AreaChilds',
        method: 'get',
        params: { id: 110000 },
      });
    });
    await vi.waitFor(() => {
      expect(resolved).toEqual([
        { value: 110101, label: '东城区', level: 1, leaf: false },
        { value: 110102, label: '西城区', level: 1, leaf: false },
      ]);
    });
  });

  it('应该清空值 当用户清空级联选择时', async () => {
    requestStub.mockResolvedValue({ data: [] });
    const wrapper = mount(AreaSelect, {
      props: { modelValue: 3 },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    wrapper.findComponent({ name: 'ElCascader' }).vm.$emit('update:model-value', []);
    await nextTick();

    expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([undefined]);
  });

  it('同一父级被反复 lazyLoad 时只请求一次 AreaChilds', async () => {
    requestStub.mockClear();
    requestStub.mockResolvedValue({ data: [] });
    const wrapper = mount(AreaSelect, {
      props: { modelValue: null, level: 4 },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    const lazyLoad = wrapper.findComponent({ name: 'ElCascader' }).props('props').lazyLoad as (
      node: { value?: unknown; level?: number },
      resolve: (options?: unknown[]) => void,
    ) => void;

    let resolved = 0;
    for (let i = 0; i < 5; i++) {
      lazyLoad({ value: 0, level: 0 }, () => {
        resolved += 1;
      });
    }

    await vi.waitFor(() => expect(resolved).toBe(5));
    const areaCalls = requestStub.mock.calls.filter(
      (call) => (call[0] as { url?: string })?.url === '/Cube/AreaChilds',
    );
    expect(areaCalls).toHaveLength(1);
    expect(areaCalls[0][0]).toMatchObject({ params: { id: 0 } });
    expect(wrapper.emitted('update:modelValue')).toBeUndefined();
  });

  it('应该按级联深度判定叶子 当后端子级没有返回 level 时', async () => {
    requestStub.mockClear();
    requestStub.mockResolvedValue({ data: [{ id: 310100, name: '市辖区' }] });
    const wrapper = mount(AreaSelect, {
      props: { modelValue: null, level: 2 },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    const props = wrapper.findComponent({ name: 'ElCascader' }).props('props') as {
      lazyLoad: (node: { value?: unknown; level?: number }, resolve: (options?: any[]) => void) => void;
    };

    // 展开省级节点（level=1），子级是第 2 级，两级选择应直接成为叶子
    const cityOptions: any[] = [];
    props.lazyLoad({ value: 310000, level: 1 }, (options) => cityOptions.push(...(options ?? [])));
    await vi.waitFor(() => expect(cityOptions).toHaveLength(1));
    expect(cityOptions[0]).toMatchObject({ value: 310100, level: 2, leaf: true });

    // 三级选择时，同一批省级子级只是第 2 级，还可继续展开
    const wrapper3 = mount(AreaSelect, {
      props: { modelValue: null, level: 3 },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();
    const props3 = wrapper3.findComponent({ name: 'ElCascader' }).props('props') as typeof props;
    const cityOptions3: any[] = [];
    props3.lazyLoad({ value: 310000, level: 1 }, (options) => cityOptions3.push(...(options ?? [])));
    await vi.waitFor(() => expect(cityOptions3).toHaveLength(1));
    expect(cityOptions3[0]).toMatchObject({ value: 310100, level: 2, leaf: false });
  });

  // 守卫契约（areaSelect.logic.ts createLazyGuard 文档）：mark 只发生在失败/空结果
  // （短路风暴），成功加载不 mark（可重入）——若「发起即 mark」，面板 store 重建后
  // 重触发 lazyLoad 会被短路，面板永久空。
  it('应该只把失败或空结果计入守卫而成功后可重入 当懒加载守卫契约生效时', async () => {
    requestStub.mockClear();
    // 挂载自动根调用（id=0）：空结果 → mark(0)
    requestStub.mockResolvedValue({ data: [] });
    const wrapper = mount(AreaSelect, {
      props: { modelValue: null },
      global: { plugins: [ElementPlus] },
    });
    await nextTick();

    const lazyProps = wrapper.findComponent({ name: 'ElCascader' }).props('props') as {
      lazyLoad: (node: { value?: unknown } | undefined, resolve: (options?: unknown) => void) => void;
    };
    const load = (value: number) =>
      new Promise<unknown[]>((resolve) => {
        lazyProps.lazyLoad({ value }, (options) => resolve((options ?? []) as unknown[]));
      });

    // 同节点（0）第二次：被 canLoad 短路 resolve([]) 且不再发请求。
    // 先冲掉 mount 自动根调用的 .then(mark) 微任务链，确保守卫状态已落定
    await new Promise((resolve) => setTimeout(resolve, 0));
    const rootCount = requestStub.mock.calls.filter(
      (c) => (c[0] as { params?: { id?: number } }).params?.id === 0,
    ).length;
    const rootAgain = await load(0);
    expect(rootAgain).toEqual([]);
    expect(
      requestStub.mock.calls.filter((c) => (c[0] as { params?: { id?: number } }).params?.id === 0).length,
    ).toBe(rootCount);

    // 非根节点成功加载：请求发出并 resolve 真数据
    requestStub.mockResolvedValue({
      data: [
        { id: 110101, name: '东城区', fullName: '北京市东城区', bigArea: false },
        { id: 110102, name: '西城区', fullName: '北京市西城区', bigArea: false },
      ],
    });
    const ok1 = await load(110000);
    expect(ok1).toEqual([
      { value: 110101, label: '东城区', level: 1, leaf: false },
      { value: 110102, label: '西城区', level: 1, leaf: false },
    ]);

    // 成功不 mark：同节点重触发不被守卫短路；子级命中 childRequests 缓存
    //（成功结果缓存不删），面板 store 重建后仍可重新灌数据
    requestStub.mockClear();
    const ok2 = await load(110000);
    expect(ok2).toEqual([
      { value: 110101, label: '东城区', level: 1, leaf: false },
      { value: 110102, label: '西城区', level: 1, leaf: false },
    ]);
    // 成功结果走 childRequests 缓存，重触发不再发新请求（守卫只拦失败/空结果）
    expect(requestStub.mock.calls).toHaveLength(0);
  });
});

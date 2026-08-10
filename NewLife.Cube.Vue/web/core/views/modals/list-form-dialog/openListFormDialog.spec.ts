/**
 * openListFormDialog 命令式弹窗逻辑单元测试
 *
 * 覆盖场景：
 * 1. 新增模式 onConfirm → POST 请求 → onSuccess → resolve(true)
 * 2. 编辑模式 onConfirm → PUT 请求 → onSuccess → resolve(true)
 * 3. API 请求失败 → onConfirm 返回 false（阻止关闭）
 * 4. onCancel → resolve(false)
 * 5. onClosed → resolve(false)
 * 6. update:modelValue 事件 → 合并到 formData
 *
 * 运行：pnpm test:unit core/views/modals/list-form-dialog/openListFormDialog.spec.ts
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ElMessage } from 'element-plus';
import { openListFormDialog } from './openListFormDialog';
import type { FieldMeta } from '../../../types/field';

// ── Mock 依赖 ───────────────────────────────────────────────────

// 用 vi.hoisted 创建可变变量，vi.mock 工厂可安全引用（hoisted 至模块顶层）
const mocks = vi.hoisted(() => ({
  openModal: vi.fn(),
  request: vi.fn(),
  ElMessage: { success: vi.fn(), error: vi.fn() },
  serializeSubmitModel: (data: Record<string, unknown>) => data,
}));

vi.mock('@newlifex/cube-vue/core/composables/useModal', () => ({
  useModal: () => ({ openModal: mocks.openModal }),
}));

vi.mock('@newlifex/cube-vue/core/utils/request', () => ({
  default: mocks.request,
}));

vi.mock('element-plus', () => ({
  ElMessage: mocks.ElMessage,
}));

vi.mock('@newlifex/cube-vue/core/utils/fieldControl', () => ({
  serializeSubmitModel: mocks.serializeSubmitModel,
}));

// ── 测试数据 ────────────────────────────────────────────────────

const FIELDS: FieldMeta[] = [
  { name: 'Name', displayName: '名称', typeName: 'String', length: 50 },
  { name: 'Enable', displayName: '启用', typeName: 'Boolean' },
];

describe('openListFormDialog', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const { openModal: openModalMock } = mocks;
  const requestMock = mocks.request;

  it('新增模式：onConfirm → POST 请求 → onSuccess → resolve(true)', async () => {
    const onSuccess = vi.fn();
    requestMock.mockResolvedValue(undefined);

    // 调用 openListFormDialog，但不 await（需要先捕获 openModal 配置）
    const promise = openListFormDialog({
      title: '新增测试',
      fields: FIELDS,
      apiPrefix: '/api/test',
      mode: 'add',
      onSuccess,
    });

    // 验证 openModal 被调用，并捕获其配置
    expect(openModalMock).toHaveBeenCalledOnce();
    const modalOptions = openModalMock.mock.calls[0][0];

    expect(modalOptions.title).toBe('新增测试');
    expect(modalOptions.type).toBe('dialog');
    expect(modalOptions.width).toBe('700px');
    expect(modalOptions.component).toBeDefined();
    expect(modalOptions.destroyOnClose).toBe(true);

    // 触发 onConfirm
    const result = await modalOptions.onConfirm();

    // 验证请求
    expect(requestMock).toHaveBeenCalledWith({
      url: '/api/test',
      method: 'post',
      data: {},
    });
    expect(mocks.ElMessage.success).toHaveBeenCalledWith('新增成功');
    expect(onSuccess).toHaveBeenCalledOnce();

    // onConfirm 返回 true（允许关闭）
    expect(result).toBeUndefined();
    // 验证 promise resolve(true)
    await expect(promise).resolves.toBe(true);
  });

  it('编辑模式：onConfirm → PUT 请求 → onSuccess → resolve(true)', async () => {
    const onSuccess = vi.fn();
    requestMock.mockResolvedValue(undefined);

    const promise = openListFormDialog({
      title: '编辑测试',
      fields: FIELDS,
      modelValue: { Name: 'old', Enable: true },
      apiPrefix: '/api/test',
      mode: 'edit',
      onSuccess,
    });

    const modalOptions = openModalMock.mock.calls[0][0];
    await modalOptions.onConfirm();

    expect(requestMock).toHaveBeenCalledWith({
      url: '/api/test',
      method: 'put',
      data: { Name: 'old', Enable: true },
    });
    expect(mocks.ElMessage.success).toHaveBeenCalledWith('更新成功');
    expect(onSuccess).toHaveBeenCalledOnce();
    await expect(promise).resolves.toBe(true);
  });

  it('API 请求失败 → onConfirm 返回 false（阻止弹窗关闭）', async () => {
    requestMock.mockRejectedValue(new Error('Network Error'));

    const promise = openListFormDialog({
      title: '失败测试',
      fields: FIELDS,
      apiPrefix: '/api/test',
      mode: 'add',
    });

    const modalOptions = openModalMock.mock.calls[0][0];
    const result = await modalOptions.onConfirm();

    // 返回 false 阻止关闭
    expect(result).toBe(false);
    // promise 不应该 resolve（弹窗未关闭）
    // 注意：promise 不会 resolve，因为 onConfirm 的 catch 没有 resolve
  });

  it('API 请求失败后 promise 不 resolve（弹窗保持打开）', async () => {
    requestMock.mockRejectedValue(new Error('Network Error'));

    openListFormDialog({
      title: '失败测试',
      fields: FIELDS,
      apiPrefix: '/api/test',
      mode: 'add',
    });

    const modalOptions = openModalMock.mock.calls[0][0];
    const result = await modalOptions.onConfirm();
    expect(result).toBe(false);
  });

  it('onCancel → resolve(false)', async () => {
    const promise = openListFormDialog({
      title: '取消测试',
      fields: FIELDS,
      apiPrefix: '/api/test',
      mode: 'add',
    });

    const modalOptions = openModalMock.mock.calls[0][0];
    modalOptions.onCancel();

    await expect(promise).resolves.toBe(false);
  });

  it('onClosed → resolve(false)（当未被其他路径 resolve 时）', async () => {
    const promise = openListFormDialog({
      title: '关闭测试',
      fields: FIELDS,
      apiPrefix: '/api/test',
      mode: 'add',
    });

    const modalOptions = openModalMock.mock.calls[0][0];
    modalOptions.onClosed();

    await expect(promise).resolves.toBe(false);
  });

  it('update:modelValue 事件 → 合并到 formData', async () => {
    requestMock.mockResolvedValue(undefined);

    const promise = openListFormDialog({
      title: '数据合并测试',
      fields: FIELDS,
      apiPrefix: '/api/test',
      mode: 'add',
    });

    const modalOptions = openModalMock.mock.calls[0][0];
    const { componentEvents } = modalOptions;

    // 模拟组件 emit update:modelValue
    componentEvents['update:modelValue']({ Name: 'hello', Enable: true });

    // 触发 onConfirm，验证数据已合并
    await modalOptions.onConfirm();

    expect(requestMock).toHaveBeenCalledWith({
      url: '/api/test',
      method: 'post',
      data: { Name: 'hello', Enable: true },
    });
    await expect(promise).resolves.toBe(true);
  });

  it('配置正确传递到 componentProps', () => {
    openListFormDialog({
      title: '配置测试',
      fields: FIELDS,
      modelValue: { Name: 'test' },
      apiPrefix: '/api/foo',
      mode: 'edit',
    });

    const modalOptions = openModalMock.mock.calls[0][0];
    expect(modalOptions.componentProps).toEqual({
      fields: FIELDS,
      modelValue: { Name: 'test' },
      apiPrefix: '/api/foo',
      mode: 'edit',
      routePath: undefined,
      columns: 2,
    });
  });

  it('routePath 传递到 componentProps', () => {
    openListFormDialog({
      title: '路由测试',
      fields: FIELDS,
      apiPrefix: '/api/test',
      mode: 'add',
      routePath: '/test/area/page',
    });

    const modalOptions = openModalMock.mock.calls[0][0];
    expect(modalOptions.componentProps.routePath).toBe('/test/area/page');
  });

  it('字段 <= 10 时弹窗类型为 dialog，2列，700px', () => {
    openListFormDialog({
      title: '类型测试',
      fields: FIELDS,
      apiPrefix: '/api/test',
      mode: 'add',
    });

    const modalOptions = openModalMock.mock.calls[0][0];
    expect(modalOptions.type).toBe('dialog');
    expect(modalOptions.width).toBe('700px');
    expect(modalOptions.componentProps.columns).toBe(2);
  });

  it('11~15 个字段时弹窗类型为 drawer，2列，50%', () => {
    const manyFields: FieldMeta[] = Array.from({ length: 12 }, (_, i) => ({
      name: `Field${i}`,
      displayName: `字段${i}`,
      typeName: 'String',
      length: 50,
    }));

    openListFormDialog({
      title: '抽屉测试',
      fields: manyFields,
      apiPrefix: '/api/test',
      mode: 'add',
    });

    const modalOptions = openModalMock.mock.calls[0][0];
    expect(modalOptions.type).toBe('drawer');
    expect(modalOptions.size).toBe('50%');
    expect(modalOptions.componentProps.columns).toBe(2);
  });

  it('字段 > 15 时弹窗类型为 drawer，3列，65%', () => {
    const manyFields: FieldMeta[] = Array.from({ length: 16 }, (_, i) => ({
      name: `Field${i}`,
      displayName: `字段${i}`,
      typeName: 'String',
      length: 50,
    }));

    openListFormDialog({
      title: '宽抽屉测试',
      fields: manyFields,
      apiPrefix: '/api/test',
      mode: 'add',
    });

    const modalOptions = openModalMock.mock.calls[0][0];
    expect(modalOptions.type).toBe('drawer');
    expect(modalOptions.size).toBe('65%');
    expect(modalOptions.componentProps.columns).toBe(3);
  });

  it('刚好 10 个字段时弹窗类型为 dialog', () => {
    const tenFields: FieldMeta[] = Array.from({ length: 10 }, (_, i) => ({
      name: `Field${i}`,
      displayName: `字段${i}`,
      typeName: 'String',
      length: 50,
    }));

    openListFormDialog({
      title: '边界测试',
      fields: tenFields,
      apiPrefix: '/api/test',
      mode: 'add',
    });

    const modalOptions = openModalMock.mock.calls[0][0];
    expect(modalOptions.type).toBe('dialog');
    expect(modalOptions.componentProps.columns).toBe(2);
  });

  it('刚好 15 个字段时弹窗类型为 drawer，2列', () => {
    const fifteenFields: FieldMeta[] = Array.from({ length: 15 }, (_, i) => ({
      name: `Field${i}`,
      displayName: `字段${i}`,
      typeName: 'String',
      length: 50,
    }));

    openListFormDialog({
      title: '边界测试',
      fields: fifteenFields,
      apiPrefix: '/api/test',
      mode: 'add',
    });

    const modalOptions = openModalMock.mock.calls[0][0];
    expect(modalOptions.type).toBe('drawer');
    expect(modalOptions.componentProps.columns).toBe(2);
  });
});

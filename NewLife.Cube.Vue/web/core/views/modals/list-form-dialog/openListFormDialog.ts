/**
 * 命令式弹窗：打开列表页新增/编辑表单弹窗
 *
 * 使用 useModal 命令式 API，替代模板中手写 el-dialog + FormPage 的方式。
 * 表单内容通过 ListFormDialog 组件渲染，确认时自动调用后端 API 保存。
 */
import { reactive } from 'vue';
import { useModal } from '@newlifex/cube-vue/core/composables/useModal';
import { serializeSubmitModel } from '@newlifex/cube-vue/core/utils/fieldControl';
import request from '@newlifex/cube-vue/core/utils/request';
import { ElMessage } from 'element-plus';
import ListFormDialog from './ListFormDialog.vue';
import type { FieldMeta } from '@newlifex/cube-vue/core/types/field';

export interface OpenListFormDialogOptions {
  /** 弹窗标题 */
  title: string;
  /** 表单字段 */
  fields: FieldMeta[];
  /** 表单数据（编辑模式传入） */
  modelValue?: Record<string, unknown>;
  /** API 前缀（用于 LOV 等后端请求） */
  apiPrefix?: string;
  /** 新增/编辑 */
  mode: 'add' | 'edit';
  /** 保存成功后的回调（如刷新列表） */
  onSuccess?: () => void;
  /** 路由路径，用于 Section 覆盖机制查找对应覆盖组件 */
  routePath?: string;
}

/**
 * 打开列表页新增/编辑表单弹窗
 *
 * 命令式调用，无需在模板中声明弹窗组件。
 * 确认时自动调用后端 API 保存，成功后执行 onSuccess 回调。
 *
 * @param options - 弹窗配置
 * @returns Promise<boolean> - true 表示保存成功，false 表示取消
 *
 * @example
 * ```typescript
 * const ok = await openListFormDialog({
 *   title: '新增用户',
 *   fields: formFields,
 *   apiPrefix: '/api/user',
 *   mode: 'add',
 *   onSuccess: () => fetchList(),
 * });
 * ```
 */
export function openListFormDialog(options: OpenListFormDialogOptions): Promise<boolean> {
  const { openModal } = useModal();

  return new Promise<boolean>((resolve) => {
    // 表单数据副本，避免修改原始数据。
    // 必须是 reactive：openModal 对 options 做了 markRaw，且 componentEvents 的
    // Object.assign 回写只有落在响应式对象上才能触发弹窗内容重渲染；
    // 否则 FormContent 的受控 :model-value 永不更新，Element Plus 会在下一次
    // 输入同步时把 DOM 值重置回旧值，表现为"输入框打不进字"。
    const formData = reactive<Record<string, unknown>>({ ...(options.modelValue ?? {}) });

    // 根据字段数量自动推断弹窗类型和列数
    //   ≤ 10: dialog，2列，700px
    //   > 10: drawer，2列，50%
    //   > 15: drawer，3列，65%（更宽适配三列布局）
    const fieldCount = options.fields.length;
    const modalType = fieldCount > 10 ? 'drawer' : 'dialog';
    const columns = fieldCount > 15 ? 3 : 2;
    const drawerSize = fieldCount > 15 ? '65%' : '50%';

    openModal({
      title: options.title,
      type: modalType,
      width: modalType === 'dialog' ? '700px' : undefined,
      size: modalType === 'drawer' ? drawerSize : undefined,
      component: ListFormDialog,
      componentProps: {
        fields: options.fields,
        modelValue: formData,
        apiPrefix: options.apiPrefix,
        mode: options.mode,
        routePath: options.routePath,
        columns,
      },
      componentEvents: {
        'update:modelValue': (val: unknown) => {
          Object.assign(formData, val as Record<string, unknown>);
        },
      },
      destroyOnClose: true,
      onConfirm: async () => {
        try {
          // 多选字段序列化（数组 → 逗号分隔字符串）
          const data = serializeSubmitModel(formData, options.fields);
          const url = options.apiPrefix ?? '';
          if (options.mode === 'edit') {
            await request({ url, method: 'put', data });
            ElMessage.success('更新成功');
          } else {
            await request({ url, method: 'post', data });
            ElMessage.success('新增成功');
          }
          options.onSuccess?.();
          resolve(true);
        } catch (err: any) {
          // 返回 false 阻止弹窗关闭，让用户修正后重试
          console.error('[openListFormDialog] 保存失败:', err);
          return false;
        }
      },
      onCancel: () => {
        resolve(false);
      },
      onClosed: () => {
        resolve(false);
      },
    });
  });
}
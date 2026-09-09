/**
 * 命令式弹窗：打开列表页新增 / 编辑 / 查看表单弹窗
 *
 * 使用 useModal 命令式 API，替代模板中手写 el-dialog + FormPage 的方式。
 * 表单内容通过 ListFormDialog 组件渲染，确认时自动调用后端 API 保存。
 *
 * ## 回显策略（不再接收整行对象）
 *
 * 编辑 / 查看只接收记录主键 `id`，弹窗内自行调用详情接口拉取完整数据回显。
 * 原因：行对象是「列表投影」——只包含列表配置里出现的字段，且值常为显示态
 * （LOV 已翻译成标签、日期已格式化、多选已合并），拿它回显会丢字段、也写坏值。
 * 详情接口返回的才是完整、原始、可提交的记录。
 *
 * 详情接口统一走 api-core 的 `cubeApi.page.getDetail(type, id)`，
 * 它请求 `GET {type}/Detail?id=`，由后端 `ApiEntityController.Detail3`
 * （`[HttpGet("Detail")]`，id 取 QueryString）承接。
 *
 * ⚠️ 后端必须同时保留两条详情路由，二者等价，仅取 id 的方式不同：
 *   - `GET {apiPrefix}/Detail?id=` → Detail3（Cube 风格，供 api-core / 魔方前端使用）
 *   - `GET {apiPrefix}/{id}`       → Detail2（REST 风格，路由里已有历史调用方）
 * 只用 api-core 时，`{type}` 传 `apiPrefix`（`/api/ioc/{area}/{controller}`）即可：
 * api-core 内部 `resolveRequestUrl` 会剥离 url 自带的 `/api` 再统一补回，
 * 传带或不带 `/api` 前缀都能得到同一最终地址。
 *
 * ## 保存
 *
 * 提交前做必填校验（FormContent 只渲染星号、不校验），多选字段序列化后提交；
 * 编辑模式若详情未携带主键，按 `idKey` 兜底补上，保证 PUT 能定位记录。
 */
import { reactive, shallowReactive } from 'vue';
import { useModal } from '@newlifex/cube-vue/core/composables/useModal';
import { serializeSubmitModel, isRequiredField } from '@newlifex/cube-vue/core/utils/fieldControl';
import { getValueByKey, resolveKey } from '@newlifex/cube-vue/core/utils/url';
import request from '@newlifex/cube-vue/core/utils/request';
// cubeApi 是 useCubeApi 的默认导出（内部已在配置层完成实例化与错误处理挂载）
import cubeApi from '@newlifex/cube-vue/core/composables/useCubeApi';
// ApiError = 后端返回 code != 0 的业务错误（api-core 已统一提示），用于区分网络/HTTP 错误
import { ApiError } from '@newlifex/api-core';
import { ElMessage } from 'element-plus';
import ListFormDialog from './ListFormDialog.vue';
import type { FieldMeta } from '@newlifex/cube-vue/core/types/field';

/** 弹窗模式：新增 / 编辑 / 查看（只读） */
export type ListFormDialogMode = 'add' | 'edit' | 'view';

/** 主键在行数据与提交体中的默认字段名 */
const DEFAULT_ID_KEY = 'id';

export interface OpenListFormDialogOptions {
  /** 弹窗标题 */
  title: string;
  /** 表单字段 */
  fields: FieldMeta[];
  /** 新增 / 编辑 / 查看，默认 'add' */
  mode?: ListFormDialogMode;
  /**
   * 记录主键值。edit / view 模式必填 —— 弹窗据此调用详情接口回显，
   * 不再接收整行对象（行是列表投影，字段不全且值为显示态）。
   */
  id?: string | number;
  /**
   * 主键字段名，默认 'id'。
   * 用途：提交时兜底 —— 若详情数据未携带主键，则在提交体中补上，保证 PUT 能定位记录。
   */
  idKey?: string;
  /** 表单初值（新增模式的默认值；edit / view 模式会被详情数据覆盖） */
  modelValue?: Record<string, unknown>;
  /** API 前缀（详情 / 保存 / LOV 等后端请求共用） */
  apiPrefix?: string;
  /** 保存成功后的回调（如刷新列表） */
  onSuccess?: () => void;
  /** 路由路径，用于 Section 覆盖机制查找对应覆盖组件 */
  routePath?: string;
}

/**
 * 提交前必填校验。
 *
 * FormContent 只按 `isRequiredField` 渲染星号、不做校验，这里补上，
 * 避免必填项为空直接打到后端再弹 fieldErrors。
 *
 * @param model 表单数据
 * @param fields 字段元数据
 * @returns 第一个未填写的必填字段显示名；全部通过时返回 null
 */
function findMissingRequired(model: Record<string, unknown>, fields: FieldMeta[]): string | null {
  for (const field of fields) {
    if (!isRequiredField(field)) continue;
    const value = getValueByKey(model, field.name);
    const empty =
      value === undefined ||
      value === null ||
      value === '' ||
      (Array.isArray(value) && value.length === 0);
    if (empty) return field.displayName || field.name;
  }
  return null;
}

/**
 * 打开列表页新增 / 编辑 / 查看表单弹窗
 *
 * 命令式调用，无需在模板中声明弹窗组件。
 * 确认时自动调用后端 API 保存，成功后执行 onSuccess 回调。
 *
 * @param options - 弹窗配置
 * @returns Promise<boolean> - true 表示保存成功，false 表示取消 / 关闭 / 失败
 *
 * @example 新增
 * ```typescript
 * const ok = await openListFormDialog({
 *   title: '新增用户',
 *   fields: formFields,
 *   apiPrefix: '/api/Admin/User',
 *   mode: 'add',
 *   onSuccess: () => fetchList(),
 * });
 * ```
 *
 * @example 编辑（只传 id，不传行对象）
 * ```typescript
 * await openListFormDialog({
 *   title: '编辑用户',
 *   fields: formFields,
 *   apiPrefix: '/api/Admin/User',
 *   mode: 'edit',
 *   id: row.id,          // idKey 默认 'id'，主键字段名不同时显式传入
 *   onSuccess: () => fetchList(),
 * });
 * ```
 *
 * @example 查看（只读）
 * ```typescript
 * await openListFormDialog({ title: '查看用户', fields: detailFields, apiPrefix, mode: 'view', id: row.id });
 * ```
 */
export function openListFormDialog(options: OpenListFormDialogOptions): Promise<boolean> {
  const mode: ListFormDialogMode = options.mode ?? 'add';
  const idKey = options.idKey ?? DEFAULT_ID_KEY;
  const readonly = mode === 'view';
  // 编辑 / 查看需要拉详情；新增没有 id 可查
  const needsDetail = mode !== 'add';
  const id = options.id;

  const label = readonly ? '查看' : '编辑';
  // 编辑/查看拿不到 id 时直接失败：打开一个回显不出来的空表单只会误导用户
  if (needsDetail && (id === undefined || id === null || id === '')) {
    ElMessage.error(`${label}失败：缺少记录 id`);
    return Promise.resolve(false);
  }
  // 保存与拉详情都依赖 apiPrefix，缺了只能给出明确提示而非发一个错误地址的请求
  if (!options.apiPrefix) {
    ElMessage.error(`${label}失败：缺少接口前缀`);
    return Promise.resolve(false);
  }

  const { openModal } = useModal();

  return new Promise<boolean>((resolve) => {
    // 保存成功 / 取消 / 关闭三条路径都会到达这里，Promise 只允许敲定一次
    let settled = false;
    const finish = (ok: boolean) => {
      if (settled) return;
      settled = true;
      resolve(ok);
    };

    // 表单数据副本，避免修改原始数据。
    // 必须是 reactive：openModal 对 options 做了 markRaw，且 componentEvents 的
    // Object.assign 回写只有落在响应式对象上才能触发弹窗内容重渲染；
    // 否则 FormContent 的受控 :model-value 永不更新，Element Plus 会在下一次
    // 输入同步时把 DOM 值重置回旧值，表现为"输入框打不进字"。
    const formData = reactive<Record<string, unknown>>({ ...(options.modelValue ?? {}) });

    // 新增模式下，Boolean 开关字段默认注入 false（“否”），
    // 用户未操作开关也能以 false 正常提交，避免缺省 undefined 导致必填误报或后端收 null
    if (mode === 'add' && options.fields) {
      for (const f of options.fields) {
        if (f.typeName === 'Boolean' && (formData[f.name] === undefined || formData[f.name] === null)) {
          formData[f.name] = false;
        }
      }
    }

    // 内容组件 props 用 shallowReactive 承载：ModalContainer 渲染时会展开
    // componentProps 并读取各属性，顶层字段（loading）变化即可触发重渲染；
    // 用 shallow 是为了不让 fields 数组被深度代理污染。
    const contentProps = shallowReactive<Record<string, unknown>>({
      fields: options.fields,
      modelValue: formData,
      apiPrefix: options.apiPrefix,
      mode,
      routePath: options.routePath,
      columns: 2,
      loading: needsDetail,
    });

    // 根据字段数量自动推断弹窗类型和列数
    //   ≤ 10: dialog，2列，700px
    //   > 10: drawer，2列，50%
    //   > 15: drawer，3列，65%（更宽适配三列布局）
    const fieldCount = options.fields.length;
    const modalType = fieldCount > 10 ? 'drawer' : 'dialog';
    contentProps.columns = fieldCount > 15 ? 3 : 2;

    /**
     * 拉取详情回填表单（api-core 的 getDetail）。
     *
     * 请求 `GET {apiPrefix}/Detail?id=`，由后端 Detail3 承接；返回已解包的
     * `ApiResponse<T>`，取 `.data` 即为记录本体。
     * 与 core/views/form.vue 一致，把字符串 "true"/"false" 归一为布尔，
     * 避免开关控件取值异常。
     */
    async function loadDetail(): Promise<void> {
      try {
        // api-core 内部会补/去重 /api 前缀，故 type 直接传 apiPrefix 即可
        const res = await cubeApi.page.getDetail<Record<string, unknown>>(options.apiPrefix!, id!);
        const data = (res?.data ?? {}) as Record<string, unknown>;
        const normalized = Object.fromEntries(
          Object.entries(data).map(([k, v]) => [k, v === 'true' ? true : v === 'false' ? false : v]),
        );
        Object.assign(formData, normalized);
      } catch (err) {
        // 保留弹窗并提示，让用户能关闭重试，而不是静默显示空表单。
        // 业务错误（ApiError）已由 api-core 的 onBusinessError 弹出后端原文，此处不重复提示。
        console.error('[openListFormDialog] 详情加载失败:', err);
        if (!(err instanceof ApiError)) {
          ElMessage.error('详情加载失败，请重试');
        }
      } finally {
        contentProps.loading = false;
      }
    }

    const modal = openModal({
      title: options.title,
      type: modalType,
      width: modalType === 'dialog' ? '700px' : undefined,
      size: modalType === 'drawer' ? (fieldCount > 15 ? '65%' : '50%') : undefined,
      component: ListFormDialog,
      componentProps: contentProps,
      componentEvents: {
        'update:modelValue': (val: unknown) => {
          Object.assign(formData, val as Record<string, unknown>);
        },
      },
      destroyOnClose: true,
      // 查看模式：表单只读，底部只给一个关闭按钮
      ...(readonly
        ? {
            footerButtons: [
              {
                text: '关闭',
                type: 'default' as const,
                onClick: (m: { close: () => void }) => m.close(),
              },
            ],
          }
        : {}),
      onConfirm: readonly
        ? undefined
        : async () => {
            if (!options.apiPrefix) {
              ElMessage.error('缺少接口前缀，无法保存');
              return false;
            }

            const missing = findMissingRequired(formData, options.fields);
            if (missing) {
              ElMessage.warning(`${missing}不能为空`);
              return false;
            }

            try {
              // 多选字段序列化（数组 → 逗号分隔字符串）
              const data = serializeSubmitModel(formData, options.fields);

              // 编辑模式兜底补主键：详情接口若未返回主键字段，PUT 无法定位记录
              if (mode === 'edit') {
                const realKey = resolveKey(data, idKey);
                if (data[realKey] === undefined || data[realKey] === null || data[realKey] === '') {
                  data[realKey] = id;
                }
              }

              if (mode === 'edit') {
                await request({ url: options.apiPrefix, method: 'put', data });
                ElMessage.success('更新成功');
              } else {
                await request({ url: options.apiPrefix, method: 'post', data });
                ElMessage.success('新增成功');
              }
              options.onSuccess?.();
              finish(true);
            } catch (err) {
              // 返回 false 阻止弹窗关闭，让用户修正后重试
              console.error('[openListFormDialog] 保存失败:', err);
              return false;
            }
          },
      onCancel: () => {
        finish(false);
      },
      onClosed: () => {
        finish(false);
      },
    });

    if (needsDetail) {
      void loadDetail();
    }

    return modal;
  });
}

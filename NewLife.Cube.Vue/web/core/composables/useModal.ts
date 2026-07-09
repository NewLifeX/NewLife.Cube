/**
 * 命令式弹窗 API - useModal
 *
 * 提供 `openModal()` / `closeModal()` / `closeAll()` 等命令式弹窗管理能力，
 * 支持 dialog 和 drawer 两种类型，内容可为组件、渲染函数或表单配置。
 *
 * 核心特性：
 * - 命令式调用，无需在模板中声明弹窗组件
 * - 自动根据表单复杂度推断 dialog/drawer 类型
 * - 支持三种内容传递方式：component / render / config
 * - 完整透传 ElDialog / ElDrawer props
 * - 使用 Teleport 挂载到 body，共享主应用上下文
 *
 * @example 基本使用
 * ```typescript
 * import { useModal } from '@newlifex/cube-vue/core/composables/useModal';
 *
 * const { openModal } = useModal();
 * const modal = openModal({
 *   title: '编辑用户',
 *   component: EditUserForm,
 *   componentProps: { userId: 1 },
 *   onConfirm: (data) => { console.log('保存:', data); },
 * });
 * ```
 *
 * @example 自动类型推断（复杂表单→drawer，简单表单→dialog）
 * ```typescript
 * openModal({
 *   type: 'auto',
 *   title: '新增产品',
 *   config: formColumns, // ColumnConfig[]
 *   modelValue: formData,
 * });
 * ```
 *
 * @example 异步组件
 * ```typescript
 * openModal({
 *   title: '详情',
 *   component: () => import('./DetailForm.vue'),
 *   componentProps: { id: 1 },
 * });
 * ```
 *
 * @example 渲染函数模式
 * ```typescript
 * openModal({
 *   title: '预览',
 *   render: (ctx) => h('div', `Hello ${ctx.props.name}`),
 *   componentProps: { name: 'World' },
 * });
 * ```
 */
import {
  type App,
  type Component,
  type InjectionKey,
  type RenderFunction,
  type VNode,
  h,
  inject,
  markRaw,
  provide,
  reactive,
  ref,
  computed,
  type Ref,
  defineAsyncComponent,
  nextTick,
} from 'vue';
import type { ColumnConfig } from '../../src/components/form/model/form';

// ─── 类型定义 ───────────────────────────────────────────────────

/** 弹窗类型：dialog 对话框 / drawer 抽屉 */
export type ModalType = 'dialog' | 'drawer';

/** 弹窗唯一标识 */
export type ModalId = string;

/** 弹窗内容：同步组件、异步导入函数或渲染函数 */
export type ModalContent = Component | (() => Promise<{ default: Component }>) | RenderFunction;

/** 表单复杂度评分 */
export interface FormComplexity {
  /** 字段数量（已过滤隐藏字段） */
  fieldCount: number;
  /** 分组数量 */
  groupCount: number;
  /** 综合评分 0-1，越高越复杂 */
  score: number;
  /** 建议的弹窗类型 */
  suggestedType: ModalType;
}

/** 弹窗渲染上下文（传给 render 函数） */
export interface ModalRenderContext<P = Record<string, unknown>> {
  /** 传入的 props */
  props: P;
  /** 当前可见状态 */
  visible: Ref<boolean>;
  /** 关闭弹窗 */
  close: () => void;
  /** 确认并返回数据 */
  confirm: (data?: unknown) => void;
  /** 设置确认按钮 loading 状态 */
  setConfirmLoading: (loading: boolean) => void;
}

/** 弹窗底部按钮配置 */
export interface FooterButton {
  /** 按钮文本 */
  text: string;
  /** 按钮类型 */
  type?: 'primary' | 'default' | 'danger' | 'warning' | 'info' | 'success';
  /** 点击回调 */
  onClick: (modal: ModalInstance) => void | Promise<void>;
  /** 是否显示 loading */
  loading?: boolean;
  /** 是否禁用 */
  disabled?: boolean;
}

/** 弹窗基础选项（所有类型共有） */
export interface ModalBaseOptions {
  /** 弹窗标题 */
  title?: string;
  /** 弹窗类型：dialog / drawer / auto（自动推断），默认 'auto' */
  type?: ModalType | 'auto';
  /** 是否显示底部操作栏，默认 true */
  showFooter?: boolean;
  /** 底部按钮数组（自定义按钮），不设置则显示默认 confirm/cancel */
  footerButtons?: FooterButton[];
  /** 确认按钮文本，默认 '确定' */
  confirmText?: string;
  /** 取消按钮文本，默认 '取消' */
  cancelText?: string;
  /** 确认按钮 loading 状态 */
  confirmLoading?: boolean;
  /** 弹窗关闭后的回调 */
  onClosed?: () => void;
  /** 点击确认按钮时的回调，返回 false 阻止关闭 */
  onConfirm?: (data?: unknown) => boolean | void | Promise<boolean | void>;
  /** 点击取消按钮时的回调，返回 false 阻止关闭 */
  onCancel?: () => boolean | void;
  /** 点击遮罩层时的回调，返回 false 阻止关闭 */
  onClickModal?: () => boolean | void;
  /** 关闭前的钩子（透传给 ElDialog/ElDrawer） */
  beforeClose?: (done: () => void) => void;

  // ─── ElDialog 专有 props ──────────────────────────────────────
  /** 对话框宽度，默认 '50%' */
  width?: string | number;
  /** 是否全屏 */
  fullscreen?: boolean;
  /** 是否居中 */
  alignCenter?: boolean;
  /** 是否追加到 body */
  appendToBody?: boolean;
  /** 自定义动画 */
  transition?: string;

  // ─── ElDrawer 专有 props ──────────────────────────────────────
  /** 抽屉尺寸，默认 '50%' */
  size?: string | number;
  /** 抽屉方向 rtl/ltr/ttb/btt，默认 'rtl' */
  direction?: 'rtl' | 'ltr' | 'ttb' | 'btt';
  /** 是否可拖动调整尺寸 */
  resizable?: boolean;
  /** 是否显示头部 */
  withHeader?: boolean;

  // ─── ElDialog / ElDrawer 共有 props ──────────────────────────
  /** 关闭时是否销毁，默认 true */
  destroyOnClose?: boolean;
  /** 点击遮罩层是否关闭 */
  closeOnClickModal?: boolean;
  /** 按 ESC 是否关闭 */
  closeOnPressEscape?: boolean;
  /** 是否锁定滚动 */
  lockScroll?: boolean;
  /** 是否显示遮罩层 */
  modal?: boolean;
  /** 遮罩层自定义类名 */
  modalClass?: string;
  /** z-index */
  zIndex?: number;
  /** 打开延迟（ms） */
  openDelay?: number;
  /** 关闭延迟（ms） */
  closeDelay?: number;
  /** 是否显示关闭按钮 */
  showClose?: boolean;

  /** 额外透传 props（未列出的 ElDialog/ElDrawer props） */
  attrs?: Record<string, unknown>;
}

/** 组件模式弹窗选项 */
export interface ComponentModalOptions<P = Record<string, unknown>> extends ModalBaseOptions {
  /** 内容组件（同步组件或异步导入函数） */
  component: ModalContent;
  /** 传递给内容组件的 props */
  componentProps?: P;
  /** 内容组件 emit 事件绑定 */
  componentEvents?: Record<string, (...args: unknown[]) => void>;
  /** 表单配置（互斥，与 component/render 三选一） */
  config?: never;
  /** 渲染函数（互斥） */
  render?: never;
  /** 表单 modelValue */
  modelValue?: never;
  /** 表单提交成功回调 */
  onSubmitSuccess?: never;
}

/** 渲染函数模式弹窗选项 */
export interface RenderModalOptions<P = Record<string, unknown>> extends ModalBaseOptions {
  /** 渲染函数 */
  render: (ctx: ModalRenderContext<P>) => VNode | VNode[];
  /** 传递给渲染函数的 props */
  componentProps?: P;
  /** 组件模式（互斥） */
  component?: never;
  /** 表单配置（互斥） */
  config?: never;
  /** 表单 modelValue */
  modelValue?: never;
  /** 表单提交成功回调 */
  onSubmitSuccess?: never;
  /** 内容组件 emit（互斥，render 模式不存在） */
  componentEvents?: never;
}

/** 表单配置模式弹窗选项 */
export interface FormModalOptions extends ModalBaseOptions {
  /** 表单列配置 */
  config: ColumnConfig[];
  /** 表单数据 */
  modelValue?: Record<string, unknown>;
  /** 表单提交成功回调 */
  onSubmitSuccess?: (data: Record<string, unknown>) => void;
  /** 组件模式（互斥） */
  component?: never;
  /** 渲染函数（互斥） */
  render?: never;
  /** 内容组件 props（互斥，表单模式自动管理） */
  componentProps?: never;
  /** 内容组件 emit（互斥） */
  componentEvents?: never;
}

/** 弹窗选项联合类型 */
export type ModalOptions<P = Record<string, unknown>> =
  | ComponentModalOptions<P>
  | RenderModalOptions<P>
  | FormModalOptions;

/** 内部弹窗条目（用于追踪所有弹窗） */
export interface ModalItem<P = Record<string, unknown>> {
  /** 唯一标识 */
  id: ModalId;
  /** 弹窗类型 */
  type: ModalType;
  /** 选项 */
  options: ModalOptions<P>;
  /** 当前可见状态 */
  visible: boolean;
  /** 确认按钮 loading 状态 */
  confirmLoading: boolean;
  /** 表单数据（仅 config 模式） */
  formData?: Record<string, unknown>;
  /** 内容组件（已解析） */
  resolvedComponent?: Component;
  /** 渲染函数结果缓存 */
  renderCache?: VNode | VNode[];
  /** 是否正在关闭中（防止重入） */
  closing?: boolean;
}

/** 弹窗实例（openModal 返回值） */
export interface ModalInstance<P = Record<string, unknown>> {
  /** 弹窗唯一标识 */
  id: ModalId;
  /** 弹窗类型 */
  type: ModalType;
  /** 可见状态（响应式） */
  visible: Ref<boolean>;
  /** 确认按钮 loading（响应式） */
  confirmLoading: Ref<boolean>;
  /** 关闭弹窗 */
  close: () => void;
  /** 触发确认 */
  confirm: (data?: unknown) => void;
  /** 设置确认按钮 loading */
  setConfirmLoading: (loading: boolean) => void;
  /** 表单数据（仅 config 模式，响应式） */
  formData?: Ref<Record<string, unknown>>;
}

/** useModal 返回值 */
export interface UseModalReturn {
  /** 打开弹窗 */
  openModal: <P extends Record<string, unknown> = Record<string, unknown>>(options: ModalOptions<P>) => ModalInstance<P>;
  /** 关闭指定弹窗 */
  closeModal: (id: ModalId) => void;
  /** 关闭所有弹窗 */
  closeAll: () => void;
  /** 获取所有弹窗实例 */
  getModals: () => ModalInstance[];
  /** 全局默认配置 */
  config: {
    defaultType: ModalType | 'auto';
    defaultWidth: string;
    defaultSize: string;
    defaultDirection: 'rtl' | 'ltr' | 'ttb' | 'btt';
  };
}

// ─── Injection Key ───────────────────────────────────────────────

/** 弹窗状态注入 key */
export const ModalKey: InjectionKey<{
  openModal: <P extends Record<string, unknown> = Record<string, unknown>>(options: ModalOptions<P>) => ModalInstance<P>;
  closeModal: (id: ModalId) => void;
  closeAll: () => void;
  getModals: () => ModalInstance[];
}> = Symbol('@newlifex/cube-vue.use-modal');

// ─── 复杂度评估与类型推断 ─────────────────────────────────────

/**
 * 评估表单复杂度
 *
 * 根据表单列配置计算字段数和分组数，并给出综合评分。
 * 评估时会自动过滤 `show: false` 和 `if` 条件为假的字段。
 *
 * @param config - 表单列配置数组
 * @returns 表单复杂度信息
 *
 * @example
 * ```typescript
 * const complexity = evaluateFormComplexity(formColumns);
 * // { fieldCount: 12, groupCount: 3, score: 0.7, suggestedType: 'drawer' }
 * ```
 */
export function evaluateFormComplexity(config: ColumnConfig[]): FormComplexity {
  // 过滤掉隐藏字段
  const visibleFields = config.filter((item) => {
    // show: false 的字段不计入
    if (item.show === false) return false;
    // if 条件为假的字段不计入
    if (typeof item.if === 'boolean' && !item.if) return false;
    return true;
  });

  const fieldCount = visibleFields.length;

  // 计算分组数
  const groups = new Set<string>();
  for (const field of visibleFields) {
    if (field.group) {
      groups.add(field.group);
    }
  }
  const groupCount = groups.size > 0 ? groups.size : 1;

  // 计算评分（0-1）
  // 字段数权重 60%：fieldCount >= 15 时达到最大值 0.6
  const fieldScore = Math.min(fieldCount / 15, 1) * 0.6;
  // 分组数权重 40%：groupCount >= 4 时达到最大值 0.4
  const groupScore = Math.min(groupCount / 4, 1) * 0.4;
  const score = Math.round((fieldScore + groupScore) * 100) / 100;

  // 推断弹窗类型
  const suggestedType: ModalType = fieldCount > 10 || groupCount > 2 ? 'drawer' : 'dialog';

  return { fieldCount, groupCount, score, suggestedType };
}

/**
 * 解析弹窗类型
 *
 * 处理 `type: 'auto'` 的自动推断逻辑，根据表单列配置自动选择 dialog 或 drawer。
 *
 * @param options - 弹窗选项
 * @returns 解析后的弹窗类型 'dialog' | 'drawer'
 *
 * @example
 * ```typescript
 * const type = resolveModalType({ type: 'auto', config: complexColumns });
 * // type === 'drawer'（复杂表单）
 *
 * const type2 = resolveModalType({ type: 'dialog', config: complexColumns });
 * // type2 === 'dialog'（显式指定覆盖）
 * ```
 */
export function resolveModalType(options: ModalOptions): ModalType {
  // 显式指定 dialog 或 drawer
  if (options.type === 'dialog' || options.type === 'drawer') {
    return options.type;
  }

  // auto 模式：根据表单配置推断
  if (options.config) {
    const complexity = evaluateFormComplexity(options.config);
    return complexity.suggestedType;
  }

  // 没有表单配置时默认使用 dialog
  return 'dialog';
}

/**
 * 解析内容组件，支持同步和异步导入
 */
export function resolveContent(content: Component | (() => Promise<{ default: Component }>)): Component {
  if (typeof content === 'function') {
    const result = (content as unknown as () => Promise<{ default: Component }>)();
    if (result && typeof result.then === 'function') {
      return defineAsyncComponent(content as unknown as () => Promise<{ default: Component }>);
    }
  }
  return content as Component;
}

// ─── 模块级单例 ─────────────────────────────────────────────────

/** 弹窗配置 */
const defaultConfig = {
  defaultType: 'auto' as ModalType | 'auto',
  defaultWidth: '50%',
  defaultSize: '50%',
  defaultDirection: 'rtl' as 'rtl' | 'ltr' | 'ttb' | 'btt',
};

/** 弹窗列表（追踪所有打开的弹窗，reactive 响应式） */
export const modals = reactive<ModalItem[]>([]);

/**
 * 生成唯一 ID
 */
function generateId(): ModalId {
  return `modal_${Date.now()}_${Math.random().toString(36).slice(2, 9)}`;
}

// ─── 核心 API ───────────────────────────────────────────────────

/**
 * 打开弹窗
 *
 * 命令式弹窗 API，支持三种内容传递方式：
 * - `component`：同步/异步组件
 * - `render`：渲染函数
 * - `config`：表单列配置
 *
 * 使用 Teleport 挂载到 body，共享主应用上下文。
 *
 * @param options - 弹窗选项
 * @returns 弹窗实例，包含 close / confirm / setConfirmLoading 等方法
 *
 * @example 组件模式
 * ```typescript
 * const modal = openModal({
 *   title: '编辑',
 *   component: MyForm,
 *   componentProps: { id: 1 },
 * });
 * ```
 *
 * @example 表单配置模式
 * ```typescript
 * const modal = openModal({
 *   type: 'auto',
 *   title: '新增',
 *   config: columns,
 *   modelValue: { name: '' },
 *   onSubmitSuccess: (data) => { ... },
 * });
 * ```
 */
function openModal<P extends Record<string, unknown> = Record<string, unknown>>(options: ModalOptions<P>): ModalInstance<P> {
  const id = generateId();
  const resolvedType = resolveModalType(options as ModalOptions);

  // 解析内容组件（如果是 component 模式）
  let resolvedComponent: Component | undefined;
  if ('component' in options && options.component) {
    resolvedComponent = resolveContent(options.component);
  }

  // 创建弹窗条目（visible 直接为 true，确保立即显示）
  const item: ModalItem = {
    id,
    type: resolvedType,
    options: markRaw(options) as unknown as ModalOptions<Record<string, unknown>>,
    visible: true,
    confirmLoading: options.confirmLoading ?? false,
    resolvedComponent: resolvedComponent ? markRaw(resolvedComponent) : undefined,
    closing: false,
  };

  // 表单模式：初始化表单数据
  if ('config' in options && options.config) {
    item.formData = reactive({ ...(options.modelValue ?? {}) }) as unknown as Record<string, unknown>;
  }

  // 添加到弹窗列表
  modals.push(item);

  // 构建响应式可见性引用（可写 computed）
  const visibleRef = computed<boolean>({
    get: () => item.visible,
    set: (val: boolean) => {
      if (!val && !item.closing) {
        closeModal(id);
      } else {
        item.visible = val;
      }
    },
  });

  // 构建响应式 confirmLoading 引用（可写 computed）
  const confirmLoadingRef = computed<boolean>({
    get: () => item.confirmLoading,
    set: (val: boolean) => { item.confirmLoading = val; },
  });

  // 构建响应式 formData 引用
  let formDataRef: Ref<Record<string, unknown>> | undefined;
  if (item.formData) {
    formDataRef = computed<Record<string, unknown>>({
      get: () => item.formData as Record<string, unknown>,
      set: (val: Record<string, unknown>) => {
        Object.assign(item.formData as Record<string, unknown>, val);
      },
    }) as Ref<Record<string, unknown>>;
  }

  // 返回实例
  return {
    id,
    type: resolvedType,
    visible: visibleRef,
    confirmLoading: confirmLoadingRef,
    close: () => closeModal(id),
    confirm: (data?: unknown) => {
      handleConfirm(item, data);
    },
    setConfirmLoading: (loading: boolean) => {
      item.confirmLoading = loading;
    },
    formData: formDataRef,
  } as ModalInstance<P>;
}

/**
 * 处理确认操作
 */
export function handleConfirm(item: ModalItem, data?: unknown): void {
  const o = item.options;
  // config 模式下返回表单数据
  if ('config' in o && o.config && item.formData) {
    if (o.onSubmitSuccess) o.onSubmitSuccess(item.formData);
    if (o.onConfirm) {
      const result = o.onConfirm(item.formData);
      handleConfirmResult(item, result);
    } else {
      closeModal(item.id);
    }
  } else if (o.onConfirm) {
    const result = o.onConfirm(data);
    handleConfirmResult(item, result);
  } else {
    closeModal(item.id);
  }
}

/**
 * 处理确认回调的返回值
 */
function handleConfirmResult(item: ModalItem, result: unknown): void {
  if (result === false) return;
  if (result instanceof Promise) {
    item.confirmLoading = true;
    result
      .then((res) => {
        if (res !== false) {
          closeModal(item.id);
        }
      })
      .finally(() => {
        item.confirmLoading = false;
      });
  } else {
    closeModal(item.id);
  }
}

/**
 * 关闭指定弹窗
 *
 * @param id - 弹窗唯一标识
 */
export function closeModal(id: ModalId): void {
  const item = modals.find((m) => m.id === id);
  if (!item || item.closing) return;

  const o = item.options;
  // 先触发 onCancel 回调
  if (o.onCancel) {
    const result = o.onCancel();
    if (result === false) return;
  }

  // 标记为关闭中，防止重入
  item.closing = true;
  // 设置 visible 为 false，触发关闭动画
  item.visible = false;
  // 等待动画结束后移除
  setTimeout(() => {
    const idx = modals.findIndex((m) => m.id === id);
    if (idx !== -1) {
      modals[idx].options.onClosed?.();
      modals.splice(idx, 1);
    }
  }, 300);
}

/**
 * 关闭所有弹窗
 */
function closeAll(): void {
  const ids = modals.map((m) => m.id);
  ids.forEach((id) => closeModal(id));
}

/**
 * 获取所有弹窗实例
 *
 * @returns 当前所有弹窗实例数组
 */
function getModals(): ModalInstance[] {
  return modals.map((item) => {
    const visibleRef = computed<boolean>({
      get: () => item.visible,
      set: (val: boolean) => {
        item.visible = val;
        if (!val) closeModal(item.id);
      },
    });
    const confirmLoadingRef = computed<boolean>({
      get: () => item.confirmLoading,
      set: (val: boolean) => { item.confirmLoading = val; },
    });
    const formDataRef = item.formData
      ? (computed<Record<string, unknown>>({
          get: () => item.formData as Record<string, unknown>,
          set: (val: Record<string, unknown>) => {
            Object.assign(item.formData as Record<string, unknown>, val);
          },
        }) as Ref<Record<string, unknown>>)
      : undefined;
    return {
      id: item.id,
      type: item.type,
      visible: visibleRef,
      confirmLoading: confirmLoadingRef,
      close: () => closeModal(item.id),
      confirm: (data?: unknown) => handleConfirm(item, data),
      setConfirmLoading: (loading: boolean) => { item.confirmLoading = loading; },
      formData: formDataRef,
    };
  });
}

// ─── useModal 组合式函数 ───────────────────────────────────────

/**
 * 获取命令式弹窗 API
 *
 * 全局单例模式，所有组件调用此函数获取的都是同一个弹窗管理器。
 * 使用 Teleport 挂载到 body，共享主应用上下文。
 *
 * @returns 包含 openModal / closeModal / closeAll / getModals 方法和 config 配置对象
 *
 * @example
 * ```typescript
 * // 在任意组件中
 * const { openModal, closeAll, config } = useModal();
 *
 * // 打开一个弹窗
 * const modal = openModal({ title: 'Hello', component: MyComponent });
 *
 * // 关闭所有弹窗
 * closeAll();
 *
 * // 修改全局默认配置
 * config.defaultType = 'drawer';
 * ```
 */
export function useModal(): UseModalReturn {
  return {
    openModal,
    closeModal,
    closeAll,
    getModals,
    config: defaultConfig,
  };
}

/**
 * 在应用初始化时将弹窗管理器注册到 provide 链
 *
 * 通常在 initApp 的 configure 函数中调用，确保所有子组件都能通过 inject 获取。
 *
 * @param app - Vue 应用实例
 *
 * @example
 * ```typescript
 * initApp((app) => {
 *   provideModal(app);
 * });
 * ```
 */
export function provideModal(app: App<Element>): void {
  const modalState = {
    openModal,
    closeModal,
    closeAll,
    getModals,
  };
  app.provide(ModalKey, modalState);
}

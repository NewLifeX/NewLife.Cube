<script lang="ts">
/**
 * 全局弹窗容器组件
 *
 * 使用 Teleport 将所有弹窗渲染到 body，共享主应用上下文。
 * 从 useModal 的 modals 响应式数组获取弹窗数据并渲染。
 *
 * 支持三种内容模式：
 * - component：组件模式（同步或异步组件）
 * - render：渲染函数模式
 * - config：表单配置模式
 */
import { defineComponent, h, computed, ref, Teleport, type VNode, type Component } from 'vue';
import { ElDialog, ElDrawer, ElButton } from 'element-plus';
import { modals, closeModal as closeModalFn, handleConfirm as handleConfirmFn } from '../composables/useModal';
import type {
  ModalItem,
  ComponentModalOptions,
  RenderModalOptions,
  FormModalOptions,
  ModalRenderContext,
  FooterButton,
} from '../composables/useModal';
import type { ColumnConfig } from '../../src/components/form/model/form';

export default defineComponent({
  name: 'ModalContainer',
  setup() {
    const contentRefs = ref<Record<string, unknown>>({});

    /**
     * 处理取消操作（由用户点击关闭按钮/遮罩层触发）
     */
    function handleCancel(item: ModalItem) {
      closeModalFn(item.id);
    }

    /**
     * 处理关闭动画结束事件
     */
    function handleClosed() {
      // 关闭动画结束后，由 closeModal 中的 setTimeout 处理移除
    }

    /**
     * 构建组件事件绑定
     */
    function buildComponentEvents(item: ModalItem): Record<string, (...args: unknown[]) => void> {
      const events: Record<string, (...args: unknown[]) => void> = {};
      const o = item.options as ComponentModalOptions;
      if (o.componentEvents) {
        for (const [key, value] of Object.entries(o.componentEvents)) {
          const eventName = key.startsWith('on') ? key : `on${key.charAt(0).toUpperCase()}${key.slice(1)}`;
          events[eventName] = value;
        }
      }
      return events;
    }

    /**
     * 构建渲染函数上下文
     */
    function buildRenderContext(item: ModalItem): ModalRenderContext {
      const o = item.options as RenderModalOptions;
      return {
        props: o.componentProps ?? {},
        visible: computed(() => item.visible) as unknown as import('vue').Ref<boolean>,
        close: () => closeModalFn(item.id),
        confirm: (d?: unknown) => handleConfirmFn(item, d),
        setConfirmLoading: (l: boolean) => { item.confirmLoading = l; },
      };
    }

    /**
     * 构建表单字段
     */
    function buildFormFields(item: ModalItem): VNode[] {
      const o = item.options as FormModalOptions;
      const fd = item.formData ?? {};
      return o.config.filter((c: ColumnConfig) => {
        if (c.show === false) return false;
        if (typeof (c as { if?: boolean }).if === 'boolean' && !(c as { if?: boolean }).if) return false;
        return true;
      }).map((c: ColumnConfig) => {
        const prop = c.prop.toString();
        const fieldProps = (c as { props?: Record<string, unknown> }).props || {};
        const modelValue = (fd as Record<string, unknown>)[prop];
        const updateModel = (v: unknown) => { (fd as Record<string, unknown>)[prop] = v; };

        let fieldVNode: VNode;
        const componentType = c.component || 'input';

        switch (componentType) {
          case 'select': {
            const options = (fieldProps.options as Array<{ label: string; value: unknown }>) || [];
            fieldVNode = h('el-select', {
              modelValue,
              'onUpdate:modelValue': updateModel,
              ...fieldProps,
            }, {
              default: () => options.map((opt) => h('el-option', {
                label: opt.label,
                value: opt.value,
              })),
            });
            break;
          }
          case 'input': {
            fieldVNode = h('el-input', {
              modelValue,
              'onUpdate:modelValue': updateModel,
              ...fieldProps,
            });
            break;
          }
          case 'switch': {
            fieldVNode = h('el-switch', {
              modelValue,
              'onUpdate:modelValue': updateModel,
              ...fieldProps,
            });
            break;
          }
          case 'inputNumber': {
            fieldVNode = h('el-input-number', {
              modelValue,
              'onUpdate:modelValue': updateModel,
              ...fieldProps,
            });
            break;
          }
          case 'radioGroup': {
            const options = (fieldProps.options as Array<{ label: string; value: unknown }>) || [];
            fieldVNode = h('el-radio-group', {
              modelValue,
              'onUpdate:modelValue': updateModel,
              ...fieldProps,
            }, {
              default: () => options.map((opt) => h('el-radio', {
                label: opt.value,
              }, () => opt.label)),
            });
            break;
          }
          case 'checkboxGroup': {
            const options = (fieldProps.options as Array<{ label: string; value: unknown }>) || [];
            fieldVNode = h('el-checkbox-group', {
              modelValue,
              'onUpdate:modelValue': updateModel,
              ...fieldProps,
            }, {
              default: () => options.map((opt) => h('el-checkbox', {
                label: opt.value,
              }, () => opt.label)),
            });
            break;
          }
          case 'datePicker': {
            fieldVNode = h('el-date-picker', {
              modelValue,
              'onUpdate:modelValue': updateModel,
              ...fieldProps,
            });
            break;
          }
          default: {
            fieldVNode = h('el-input', {
              modelValue,
              'onUpdate:modelValue': updateModel,
              ...fieldProps,
            });
            break;
          }
        }

        return h('el-form-item', {
          label: c.label ?? prop,
          prop,
          rules: c.required ? [{ required: true, message: `${c.label}不能为空`, trigger: 'blur' }] : [],
        }, () => fieldVNode);
      });
    }

    /**
     * 构建弹窗内容
     */
    function renderContent(item: ModalItem): VNode | VNode[] | null {
      const o = item.options;
      // component 模式
      if ('component' in o && o.component && item.resolvedComponent) {
        const opts = o as ComponentModalOptions;
        const props: Record<string, unknown> = { ...(opts.componentProps || {}) };
        const events = buildComponentEvents(item);
        // 合并事件
        Object.assign(props, events);
        return h(item.resolvedComponent as Component, {
          ...props,
          ref: (el: unknown) => {
            if (el) contentRefs.value[item.id] = el;
          },
        });
      }
      // render 函数模式
      if ('render' in o && o.render) {
        const opts = o as RenderModalOptions;
        const ctx = buildRenderContext(item);
        return opts.render(ctx);
      }
      // config 表单模式
      if ('config' in o && o.config) {
        const fields = buildFormFields(item);
        return h('el-form', { model: item.formData, labelWidth: '120px' }, { default: () => fields });
      }
      return null;
    }

    /**
     * 构建底部按钮
     */
    function renderFooter(item: ModalItem): VNode | null {
      const o = item.options;
      if (o.showFooter === false) return null;
      const style = 'text-align:right;padding:12px 20px;border-top:1px solid #e4e7ed;';

      if (o.footerButtons?.length) {
        return h('div', { class: 'modal-footer', style },
          o.footerButtons.map((b: FooterButton) => h(ElButton, {
            type: b.type ?? 'default',
            loading: b.loading,
            disabled: b.disabled,
            onClick: () => b.onClick({
              id: item.id,
              type: item.type,
              visible: computed(() => item.visible) as unknown as import('vue').Ref<boolean>,
              confirmLoading: computed(() => item.confirmLoading) as unknown as import('vue').Ref<boolean>,
              close: () => closeModalFn(item.id),
              confirm: (d?: unknown) => handleConfirmFn(item, d),
              setConfirmLoading: (l: boolean) => { item.confirmLoading = l; },
              formData: item.formData ? computed(() => item.formData) as unknown as import('vue').Ref<Record<string, unknown>> : undefined,
            }),
          }, { default: () => b.text })),
        );
      }

      return h('div', { class: 'modal-footer', style }, [
        h(ElButton, { onClick: () => handleCancel(item) }, { default: () => o.cancelText ?? '取消' }),
        h(ElButton, {
          type: 'primary',
          loading: item.confirmLoading,
          onClick: () => handleConfirmFn(
            item,
            'config' in o && o.config && item.formData ? item.formData : undefined
          ),
        }, { default: () => o.confirmText ?? '确定' }),
      ]);
    }

    /**
     * 构建弹窗 props
     */
    function buildDialogProps(item: ModalItem): Record<string, unknown> {
      const o = item.options;
      const p: Record<string, unknown> = {
        modelValue: item.visible,
        'onUpdate:modelValue': (v: boolean) => {
          if (!v && !item.closing) {
            handleCancel(item);
          }
        },
        title: o.title ?? '',
        destroyOnClose: o.destroyOnClose ?? true,
        appendToBody: false,
        onClosed: () => handleClosed(),
      };
      ['closeOnClickModal', 'closeOnPressEscape', 'lockScroll', 'modal', 'modalClass',
        'zIndex', 'openDelay', 'closeDelay', 'showClose'].forEach((k) => {
        const obj = o as unknown as Record<string, unknown>;
        if (obj[k] !== undefined) p[k] = obj[k];
      });
      if (o.beforeClose) p.beforeClose = o.beforeClose;

      if (item.type === 'dialog') {
        p.width = o.width ?? '50%';
        if (o.fullscreen !== undefined) p.fullscreen = o.fullscreen;
        if ((o as { alignCenter?: boolean }).alignCenter !== undefined) p.alignCenter = (o as { alignCenter?: boolean }).alignCenter;
        if ((o as { appendToBody?: boolean }).appendToBody !== undefined) p.appendToBody = (o as { appendToBody?: boolean }).appendToBody;
        if ((o as { transition?: string }).transition !== undefined) p.transition = (o as { transition?: string }).transition;
      } else {
        p.size = o.size ?? '50%';
        p.direction = (o as { direction?: string }).direction ?? 'rtl';
        if ((o as { resizable?: boolean }).resizable !== undefined) p.resizable = (o as { resizable?: boolean }).resizable;
        if ((o as { withHeader?: boolean }).withHeader !== undefined) p.withHeader = (o as { withHeader?: boolean }).withHeader;
      }
      if (o.attrs) Object.assign(p, o.attrs);
      return p;
    }

    return () => {
      const dialogs = modals.filter((m) => m.type === 'dialog').map((item) => {
        const content = renderContent(item);
        const footer = renderFooter(item);
        return h(ElDialog, buildDialogProps(item), {
          default: () => content,
          footer: () => footer,
        });
      });

      const drawers = modals.filter((m) => m.type === 'drawer').map((item) => {
        const content = renderContent(item);
        const footer = renderFooter(item);
        return h(ElDrawer, buildDialogProps(item), {
          default: () => content,
          footer: () => footer,
        });
      });

      return h(Teleport, { to: 'body' }, [...dialogs, ...drawers]);
    };
  },
});
</script>

/**
 * AI 填表钩子：监听 AiAssistant 派发的 cube:ai-fill-form 事件，把 AI 生成的字段值填入 antd 表单
 *
 * AiAssistant 在收到后端 fill_form 工具结果（{kind:'fill_form', values, skipped}）时
 * dispatch 全局事件，本钩子由各表单页（FormPage / FormDialog / ConfigPage）挂载监听，
 * 通过 form.setFieldsValue 填充并提示用户核对后保存。
 */
import { useEffect } from 'react';
import type { FormInstance } from 'antd';
import { message } from 'antd';

/** 布尔串转布尔（AI 可能返回 'true'/'false' 字符串） */
function normalizeValue(v: unknown): unknown {
  if (v === 'true') return true;
  if (v === 'false') return false;
  return v;
}

/**
 * 监听 AI 填表事件并写入表单
 * @param form antd 表单实例（由 Form.useForm() 创建）
 * @param enabled 是否启用监听（如弹窗关闭时传 false 跳过）
 */
export function useAiFillForm(form: FormInstance, enabled = true) {
  useEffect(() => {
    if (!enabled) return;

    const onFill = (e: Event) => {
      const detail = (e as CustomEvent<{ values: Record<string, unknown>; skipped?: string[] }>).detail;
      if (!detail?.values) return;

      // 布尔串转布尔，避免复选框等控件拿到 'true'/'false' 字符串
      const values = Object.fromEntries(
        Object.entries(detail.values).map(([k, v]) => [k, normalizeValue(v)]),
      );
      form.setFieldsValue(values);

      const names = Object.keys(detail.values).join('、');
      const skip = detail.skipped?.length ? `（跳过：${detail.skipped.join('、')}）` : '';
      message.success(`AI 已预填 ${Object.keys(detail.values).length} 个字段（${names}）${skip}，请检查后保存`);
    };

    window.addEventListener('cube:ai-fill-form', onFill);
    return () => window.removeEventListener('cube:ai-fill-form', onFill);
  }, [form, enabled]);
}
